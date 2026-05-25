using System;
using ArtClub.Models.Enums;

namespace ArtClub.Models.ViewModels.Event
{
    public class EventSummaryViewModel
    {
        public string Title { get; set; }
        public string OrganizerName { get; set; }
        public string ResourceName { get; set; }
        public string Status { get; set; }
        public DateTime StartDate { get; set; }
        public int InviteCount { get; set; }
        public ReservationStatus? ReservationStatus { get; set; }
        public string ReservationStatusLabel => GetReservationStatusLabel();
        public string ReservationStatusBadgeClass => GetReservationStatusBadgeClass();

        private string GetReservationStatusLabel()
        {
            return ReservationStatus switch
            {
                Models.Enums.ReservationStatus.Confirmed => "Confirmed",
                Models.Enums.ReservationStatus.PendingApproval => "Pending Approval",
                Models.Enums.ReservationStatus.OverrideRequired => "Reschedule Required",
                Models.Enums.ReservationStatus.Cancelled => "Cancelled",
                _ => "Unknown"
            };
        }

        private string GetReservationStatusBadgeClass()
        {
            return ReservationStatus switch
            {
                Models.Enums.ReservationStatus.Confirmed => "badge bg-success",
                Models.Enums.ReservationStatus.PendingApproval => "badge bg-warning text-dark",
                Models.Enums.ReservationStatus.OverrideRequired => "badge bg-danger",
                Models.Enums.ReservationStatus.Cancelled => "badge bg-secondary",
                _ => "badge bg-secondary"
            };
        }
    }
}
