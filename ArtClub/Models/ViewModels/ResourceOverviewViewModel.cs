namespace ArtClub.Models.ViewModels
{
    public class ResourceOverviewViewModel
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public int Capacity { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }

        // For equipment: quantity available (can have multiple)
        public int QuantityAvailable { get; set; } = 1;
    }
}