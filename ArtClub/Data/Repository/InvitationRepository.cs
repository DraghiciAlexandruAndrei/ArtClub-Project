using ArtClub.DataAccess;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using Microsoft.EntityFrameworkCore;

public class InvitationRepository : IInvitationRepository
{
    private readonly ApplicationDbContext _context;
    public InvitationRepository(ApplicationDbContext context) => _context = context;

    public async Task<Invitation> GetByIdAsync(int id) =>
        await _context.Invitations.Include(i => i.Event).FirstOrDefaultAsync(i => i.Id == id);

    public async Task<List<Invitation>> GetByInviteeIdAsync(int userId)
    {
        return await _context.Invitations
            .Include(i => i.Event)
            .Where(i => i.InviteeId == userId && i.Status == InvitationStatus.Pending) // Folosim Enum-ul aici
            .ToListAsync();
    }

    public async Task<bool> AddAsync(Invitation invitation)
    {
        await _context.Invitations.AddAsync(invitation);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAsync(Invitation invitation)
    {
        _context.Invitations.Update(invitation);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> ExistsAsync(int eventId, int inviteeId) =>
        await _context.Invitations.AnyAsync(i => i.EventId == eventId && i.InviteeId == inviteeId);
}