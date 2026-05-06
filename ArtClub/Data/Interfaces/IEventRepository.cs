using ArtClub.Models.Entities;

namespace ArtClub.DataAccess.Interfaces
{
    public interface IEventRepository
    {
        Task<List<Event>> GetAllWithDetailsAsync();
        Task<Event?> GetByTitleWithDetailsAsync(string title);
        Task<Event?> GetByIdWithReservationAsync(int id);
        Task<Resource?> GetResourceByNameAsync(string name);
        Task<int?> GetFirstUserIdAsync();
        Task AddAsync(Event artEvent);
        Task<bool> SaveChangesAsync();
        void Remove(Event artEvent);

        // Pentru Invitații (Poți face și IInvitationRepository separat)
        Task AddInvitationAsync(Invitation invitation);
        Task<Invitation?> GetInvitationByIdAsync(int id);

        Task<List<Resource>> GetAllResourcesAsync();

        Task<List<Event>> GetByOrganizerIdAsync(string userId);
    }
}