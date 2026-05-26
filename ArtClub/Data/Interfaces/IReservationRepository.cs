using ArtClub.Models.Entities;

namespace ArtClub.DataAccess.Interfaces
{
    public interface IReservationRepository
    {
        Task<bool> HasOverlappingReservationAsync(int resourceId, DateTime bufferStart, DateTime bufferEnd);
        Task AddReservationAsync(Reservation reservation);
        Task<List<Resource>> GetAllResourcesWithReservationsAsync();
        Task<Resource?> GetResourceByNameWithReservationsAsync(string name);
        Task<Resource?> GetResourceByNameAsync(string name);
        Task AddResourceAsync(Resource resource);
        Task<List<Reservation>> GetCalendarAsync();
        Task<bool> SaveChangesAsync();
        void RemoveResource(Resource resource);

        /// <summary>
        /// Gets a reservation by ID.
        /// </summary>
        Task<Reservation?> GetReservationByIdAsync(int reservationId);

        /// <summary>
        /// Gets all pending override reservations.
        /// </summary>
        Task<List<Reservation>> GetPendingOverrideReservationsAsync();

        /// <summary>
        /// Gets all reservations that overlap with the specified time range for a resource.
        /// </summary>
        Task<List<Reservation>> GetOverlappingReservationsAsync(int resourceId, DateTime start, DateTime end);

        /// <summary>
        /// Gets all reservations requiring reschedule due to admin override.
        /// </summary>
        Task<List<Reservation>> GetRequiringRescheduleAsync();

        /// <summary>
        /// Gets all external user (non-admin) reservations that overlap with the specified time range,
        /// including their buffer zones.
        /// </summary>
        Task<List<Reservation>> GetExternalUserConflictingReservationsAsync(int resourceId, DateTime start, DateTime end);

        /// <summary>
        /// Gets all conflicting reservations (admin and non-admin) for a resource in the specified time range.
        /// Used when an admin wants to override and cancel conflicting reservations.
        /// </summary>
        Task<List<Reservation>> GetAllConflictingReservationsAsync(int resourceId, DateTime start, DateTime end);
    }
}
