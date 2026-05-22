namespace ArtClub.Models.Enums
{
    /// <summary>
    /// REQ-20: Represents the status of a reservation, including pending approval for admin overrides.
    /// </summary>
    public enum ReservationStatus
    {
        /// <summary>
        /// Reservation is confirmed and valid.
        /// </summary>
        Confirmed = 0,

        /// <summary>
        /// Admin override reservation pending approval by member or automatic approval after 1 hour.
        /// </summary>
        PendingApproval = 1,

        /// <summary>
        /// Reservation was overridden by admin - user must reschedule to a new time.
        /// </summary>
        OverrideRequired = 2,

        /// <summary>
        /// Reservation was cancelled.
        /// </summary>
        Cancelled = 3
    }
}
