using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IInvitationService
    {
        Task<bool> SendInvitationAsync(int eventId, int inviteeId);
        Task<List<Invitation>> GetUserInboxAsync(int userId);
        Task<bool> AcceptInvitationAsync(int id);
        Task<bool> DeclineInvitationAsync(int id);
        Task<bool> IsAlreadyInvitedAsync(int eventId, int inviteeId);


    }
}