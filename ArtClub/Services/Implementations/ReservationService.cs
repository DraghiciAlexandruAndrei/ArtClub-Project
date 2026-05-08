using ArtClub.Models.Entities;
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

        public async Task<bool> CheckAvailabilityAsync(int resourceId, DateTime start, DateTime end)
        {
            // Aplicăm buffer-ul de o zi doar pentru cererea curentă
            var requestedBufferStart = start.AddDays(-1);
            var requestedBufferEnd = end.AddDays(1);

            // Întrebăm repository-ul dacă acest interval extins lovește vreo rezervare existentă
            var isOverlapping = await _reservationRepo.HasOverlappingReservationAsync(
                resourceId,
                requestedBufferStart,
                requestedBufferEnd);

            return !isOverlapping;
        }

        public async Task CreateReservationAsync(Reservation reservation)
        {
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
    }
}