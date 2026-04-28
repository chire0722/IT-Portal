// Project comment: AssetAnalyticsViewModel.cs is part of the IT Form application source code.

using System;
using System.Collections.Generic;
using IT_Form.Application.Common.Models;

namespace IT_Form.Models.ViewModels
{
    /// <summary>
    /// Provides view data for the AssetAnalytics screen or form.
    /// </summary>
    public class AssetAnalyticsViewModel
    {
        public int TotalAssets { get; set; }
        public int IssuedAssets { get; set; }
        public int UnderWarrantyCount { get; set; }
        public int ExpiredWarrantyCount { get; set; }
        public int ExpiringSoonCount { get; set; }
        public IEnumerable<ITAsset> Assets { get; set; } = new List<ITAsset>();
        public int? SelectedCompanyId { get; set; }
        public List<PageCompanyOption> CompanyOptions { get; set; } = [];
    }
}
