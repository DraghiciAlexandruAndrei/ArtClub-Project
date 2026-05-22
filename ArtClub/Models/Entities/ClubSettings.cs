namespace ArtClub.Models.Entities
{
    /// <summary>
    /// Stores configurable settings for the art club, including payment amounts and policies.
    /// REQ-44: Admin-defined payment amounts (removal of hardcoding)
    /// </summary>
    public class ClubSettings
    {
        public int Id { get; set; }

        /// <summary>
        /// Non-member reservation fee per day (in lei). Default: 400 lei.
        /// </summary>
        public decimal NonMemberReservationFeePerDay { get; set; } = 400m;

        /// <summary>
        /// Membership/subscription cost (in lei). Default: value to be determined.
        /// </summary>
        public decimal MembershipCost { get; set; } = 100m;

        /// <summary>
        /// Event organization cost per art piece (in lei). Default: 200 lei.
        /// </summary>
        public decimal EventCostPerArtPiece { get; set; } = 200m;

        /// <summary>
        /// Event organization cost per location/resource (in lei). Default: 300 lei.
        /// </summary>
        public decimal EventCostPerLocation { get; set; } = 300m;

        /// <summary>
        /// Hours after which a pending admin override reservation is auto-approved.
        /// Default: 1 hour (3600 seconds).
        /// </summary>
        public int PendingOverrideApprovalHours { get; set; } = 1;

        /// <summary>
        /// Last updated timestamp.
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
