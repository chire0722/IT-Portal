// Project comment: SoftwareLicenseCompany.cs is part of the IT Form application source code.

namespace IT_Form.Models
{
    /// <summary>
    /// Defines the SoftwareLicenseCompany class used by the IT Form application.
    /// </summary>
    public class SoftwareLicenseCompany
    {
        public int Id { get; set; }

        public int SoftwareLicenseId { get; set; }
        public SoftwareLicense SoftwareLicense { get; set; } = null!;

        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;
    }
}
