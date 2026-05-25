using ArtClub.Models.Entities;

public interface IInvitationRepository
{
    Task<Invitation> GetByIdAsync(int id);
    Task<List<Invitation>> GetByInviteeIdAsync(int userId);
    Task<bool> AddAsync(Invitation invitation);
    Task<bool> UpdateAsync(Invitation invitation);
    Task<bool> ExistsAsync(int eventId, int inviteeId);
}