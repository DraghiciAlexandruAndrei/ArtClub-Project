using ArtClub.Models.Entities;

namespace ArtClub.DataAccess.Interfaces
{
    public interface IArtPieceRepository
    {
        Task<List<ArtPiece>> GetAllAsync();
        Task<List<ArtPiece>> GetPopularAsync();
        Task<ArtPiece?> GetByIdAsync(int id);
        Task AddAsync(ArtPiece artPiece);
        Task<bool> UpdateAsync(ArtPiece artPiece);
        Task<bool> DeleteAsync(int id);
        Task AddToEventAsync(int eventId, int artPieceId);
        Task<List<int>> GetAssignedIdsForEventAsync(int eventId);
    }
}