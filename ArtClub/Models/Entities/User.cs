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
        public int EventCreationLimit { get; set; } = 1; // 1 implicit, 5 după upgrade

        // Relații
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public virtual ICollection<Invitation> ReceivedInvitations { get; set; } = new List<Invitation>();
        
        // Dacă un utilizator poate organiza evenimente
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

        // Metodele tale de logică pot rămâne aici
        public bool CanCreateMoreEvents(int currentEventsCount)
        {
            return currentEventsCount < EventCreationLimit;
        }
    }
}