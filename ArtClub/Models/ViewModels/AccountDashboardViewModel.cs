using System.Collections.Generic;

namespace ArtClub.Models.ViewModels
{
    public class AccountDashboardViewModel
    {
        public string DisplayName { get; set; }
        public string CurrentRole { get; set; }
        public string MembershipStatus { get; set; }
        public string Email { get; set; }
        public List<string> Notifications { get; set; } = new List<string>();
        public List<string> QuickActions { get; set; } = new List<string>();
    }
}