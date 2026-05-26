using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IArtPieceService
    {
        Task<List<ArtPiece>> GetAllArtPiecesAsync();
        Task<List<ArtPiece>> GetPopularPiecesAsync();
        Task<ArtPiece?> GetArtPieceByIdAsync(int id);
        Task CreateArtPieceAsync(ArtPiece artPiece);
        Task<bool> UpdateArtPieceAsync(ArtPiece artPiece);
        Task<bool> DeleteArtPieceAsync(int id);

        Task AddArtPieceToEventAsync(int eventId, int artPieceId);
        Task<List<ArtPiece>> GetAvailablePiecesForEventAsync(int eventId);
    }
}