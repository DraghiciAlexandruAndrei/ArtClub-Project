using ArtClub.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace ArtClub.Models.Entities
{
    // Moștenim IdentityUser<int> pentru a folosi întregi ca chei primare
    public class User : IdentityUser<int>
    {
        // Proprietăți custom pentru ArtClub
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;

        // REQ-5: Logica de membership mutată aici pentru simplitate
        public bool IsMembershipActive { get; set; }
        public DateTime? MembershipDate { get; set; }
        public int EventCreationLimit { get; set; } = 1; // Legacy property, use GetEventCreationLimit() instead

        // Admin features
        public bool IsBanned { get; set; } = false;
        public int AdminLevel { get; set; } = 0;
        public bool CanOverrideReservations { get; set; } = false;

        // Relații
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Invitation> ReceivedInvitations { get; set; } = new List<Invitation>();

        // Dacă un utilizator poate organiza evenimente
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

        // Get event creation limit based on role
        public int GetEventCreationLimit()
        {
            return Role switch
            {
                UserRole.Admin => 15,
                UserRole.Member => 5,
                _ => 1 // External/default
            };
        }

        // Metodele tale de logică pot rămâne aici
        public bool CanCreateMoreEvents(int currentEventsCount)
        {
            return currentEventsCount < GetEventCreationLimit();
        }
    }
}