using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IEventService
    {
        Task<bool> CreateEventAsync(Event model);
        Task<bool> CancelEventAsync(int eventId);
        Task SendInvitationAsync(int eventId, int inviteeId);
        Task RespondToInvitationAsync(int invitationId, bool accept);

        Task<List<Event>> GetAllEventsAsync();
        Task<Event?> GetEventByTitleAsync(string title);
        Task<bool> UpdateEventAsync(string originalTitle, Event model);
        Task<bool> DeleteEventByTitleAsync(string title);
        Task<Resource?> GetResourceByNameAsync(string resourceName);

        Task<int?> GetDefaultOrganizerIdAsync();

        Task<List<Resource>> GetAllResourcesAsync();

        Task<List<User>> GetAllMembersAsync();
    }
}