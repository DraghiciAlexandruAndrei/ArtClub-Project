using ArtClub.Models.Entities;
using System;
using System.Collections.Generic;

namespace ArtClub.Models.ViewModels
{
    public class EventDetailsViewModel
    {
        public int EventId { get; set; }
        public string Title { get; set; }
        public string OrganizerName { get; set; }
        public string ResourceName { get; set; }
        public DateTime Date { get; set; }
        public int AttendingCount { get; set; }
        public List<string> ArtPieceNames { get; set; }

        // Proprietatea nouă:
        public List<Invitation> Invitations { get; set; } = new List<Invitation>();
    }
}