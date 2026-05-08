namespace ArtClub.Models.Entities
{
    public class Reservation
    {
        public int Id { get; set; }
        public int ResourceId { get; set; }
        public virtual Resource Resource { get; set; }

        public int? EventId { get; set; }
        public virtual Event Event { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        // Buffer-ul de 1 zi menționat în diagramă
        public DateTime BufferStart => StartTime.AddDays(-1);
        public DateTime BufferEnd => EndTime.AddDays(1);
    }
}