using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IReservationService
    {
        /// <summary>
        /// Checks if a resource is available for the given time period.
        /// If adminUserId is provided and the admin can override, returns true regardless of conflicts.
        /// REQ-20: Admin can override reservations, which creates a pending reservation.
        /// </summary>
        Task<bool> CheckAvailabilityAsync(int resourceId, DateTime start, DateTime end, int? adminUserId = null);

        /// <summary>
        /// Creates a regular reservation. If adminUserId is provided, creates an override reservation with PendingApproval status.
        /// REQ-20: Admin overrides are pending approval for 1 hour or until member approves.
        /// </summary>
        Task CreateReservationAsync(Reservation reservation, int? adminUserId = null);

        Task<List<Resource>> GetAllResourcesAsync();
        Task<Resource?> GetResourceByNameAsync(string name);
        Task CreateResourceAsync(Resource resource);
        Task<bool> UpdateResourceAsync(string originalName, Resource model);
        Task<bool> DeleteResourceAsync(string name);
        Task<List<Reservation>> GetReservationCalendarAsync();

        /// <summary>
        /// Gets all reservations.
        /// </summary>
        Task<List<Reservation>> GetAllReservationsAsync();

        /// <summary>
        /// Gets all pending override reservations that need manual approval.
        /// </summary>
        Task<List<Reservation>> GetPendingOverridesAsync();

        /// <summary>
        /// Gets pending admin override reservations that are ready for auto-approval (older than 1 hour).
        /// </summary>
        Task<List<Reservation>> GetPendingOverridesToAutoApproveAsync();

        /// <summary>
        /// Auto-approves a pending override reservation after 1 hour.
        /// </summary>
        Task<bool> AutoApprovePendingOverrideAsync(int reservationId);

        /// <summary>
        /// Member or admin approves a pending override reservation.
        /// </summary>
        Task<bool> ApprovePendingOverrideAsync(int reservationId);

        /// <summary>
        /// Rejects a pending override reservation, requiring user to reschedule.
        /// </summary>
        Task<bool> RejectPendingOverrideAsync(int reservationId);

        /// <summary>
        /// Gets a reservation by ID.
        /// </summary>
        Task<Reservation?> GetReservationByIdAsync(int reservationId);

        /// <summary>
        /// Gets all reservations requiring reschedule due to admin override.
        /// </summary>
        Task<List<Reservation>> GetRequiringRescheduleAsync();

        /// <summary>
        /// Updates a reservation to a new time slot after reschedule.
        /// </summary>
        Task<bool> RescheduleReservationAsync(int reservationId, DateTime newStart, DateTime newEnd);

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        Task<bool> SaveChangesAsync();

        /// <summary>
        /// Gets all external user (non-admin) reservations that overlap with the specified time range,
        /// including their buffer zones.
        /// </summary>
        Task<List<Reservation>> GetExternalUserConflictingReservationsAsync(int resourceId, DateTime start, DateTime end);
    }
}

