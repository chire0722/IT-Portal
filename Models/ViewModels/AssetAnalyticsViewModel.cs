using System;
using System.Collections.Generic;

namespace IT_Form.Models.ViewModels
{
    public class AssetAnalyticsViewModel
    {
        public int TotalAssets { get; set; }
        public int IssuedAssets { get; set; }
        public int UnderWarrantyCount { get; set; }
        public int ExpiredWarrantyCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public IEnumerable<ITAsset> Assets { get; set; } = new List<ITAsset>();
    }
}
