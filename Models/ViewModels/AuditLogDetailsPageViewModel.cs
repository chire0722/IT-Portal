// Project comment: AuditLogDetailsPageViewModel.cs is part of the IT Form application source code.

using System.Collections.Generic;

namespace IT_Form.Models.ViewModels
{
    /// <summary>
    /// Provides view data for the AuditLogDetailsPage screen or form.
    /// </summary>
    public class AuditLogDetailsPageViewModel
    {
        public AuditLog Entry { get; set; } = null!;
        public string? FormattedAuditDetails { get; set; }
        public IReadOnlyCollection<AuditLog> RelatedEntries { get; set; } = new List<AuditLog>();
        public bool FailuresOnly { get; set; }
        public int ChainTotalCount { get; set; }
        public int ChainFailureCount { get; set; }
        public int VisibleChainCount => RelatedEntries.Count;

        public bool HasCorrelation => Entry?.CorrelationId.HasValue == true;
        public bool HasRelatedEntries => RelatedEntries.Count > 1;
    }
}
