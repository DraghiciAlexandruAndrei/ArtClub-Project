using System;
using System.Collections.Generic;
using System.Linq;

namespace ArtClub.Models.ViewModels.Account
{
    public class InvitationInboxViewModel
    {
        public int InvitationId { get; set; }
        public string EventTitle { get; set; }
        public string OrganizerName { get; set; }
        public DateTime EventDate { get; set; }
        public string Description { get; set; }
    }
}