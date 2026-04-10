using IT_Form.Helpers;
using IT_Form.Models;
using Xunit;

namespace IT_Form.Tests
{
    public class CompanyImportMatchHelperTests
    {
        [Fact]
        public void FindCompanyByReference_MatchesUniquePartialCompanyName()
        {
            var companies = new[]
            {
                new Company { Id = 1, Name = "YTC Precast", Lot = "LOT897", IsActive = true },
                new Company { Id = 2, Name = "YTC Transportation", Lot = "LOT123", IsActive = true }
            };

            var result = CompanyImportMatchHelper.FindCompanyByReference(companies, "Precast");

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public void FindCompanyByReference_MatchesUniqueLotNumberFragment()
        {
            var companies = new[]
            {
                new Company { Id = 1, Name = "YTC Precast", Lot = "LOT897", IsActive = true },
                new Company { Id = 2, Name = "YTC Transportation", Lot = "LOT123", IsActive = true }
            };

            var result = CompanyImportMatchHelper.FindCompanyByReference(companies, "897");

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public void FindCompanyByReference_MatchesCombinedPartialNameAndLot()
        {
            var companies = new[]
            {
                new Company { Id = 1, Name = "YTC Precast", Lot = "LOT897", IsActive = true },
                new Company { Id = 2, Name = "YTC Precast", Lot = "LOT123", IsActive = true }
            };

            var result = CompanyImportMatchHelper.FindCompanyByReference(companies, "Precast 897");

            Assert.NotNull(result);
            Assert.Equal(1, result!.Id);
        }

        [Fact]
        public void FindCompanyByReference_DoesNotMatchAmbiguousPartialCompanyName()
        {
            var companies = new[]
            {
                new Company { Id = 1, Name = "YTC Precast", Lot = "LOT897", IsActive = true },
                new Company { Id = 2, Name = "YTC Transportation", Lot = "LOT123", IsActive = true }
            };

            var result = CompanyImportMatchHelper.FindCompanyByReference(companies, "YTC");

            Assert.Null(result);
        }
    }
}
