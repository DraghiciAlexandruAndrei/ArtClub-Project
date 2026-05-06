namespace ArtClub.Models.ViewModels
{
    public class MemberDashboardViewModel
    {
        // Info Profil
        public string UserName { get; set; }
        public bool IsMembershipActive { get; set; }
        public int RemainingEventLimit { get; set; }

        // Statistici rapide
        public int EventsOrganizedCount { get; set; }
        public int PendingInvitationsCount { get; set; }

        // Liste pentru afișare
        public List<EventSummaryViewModel> UpcomingEvents { get; set; } // Evenimente la care participă
        public List<InvitationInboxViewModel> RecentInvitations { get; set; }
    }
}