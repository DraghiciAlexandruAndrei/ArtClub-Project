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
    }
}