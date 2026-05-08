using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;

namespace ArtClub.Models.ViewModels
{
    public class EventCreateViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public int SelectedResourceId { get; set; }
        public string ResourceName { get; set; }

        public List<int> SelectedArtPieceIds { get; set; } = new List<int>();
        public List<SelectListItem>? AvailableResources { get; set; }
    }
}