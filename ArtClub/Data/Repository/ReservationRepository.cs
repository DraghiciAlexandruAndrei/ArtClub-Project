using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.DataAccess.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> HasOverlappingReservationAsync(int resourceId, DateTime start, DateTime end)
        {
            // Check if the requested interval [start, end] overlaps with ANY existing reservation's interval (including their buffers)
            // Existing reservations have: [reservation.StartTime - 1 day, reservation.EndTime + 1 day]
            // We need to check if the new request overlaps with the buffered intervals of existing reservations
            return await _context.Reservations
                .Where(r => r.ResourceId == resourceId && r.Status != ReservationStatus.Cancelled)
                .AnyAsync(r => 
                    // Check if new request overlaps with existing reservation's BUFFERED time range
                    // Compute buffer inline for EF Core translation: StartTime - 1 day, EndTime + 1 day
                    r.StartTime.AddDays(-1) < end && 
                    r.EndTime.AddDays(1) > start);
        }

        /// <summary>
        /// Gets all reservations that overlap with the specified time range for a resource.
        /// </summary>
        public async Task<List<Reservation>> GetOverlappingReservationsAsync(int resourceId, DateTime start, DateTime end)
        {
            return await _context.Reservations
                .Where(r => r.ResourceId == resourceId &&
                           r.StartTime < end &&
                           r.EndTime > start &&
                           r.Status != ReservationStatus.Cancelled &&
                           r.Status != ReservationStatus.OverrideRequired)
                .ToListAsync();
        }

        public async Task AddReservationAsync(Reservation reservation) => await _context.Reservations.AddAsync(reservation);

        public async Task<List<Resource>> GetAllResourcesWithReservationsAsync() =>
            await _context.Resources.Include(r => r.Reservations).ToListAsync();

        public async Task<Resource?> GetResourceByNameWithReservationsAsync(string name) =>
            await _context.Resources.Include(r => r.Reservations).FirstOrDefaultAsync(r => r.Name == name);

        public async Task<Resource?> GetResourceByNameAsync(string name) =>
            await _context.Resources.FirstOrDefaultAsync(r => r.Name == name);

        public async Task AddResourceAsync(Resource resource) => await _context.Resources.AddAsync(resource);

        public async Task<List<Reservation>> GetCalendarAsync() =>
            await _context.Reservations.Include(r => r.Resource).OrderBy(r => r.StartTime).Include(r => r.Event).ToListAsync();

        public void RemoveResource(Resource resource) => _context.Resources.Remove(resource);

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

        /// <summary>
        /// Gets a reservation by ID.
        /// </summary>
        public async Task<Reservation?> GetReservationByIdAsync(int reservationId) =>
            await _context.Reservations.FirstOrDefaultAsync(r => r.Id == reservationId);

        /// <summary>
        /// Gets all pending override reservations.
        /// </summary>
        public async Task<List<Reservation>> GetPendingOverrideReservationsAsync() =>
            await _context.Reservations
                .Where(r => r.IsAdminOverride && r.Status == ReservationStatus.PendingApproval)
                .ToListAsync();

        /// <summary>
        /// Gets all reservations requiring reschedule due to admin override.
        /// </summary>
        public async Task<List<Reservation>> GetRequiringRescheduleAsync() =>
            await _context.Reservations
                .Include(r => r.Resource)
                .Include(r => r.Event)
                .Where(r => r.Status == ReservationStatus.OverrideRequired)
                .ToListAsync();

        /// <summary>
        /// Gets all external user (non-admin) reservations that overlap with the specified time range,
        /// including their buffer zones.
        /// </summary>
        public async Task<List<Reservation>> GetExternalUserConflictingReservationsAsync(int resourceId, DateTime start, DateTime end)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                    .ThenInclude(e => e.Organizer)
                .Where(r => r.ResourceId == resourceId &&
                           r.Status != ReservationStatus.Cancelled &&
                           r.Status != ReservationStatus.OverrideRequired &&
                           r.Event != null &&
                           // Include buffer: check if overlap exists with buffer zones
                           // Compute buffer inline for EF Core translation: StartTime - 1 day, EndTime + 1 day
                           r.StartTime.AddDays(-1) < end &&
                           r.EndTime.AddDays(1) > start &&
                           // Only include reservations from non-admin users
                           (r.Event.Organizer.Role == UserRole.Member || 
                            r.Event.Organizer.Role == UserRole.External))
                .ToListAsync();
        }

        /// <summary>
        /// Gets all conflicting reservations (admin and non-admin) for a resource in the specified time range.
        /// Used when an admin wants to override and cancel conflicting reservations.
        /// </summary>
        public async Task<List<Reservation>> GetAllConflictingReservationsAsync(int resourceId, DateTime start, DateTime end)
        {
            return await _context.Reservations
                .Include(r => r.Event)
                    .ThenInclude(e => e.Organizer)
                .Where(r => r.ResourceId == resourceId &&
                           r.Status != ReservationStatus.Cancelled &&
                           r.Event != null &&
                           // Include buffer: check if overlap exists with buffer zones
                           // Compute buffer inline for EF Core translation: StartTime - 1 day, EndTime + 1 day
                           r.StartTime.AddDays(-1) < end &&
                           r.EndTime.AddDays(1) > start)
                .ToListAsync();
        }
    }
}
