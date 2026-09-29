using StayPilot.Application.Contracts.Response;
using StayPilot.Application.Interfaces.Services;

namespace StayPilot.Api.Services
{
    /// <inheritdoc cref="IStatsRecalculationRunner"/>
    /// <remarks>
    /// Registered as a singleton, because the whole point is one piece of state shared by every
    /// request: is a run going on, and how did the last one end.
    ///
    /// Known limit, accepted rather than hidden: the API runs on an App Service plan with no
    /// "Always On", so an idle recycle can kill a run in flight. Nothing here can stop that. What
    /// it does instead is refuse to lie about it - the state goes back to "not running" with no
    /// new CalculatedAtUtc, and the caller compares the stamp on the stats rows before and after
    /// to see the run never landed. The rewrite itself is one SaveChanges, so a killed run leaves
    /// the old rows intact rather than half a table.
    /// </remarks>
    public class StatsRecalculationRunner : IStatsRecalculationRunner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StatsRecalculationRunner> _logger;

        // Guards every field below it. They are read by status requests while the run is writing
        // them, so none of this is safe to touch outside the lock.
        private readonly object _gate = new();

        private bool _isRunning;
        private DateTime? _startedAtUtc;
        private DateTime? _finishedAtUtc;
        private int _listingsUsed;
        private int _rowsWritten;
        private DateTime? _calculatedAtUtc;
        private string? _failureReason;

        public StatsRecalculationRunner(IServiceScopeFactory scopeFactory, ILogger<StatsRecalculationRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <inheritdoc/>
        public RecalculateMarketAreaStatsResponse Start()
        {
            lock (_gate)
            {
                if (_isRunning)
                {
                    return Snapshot();
                }

                _isRunning = true;
                _startedAtUtc = DateTime.UtcNow;
                _finishedAtUtc = null;
                _failureReason = null;
            }

            // Deliberately not awaited: the caller gets its answer now, the work carries on after
            // the response has gone. RunAsync never throws, so there is no unobserved task
            // exception to lose.
            _ = Task.Run(RunAsync);

            return GetStatus();
        }

        /// <inheritdoc/>
        public RecalculateMarketAreaStatsResponse GetStatus()
        {
            lock (_gate)
            {
                return Snapshot();
            }
        }

        /// <summary>
        /// The run itself. Owns its own DI scope: the request that started it is long gone by the
        /// time this touches the database, and with it the scoped DbContext it would otherwise
        /// have captured.
        /// </summary>
        private async Task RunAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var statsService = scope.ServiceProvider.GetRequiredService<IMarketAreaStatsService>();
                var result = await statsService.RecalculateMarketAreaStatsAsync();

                lock (_gate)
                {
                    if (result.Succeeded)
                    {
                        _listingsUsed = result.ListingsUsed;
                        _rowsWritten = result.RowsWritten;
                        _calculatedAtUtc = result.CalculatedAtUtc;
                        _failureReason = null;
                    }
                    else
                    {
                        // The recalculation reports trouble as errors rather than throwing, so a
                        // failed run looks exactly like a finished one unless we read them out.
                        _failureReason = string.Join(" ", result.Errors!.Select(x => x.ErrorMessage));
                    }
                }

                _logger.LogInformation(
                    "Stats recalculation finished. Succeeded: {Succeeded}, listings: {ListingsUsed}, rows: {RowsWritten}.",
                    result.Succeeded,
                    result.ListingsUsed,
                    result.RowsWritten);
            }
            catch (Exception exception)
            {
                // Nothing is awaiting this task, so an exception escaping here would vanish
                // without a trace and leave _isRunning stuck true.
                _logger.LogError(exception, "Stats recalculation failed.");

                lock (_gate)
                {
                    _failureReason = exception.Message;
                }
            }
            finally
            {
                lock (_gate)
                {
                    _isRunning = false;
                    _finishedAtUtc = DateTime.UtcNow;
                }
            }
        }

        /// <summary>The current state as a response. Only ever called while holding the lock.</summary>
        private RecalculateMarketAreaStatsResponse Snapshot()
        {
            return new RecalculateMarketAreaStatsResponse
            {
                IsRunning = _isRunning,
                StartedAtUtc = _startedAtUtc,
                FinishedAtUtc = _finishedAtUtc,
                ListingsUsed = _listingsUsed,
                RowsWritten = _rowsWritten,
                CalculatedAtUtc = _calculatedAtUtc,
                FailureReason = _failureReason
            };
        }
    }
}
