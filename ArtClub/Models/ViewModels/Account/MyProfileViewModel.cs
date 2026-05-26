using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels.Event;

namespace ArtClub.Models.ViewModels.Account
{
    public class MyProfileViewModel
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime JoinedDate { get; set; }
        public UserRole Role { get; set; }

        // REQ: Status membru pentru calculul tarifelor și afișarea butonului de plată
        public bool IsMembershipActive { get; set; }

        // Lista de invitații primite (existentă)
        public List<InvitationInboxViewModel> PendingInvitations { get; set; } = new();

        // REQ: Lista de evenimente organizate de utilizator
        public List<EventSummaryViewModel> MyOrganizedEvents { get; set; } = new();
    }
}
