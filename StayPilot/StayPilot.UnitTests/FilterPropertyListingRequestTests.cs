using System.ComponentModel.DataAnnotations;
using StayPilot.Application.Contracts.Request;

namespace StayPilot.UnitTests
{
    // The paging bounds on the listing search, pinned.
    //
    // They used to be PageNumber 1..50 and PageSize 1..20, which multiply out to a hard ceiling
    // of 1,000 rows. A distrito like Faro holds over nine thousand listings, so page 51 answered
    // 400 and most of the market simply could not be reached - and the browser, which fetched
    // every page it was allowed and then sorted the pile itself, ranked that arbitrary first
    // thousand while calling it the whole set.
    //
    // These are not tests of the .NET validation attribute; they are a guard on the contract, so
    // that narrowing either range again fails here rather than in the browser months later.
    public class FilterPropertyListingRequestTests
    {
        private static List<ValidationResult> Validate(FilterPropertyListingRequest request)
        {
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);

            return results;
        }

        private static bool IsInvalid(List<ValidationResult> results, string member)
        {
            return results.Any(x => x.MemberNames.Contains(member));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(51)]    // the first page the old ceiling refused
        [InlineData(471)]   // Faro's last page at 20 a page
        [InlineData(10000)]
        public void PageNumber_WithinRange_IsAccepted(int pageNumber)
        {
            var request = new FilterPropertyListingRequest { PageNumber = pageNumber };

            Assert.False(IsInvalid(Validate(request), nameof(request.PageNumber)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(10001)]
        public void PageNumber_OutsideRange_IsRejected(int pageNumber)
        {
            var request = new FilterPropertyListingRequest { PageNumber = pageNumber };

            Assert.True(IsInvalid(Validate(request), nameof(request.PageNumber)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(20)]
        [InlineData(50)]
        [InlineData(100)]   // the page size the old cap refused
        public void PageSize_WithinRange_IsAccepted(int pageSize)
        {
            var request = new FilterPropertyListingRequest { PageSize = pageSize };

            Assert.False(IsInvalid(Validate(request), nameof(request.PageSize)));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void PageSize_OutsideRange_IsRejected(int pageSize)
        {
            var request = new FilterPropertyListingRequest { PageSize = pageSize };

            Assert.True(IsInvalid(Validate(request), nameof(request.PageSize)));
        }

        // The point of the whole change, stated as a number: the two ranges together have to
        // reach past any single distrito in the collection. Faro is the largest slice a reader
        // actually browses; at the old bounds this product was 1,000.
        [Fact]
        public void PagingBounds_ReachTheWholeOfALargeDistrito()
        {
            var request = new FilterPropertyListingRequest();

            var maximumPageNumber = MaximumOf(request, nameof(request.PageNumber));
            var maximumPageSize = MaximumOf(request, nameof(request.PageSize));

            Assert.True(
                maximumPageNumber * maximumPageSize >= 10_000,
                $"Paging reaches only {maximumPageNumber * maximumPageSize} rows; Faro alone holds over 9,000.");
        }

        private static int MaximumOf(FilterPropertyListingRequest request, string propertyName)
        {
            var range = request.GetType()
                .GetProperty(propertyName)!
                .GetCustomAttributes(typeof(RangeAttribute), inherit: false)
                .Cast<RangeAttribute>()
                .Single();

            return (int)range.Maximum;
        }
    }
}
