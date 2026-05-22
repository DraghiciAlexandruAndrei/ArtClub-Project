using ArtClub.Models.Enums;

namespace ArtClub.Models.ViewModels.Reservation
{
    public class ReservationStatusViewModel
    {
        public int ReservationId { get; set; }
        public int ResourceId { get; set; }
        public string ResourceName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public ReservationStatus Status { get; set; }
        public bool IsAdminOverride { get; set; }
        public int? AdminOverrideById { get; set; }
        public DateTime? OverrideCreatedAt { get; set; }
        public string StatusLabel => GetStatusLabel();
        public string StatusBadgeClass => GetStatusBadgeClass();

        private string GetStatusLabel()
        {
            return Status switch
            {
                ReservationStatus.Confirmed => "Confirmed",
                ReservationStatus.PendingApproval => "Pending Approval",
                ReservationStatus.OverrideRequired => "Reschedule Required",
                ReservationStatus.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        private string GetStatusBadgeClass()
        {
            return Status switch
            {
                ReservationStatus.Confirmed => "badge bg-success",
                ReservationStatus.PendingApproval => "badge bg-warning text-dark",
                ReservationStatus.OverrideRequired => "badge bg-danger",
                ReservationStatus.Cancelled => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }
    }
}
