using ArtClub.Models.Enums;

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

        /// <summary>
        /// REQ-20: Status of the reservation (Confirmed, PendingApproval, Postponed, Cancelled).
        /// </summary>
        public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;

        /// <summary>
        /// REQ-20: Indicates if this reservation is an admin override that conflicts with existing reservations.
        /// </summary>
        public bool IsAdminOverride { get; set; } = false;

        /// <summary>
        /// REQ-20: ID of the admin who created this override. Null if not an override.
        /// </summary>
        public int? AdminOverrideById { get; set; }

        /// <summary>
        /// REQ-20: Timestamp when the override was created. Used to auto-approve after 1 hour.
        /// </summary>
        public DateTime? OverrideCreatedAt { get; set; }
    }
}