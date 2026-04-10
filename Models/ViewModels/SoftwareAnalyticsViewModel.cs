using System.Collections.Generic;

namespace IT_Form.Models.ViewModels
{
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
