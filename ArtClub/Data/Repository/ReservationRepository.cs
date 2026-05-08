using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.DataAccess.Repositories
{
    public class ReservationRepository : IReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public ReservationRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> HasOverlappingReservationAsync(int resourceId, DateTime start, DateTime end)
        {
            // Verificăm strict dacă intervalul [start, end] (care include deja buffer-ul)
            // se intersectează cu orice rezervare existentă.
            return await _context.Reservations
                .AnyAsync(r => r.ResourceId == resourceId &&
                               r.StartTime < end &&
                               r.EndTime > start);
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
    }
}