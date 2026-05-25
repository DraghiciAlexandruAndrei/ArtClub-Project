using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.DataAccess.Repositories
{
    public class ArtPieceRepository : IArtPieceRepository
    {
        private readonly ApplicationDbContext _context;

        public ArtPieceRepository(ApplicationDbContext context) => _context = context;

        public async Task<List<ArtPiece>> GetAllAsync()
        {
            return await _context.ArtPieces.OrderBy(ap => ap.Title).ToListAsync();
        }

        public async Task<List<ArtPiece>> GetPopularAsync()
        {
            return await _context.ArtPieces.Where(ap => ap.IsPopular).OrderBy(ap => ap.Title).ToListAsync();
        }

        public async Task<ArtPiece?> GetByIdAsync(int id)
        {
            return await _context.ArtPieces.FirstOrDefaultAsync(ap => ap.Id == id);
        }

        public async Task AddAsync(ArtPiece artPiece)
        {
            _context.ArtPieces.Add(artPiece);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UpdateAsync(ArtPiece artPiece)
        {
            var existing = await GetByIdAsync(artPiece.Id);
            if (existing == null) return false;

            existing.Title = artPiece.Title;
            existing.Creator = artPiece.Creator;
            existing.ImageUrl = artPiece.ImageUrl;
            existing.IsPopular = artPiece.IsPopular;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var artPiece = await GetByIdAsync(id);
            if (artPiece == null) return false;

            _context.ArtPieces.Remove(artPiece);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddToEventAsync(int eventId, int artPieceId)
        {
            _context.EventArtPieces.Add(new EventArtPiece { EventId = eventId, ArtPieceId = artPieceId });
            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetAssignedIdsForEventAsync(int eventId)
        {
            return await _context.EventArtPieces.Where(eap => eap.EventId == eventId).Select(eap => eap.ArtPieceId).ToListAsync();
        }
    }
}