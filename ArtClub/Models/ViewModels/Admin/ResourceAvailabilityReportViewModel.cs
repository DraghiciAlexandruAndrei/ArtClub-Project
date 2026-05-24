using System;
using System.Collections.Generic;

namespace ArtClub.Models.ViewModels.Admin
{
    public class ResourceAvailabilityReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AvailableCount => AvailableResources?.Count ?? 0;
        public int UnavailableCount { get; set; }
        public List<ResourceAvailabilityReportRowViewModel> AvailableResources { get; set; } = new();
    }

    public class ResourceAvailabilityReportRowViewModel
    {
        public int ResourceId { get; set; }
        public string ResourceName { get; set; }
        public int Capacity { get; set; }
        public int ConflictCount { get; set; }
    }
}
