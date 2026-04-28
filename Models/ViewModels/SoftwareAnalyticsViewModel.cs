// Project comment: SoftwareAnalyticsViewModel.cs is part of the IT Form application source code.

using System.Collections.Generic;

namespace IT_Form.Models.ViewModels
{
    /// <summary>
    /// Provides view data for the SoftwareAnalytics screen or form.
    /// </summary>
    public class SoftwareAnalyticsViewModel
    {
        public int TotalSoftwareCount { get; set; }
        public int TotalVersionCount { get; set; }
        public int TotalSeatCapacity { get; set; }
        public int TotalAssignedSeats { get; set; }
        public int ExpiringSoonCount { get; set; }
        
        public double UtilizationPercentage => TotalSeatCapacity > 0 
            ? (double)TotalAssignedSeats / TotalSeatCapacity * 100 
            : 0;

        public IEnumerable<StandardSoftware> SoftwareList { get; set; } = new List<StandardSoftware>();
    }
}
