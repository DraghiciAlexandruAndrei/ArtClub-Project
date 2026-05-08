    using System;

namespace ArtClub.Models.ViewModels
{
    public class EventSummaryViewModel
    {
        public string Title { get; set; }
        public string OrganizerName { get; set; }
        public string ResourceName { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public int InviteCount { get; set; }
    }
}