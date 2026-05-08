using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using ArtClub.Services.Interfaces;

namespace ArtClub.Services.Implementations
{
    public class ArtPieceService : IArtPieceService
    {
        private readonly IArtPieceRepository _artPieceRepository;

        public ArtPieceService(IArtPieceRepository artPieceRepository)
        {
            _artPieceRepository = artPieceRepository;
        }

        public async Task<List<ArtPiece>> GetAllArtPiecesAsync() => await _artPieceRepository.GetAllAsync();

        public async Task<List<ArtPiece>> GetPopularPiecesAsync() => await _artPieceRepository.GetPopularAsync();

        public async Task<ArtPiece?> GetArtPieceByIdAsync(int id) => await _artPieceRepository.GetByIdAsync(id);

        public async Task CreateArtPieceAsync(ArtPiece artPiece) => await _artPieceRepository.AddAsync(artPiece);

        public async Task<bool> UpdateArtPieceAsync(ArtPiece artPiece) => await _artPieceRepository.UpdateAsync(artPiece);

        public async Task<bool> DeleteArtPieceAsync(int id) => await _artPieceRepository.DeleteAsync(id);

        public async Task AddArtPieceToEventAsync(int eventId, int artPieceId) =>
            await _artPieceRepository.AddToEventAsync(eventId, artPieceId);

        public async Task<List<ArtPiece>> GetAvailablePiecesForEventAsync(int eventId)
        {
            var assignedIds = await _artPieceRepository.GetAssignedIdsForEventAsync(eventId);
            var allPieces = await _artPieceRepository.GetAllAsync();

            return allPieces.Where(ap => !assignedIds.Contains(ap.Id)).ToList();
        }
    }
}