// Project comment: AuditLogIndexPageViewModel.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using IT_Form.Helpers;

namespace IT_Form.Models.ViewModels
{
    /// <summary>
    /// Provides view data for the AuditLogIndexPage screen or form.
    /// </summary>
    public class AuditLogIndexPageViewModel
    {
        public PaginatedList<AuditLog> Entries { get; set; } = new(new List<AuditLog>(), 0, 1, 25);

        public string? Entity { get; set; }
        public string? AuditAction { get; set; }
        public string? Search { get; set; }
        public bool? IsSuccess { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }

        public int FilteredTotalCount => Entries.TotalCount;
        public int FilteredSuccessCount { get; set; }
        public int FilteredFailureCount { get; set; }
        public int FilteredCommandCount { get; set; }
        public int FailedCommandCount { get; set; }
        public double FailureRate => FilteredTotalCount == 0
            ? 0
            : Math.Round((double)FilteredFailureCount / FilteredTotalCount * 100, 1);

        public IEnumerable<AuditLogActionStatViewModel> TopFailedActions { get; set; } = new List<AuditLogActionStatViewModel>();
        public IEnumerable<AuditLogActorStatViewModel> TopFailedActors { get; set; } = new List<AuditLogActorStatViewModel>();
        public IEnumerable<AuditLogTrendPointViewModel> FailureTrend { get; set; } = new List<AuditLogTrendPointViewModel>();
    }

    /// <summary>
    /// Provides view data for the AuditLogActionStat screen or form.
    /// </summary>
    public class AuditLogActionStatViewModel
    {
        public string Action { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    /// <summary>
    /// Provides view data for the AuditLogTrendPoint screen or form.
    /// </summary>
    public class AuditLogTrendPointViewModel
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
        public string Label => Date.ToString("dd MMM");
    }

    /// <summary>
    /// Provides view data for the AuditLogActorStat screen or form.
    /// </summary>
    public class AuditLogActorStatViewModel
    {
        public string Actor { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
