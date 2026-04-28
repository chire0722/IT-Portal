// Project comment: CompanyImportMatchHelperTests.cs is part of the IT Form application source code.

using IT_Form.Helpers;
using IT_Form.Models;
using Xunit;

namespace IT_Form.Tests
{
    /// <summary>
    /// Defines the CompanyImportMatchHelperTests class used by the IT Form application.
    /// </summary>
    public class CompanyImportMatchHelperTests
    {
        [Fact]
        /// <summary>
        /// Runs the FindCompanyByReference_MatchesUniquePartialCompanyName operation.
        /// </summary>
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
        /// <summary>
        /// Runs the FindCompanyByReference_MatchesUniqueLotNumberFragment operation.
        /// </summary>
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
        /// <summary>
        /// Runs the FindCompanyByReference_MatchesCombinedPartialNameAndLot operation.
        /// </summary>
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
        /// <summary>
        /// Runs the FindCompanyByReference_DoesNotMatchAmbiguousPartialCompanyName operation.
        /// </summary>
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
