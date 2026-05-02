using System;

namespace ArtClub.Models.ViewModels
{
    public class InvitationInboxViewModel
    {
        public string EventTitle { get; set; }
        public string SenderName { get; set; }
        public string Status { get; set; }
        public DateTime SentAt { get; set; }
    }
}