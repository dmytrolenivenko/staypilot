
namespace StayPilot.Application.Interfaces.Services
{
    public interface ICurrentUser
    {
        Task<int> GetCurrentUserIdAsync();
    }
}
