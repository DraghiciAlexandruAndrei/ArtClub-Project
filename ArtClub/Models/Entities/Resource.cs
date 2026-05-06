using System.Collections.Generic;
using ArtClub.Models.Enums;

namespace ArtClub.Models.Entities
{
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public decimal BasePrice { get; set; }

        // Resource type: Venue/Room vs Equipment
        public ResourceType Type { get; set; }

        // For equipment: quantity available (can have multiple)
        // For venues: usually 1 (single room/hall)
        public int QuantityAvailable { get; set; } = 1;

        // Proprietăți de navigare (Relațiile din diagrama ta)

        // O resursă poate avea mai multe rezervări în timp
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // O resursă poate fi asociată cu mai multe evenimente (istoric)
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}