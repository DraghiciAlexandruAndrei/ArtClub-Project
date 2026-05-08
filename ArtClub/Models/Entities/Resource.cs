using System.Collections.Generic;

namespace ArtClub.Models.Entities
{
    public class Resource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int Capacity { get; set; }
        public decimal BasePrice { get; set; }

        // Proprietăți de navigare (Relațiile din diagrama ta)

        // O resursă poate avea mai multe rezervări în timp
        public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

        // O resursă poate fi asociată cu mai multe evenimente (istoric)
        public virtual ICollection<Event> Events { get; set; } = new List<Event>();
    }
}