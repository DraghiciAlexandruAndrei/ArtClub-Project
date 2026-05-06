using ArtClub.Models.Enums;
using System.Collections.Generic;
using System;

namespace ArtClub.Models.Entities
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public EventStatus Status { get; set; } = EventStatus.Draft;

        // Buget eveniment
        public decimal Budget { get; set; }

        // CORECT: Legătura trebuie să fie către clasa User (care moștenește IdentityUser)
        public int OrganizerId { get; set; }
        public virtual User Organizer { get; set; }

        public int ResourceId { get; set; }
        public virtual Resource Resource { get; set; }

        public virtual Reservation Reservation { get; set; }

        // Relații Many-to-Many și invitații
        public virtual ICollection<EventArtPiece> EventArtPieces { get; set; } = new List<EventArtPiece>();
        public virtual ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

        public void Activate() => Status = EventStatus.Active;
        public void Cancel() => Status = EventStatus.Cancelled;
    }
}