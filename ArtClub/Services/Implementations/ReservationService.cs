using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Services.Interfaces;
using ArtClub.DataAccess.Interfaces;

namespace ArtClub.Services.Implementations
{
    public class ReservationService : IReservationService
    {
        private readonly IReservationRepository _reservationRepo;

        public ReservationService(IReservationRepository reservationRepo)
        {
            _reservationRepo = reservationRepo;
        }

        /// <summary>
        /// Checks if a resource is available for the given time period.
        /// REQ-20: If adminUserId is provided, the check allows admin overrides (returns true always).
        /// For non-admins, applies 1-day buffer strictly.
        /// </summary>
        public async Task<bool> CheckAvailabilityAsync(int resourceId, DateTime start, DateTime end, int? adminUserId = null)
        {
            // Admin bypass - admins can always book (override will handle conflicts)
            if (adminUserId.HasValue)
            {
                return true;
            }

            // For regular users (members/external), check with buffer applied
            // The buffer is already included in the Reservation.BufferStart/BufferEnd properties
            var isOverlapping = await _reservationRepo.HasOverlappingReservationAsync(
                resourceId,
                start,
                end);

            return !isOverlapping;
        }

        /// <summary>
        /// Creates a regular reservation.
        /// REQ-20: If adminUserId is provided, creates an override reservation with PendingApproval status
        /// and marks all conflicting reservations as OverrideRequired (requiring user to reschedule).
        /// </summary>
        public async Task CreateReservationAsync(Reservation reservation, int? adminUserId = null)
        {
            if (adminUserId.HasValue)
            {
                // This is an admin override - mark it as pending approval
                reservation.IsAdminOverride = true;
                reservation.AdminOverrideById = adminUserId.Value;
                reservation.Status = ReservationStatus.PendingApproval;
                reservation.OverrideCreatedAt = DateTime.UtcNow;

                // Find all conflicting reservations and mark them as OverrideRequired
                var conflictingReservations = await _reservationRepo.GetExternalUserConflictingReservationsAsync(
                    reservation.ResourceId,
                    reservation.StartTime,
                    reservation.EndTime);

                foreach (var conflictingRes in conflictingReservations)
                {
                    // Mark external user reservations as requiring reschedule
                    if (conflictingRes.Status != ReservationStatus.Cancelled && 
                        !conflictingRes.IsAdminOverride)
                    {
                        conflictingRes.Status = ReservationStatus.OverrideRequired;
                    }
                }
            }

            await _reservationRepo.AddReservationAsync(reservation);
            await _reservationRepo.SaveChangesAsync();
        }

        public async Task<List<Resource>> GetAllResourcesAsync()
        {
            return await _reservationRepo.GetAllResourcesWithReservationsAsync();
        }

        public async Task<Resource?> GetResourceByNameAsync(string name)
        {
            return await _reservationRepo.GetResourceByNameWithReservationsAsync(name);
        }

        public async Task CreateResourceAsync(Resource resource)
        {
            await _reservationRepo.AddResourceAsync(resource);
            await _reservationRepo.SaveChangesAsync();
        }

        public async Task<bool> UpdateResourceAsync(string originalName, Resource model)
        {
            var resource = await _reservationRepo.GetResourceByNameAsync(originalName);

            if (resource == null) return false;

            resource.Name = model.Name;
            resource.Description = model.Description;
            resource.Capacity = model.Capacity;
            resource.BasePrice = model.BasePrice;
            resource.IsExhibitionHall = model.IsExhibitionHall;

            return await _reservationRepo.SaveChangesAsync();
        }

        public async Task<bool> DeleteResourceAsync(string name)
        {
            var resource = await _reservationRepo.GetResourceByNameAsync(name);

            if (resource == null) return false;

            _reservationRepo.RemoveResource(resource);
            return await _reservationRepo.SaveChangesAsync();
        }

        public async Task<List<Reservation>> GetReservationCalendarAsync()
        {
            return await _reservationRepo.GetCalendarAsync();
        }

        /// <summary>
        /// Gets all reservations.
        /// </summary>
        public async Task<List<Reservation>> GetAllReservationsAsync()
        {
            return await _reservationRepo.GetCalendarAsync();
        }

        /// <summary>
        /// Gets all pending override reservations that need manual approval.
        /// </summary>
        public async Task<List<Reservation>> GetPendingOverridesAsync()
        {
            return await _reservationRepo.GetPendingOverrideReservationsAsync();
        }

        /// <summary>
        /// Gets pending admin override reservations that are ready for auto-approval (older than 1 hour).
        /// </summary>
        public async Task<List<Reservation>> GetPendingOverridesToAutoApproveAsync()
        {
            var pending = await _reservationRepo.GetPendingOverrideReservationsAsync();
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);

            // Return only those that were created more than 1 hour ago
            return pending.Where(r => r.OverrideCreatedAt.HasValue && r.OverrideCreatedAt.Value < oneHourAgo).ToList();
        }

        /// <summary>
        /// Auto-approves a pending override reservation after 1 hour.
        /// </summary>
        public async Task<bool> AutoApprovePendingOverrideAsync(int reservationId)
        {
            var reservation = await _reservationRepo.GetReservationByIdAsync(reservationId);
            if (reservation == null || reservation.Status != ReservationStatus.PendingApproval)
                return false;

            reservation.Status = ReservationStatus.Confirmed;
            return await _reservationRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Member or admin approves a pending override reservation.
        /// </summary>
        public async Task<bool> ApprovePendingOverrideAsync(int reservationId)
        {
            var reservation = await _reservationRepo.GetReservationByIdAsync(reservationId);
            if (reservation == null || reservation.Status != ReservationStatus.PendingApproval)
                return false;

            reservation.Status = ReservationStatus.Confirmed;
            return await _reservationRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Rejects a pending override reservation, marking the conflicting reservation as postponed.
        /// </summary>
        public async Task<bool> RejectPendingOverrideAsync(int reservationId)
        {
            var reservation = await _reservationRepo.GetReservationByIdAsync(reservationId);
            if (reservation == null || reservation.Status != ReservationStatus.PendingApproval)
                return false;

            reservation.Status = ReservationStatus.OverrideRequired;
            return await _reservationRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Gets a reservation by ID.
        /// </summary>
        public async Task<Reservation?> GetReservationByIdAsync(int reservationId)
        {
            return await _reservationRepo.GetReservationByIdAsync(reservationId);
        }

        /// <summary>
        /// Gets all reservations requiring reschedule due to admin override.
        /// </summary>
        public async Task<List<Reservation>> GetRequiringRescheduleAsync()
        {
            return await _reservationRepo.GetRequiringRescheduleAsync();
        }

        /// <summary>
        /// Updates a reservation to a new time slot after reschedule.
        /// </summary>
        public async Task<bool> RescheduleReservationAsync(int reservationId, DateTime newStart, DateTime newEnd)
        {
            var reservation = await _reservationRepo.GetReservationByIdAsync(reservationId);
            if (reservation == null)
                return false;

            // Check if new time slot is available
            var isAvailable = await CheckAvailabilityAsync(reservation.ResourceId, newStart, newEnd);
            if (!isAvailable)
                return false;

            reservation.StartTime = newStart;
            reservation.EndTime = newEnd;
            reservation.Status = ReservationStatus.Confirmed;
            return await _reservationRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Saves changes to the database.
        /// </summary>
        public async Task<bool> SaveChangesAsync()
        {
            return await _reservationRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all external user (non-admin) reservations that overlap with the specified time range,
        /// including their buffer zones.
        /// </summary>
        public async Task<List<Reservation>> GetExternalUserConflictingReservationsAsync(int resourceId, DateTime start, DateTime end)
        {
            return await _reservationRepo.GetExternalUserConflictingReservationsAsync(resourceId, start, end);
        }
    }
}
