using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtClub.Models.ViewModels
{
    public class ResourceCreateViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }

        // Resource type selection
        public int ResourceTypeId { get; set; }

        // For equipment: how many units are available
        public int QuantityAvailable { get; set; } = 1;

        // For dropdown selection in view
        public List<SelectListItem> ResourceTypes { get; set; } = new List<SelectListItem>();
    }
}