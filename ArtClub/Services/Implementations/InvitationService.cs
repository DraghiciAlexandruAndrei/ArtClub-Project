using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Services.Interfaces;
using ArtClub.DataAccess.Interfaces;

namespace ArtClub.Services
{
    public class InvitationService : IInvitationService
    {
        private readonly IInvitationRepository _invitationRepo;

        public InvitationService(IInvitationRepository invitationRepo)
        {
            _invitationRepo = invitationRepo;
        }

        public async Task<bool> SendInvitationAsync(int eventId, int inviteeId)
        {
            // Validare: Nu trimitem invitație dacă utilizatorul este deja invitat
            if (await IsAlreadyInvitedAsync(eventId, inviteeId))
                return false;

            var invitation = new Invitation
            {
                EventId = eventId,
                InviteeId = inviteeId,
                Status = InvitationStatus.Pending
            };

            return await _invitationRepo.AddAsync(invitation);
        }

        public async Task<List<Invitation>> GetUserInboxAsync(int userId)
        {
            // Returnează doar invitațiile Pending pentru inbox-ul utilizatorului
            return await _invitationRepo.GetByInviteeIdAsync(userId);
        }

        public async Task<bool> AcceptInvitationAsync(int id)
        {
            var invitation = await _invitationRepo.GetByIdAsync(id);
            if (invitation == null) return false;

            invitation.Accept(); // Folosește metoda ta: Status = Accepted
            return await _invitationRepo.UpdateAsync(invitation);
        }

        public async Task<bool> DeclineInvitationAsync(int id)
        {
            var invitation = await _invitationRepo.GetByIdAsync(id);
            if (invitation == null) return false;

            invitation.Decline(); // Folosește metoda ta: Status = Declined
            return await _invitationRepo.UpdateAsync(invitation);
        }

        public async Task<bool> IsAlreadyInvitedAsync(int eventId, int inviteeId)
        {
            return await _invitationRepo.ExistsAsync(eventId, inviteeId);
        }
    }
}