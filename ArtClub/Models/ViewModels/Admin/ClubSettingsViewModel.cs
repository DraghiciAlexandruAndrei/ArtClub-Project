using System.ComponentModel.DataAnnotations;

namespace ArtClub.Models.ViewModels.Admin
{
    public class ClubSettingsViewModel
    {
        [Required]
        public decimal NonMemberReservationFeePerDay { get; set; }

        [Required]
        public decimal MembershipCost { get; set; }

        [Required]
        public decimal EventCostPerArtPiece { get; set; }

        [Required]
        public decimal EventCostPerLocation { get; set; }

        [Required]
        [Range(1, 24)]
        public int PendingOverrideApprovalHours { get; set; }
    }
}
