using ArtClub.Models.Enums;

namespace ArtClub.Models.Entities
{
    public class Invitation
    {
        public int Id { get; set; }

        public int EventId { get; set; }
        public virtual Event Event { get; set; }

        public int InviteeId { get; set; }
        public virtual User Invitee { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        // Metodele din diagrama ta UML
        public void Accept() => Status = InvitationStatus.Accepted;
        public void Decline() => Status = InvitationStatus.Declined;
    }
}