using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IReservationService
    {
        Task<bool> CheckAvailabilityAsync(int resourceId, DateTime start, DateTime end);
        Task CreateReservationAsync(Reservation reservation);

        Task<List<Resource>> GetAllResourcesAsync();
        Task<Resource?> GetResourceByNameAsync(string name);
        Task CreateResourceAsync(Resource resource);
        Task<bool> UpdateResourceAsync(string originalName, Resource model);
        Task<bool> DeleteResourceAsync(string name);
        Task<List<Reservation>> GetReservationCalendarAsync();
    }
}