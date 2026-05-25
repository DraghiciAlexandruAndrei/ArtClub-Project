using ArtClub.Models.Entities;
using ArtClub.Models.ViewModels.ArtPiece;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    [Authorize] // Protejează controller-ul implicit
    public class ArtPieceController : Controller
    {
        private readonly IArtPieceService _artPieceService;

        public ArtPieceController(IArtPieceService artPieceService)
        {
            _artPieceService = artPieceService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var pieces = await _artPieceService.GetAllArtPiecesAsync();
            var model = pieces.Select(p => new ArtPieceListViewModel
            {
                Id = p.Id,
                Title = p.Title,
                ArtistName = p.Creator,
                Style = p.IsPopular ? "Popular" : "Standard"
            }).ToList();

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Popular()
        {
            var pieces = await _artPieceService.GetPopularPiecesAsync();
            var model = pieces.Select(p => new ArtPieceListViewModel
            {
                Id = p.Id,
                Title = p.Title,
                ArtistName = p.Creator,
                Style = "Popular"
            }).ToList();

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var piece = await _artPieceService.GetArtPieceByIdAsync(id);
            if (piece == null) return NotFound();

            var model = new ArtPieceDetailsViewModel
            {
                Id = piece.Id,
                Title = piece.Title,
                Creator = piece.Creator,
                Description = piece.ImageUrl,
                AssociatedEvents = new List<string>(),
                LastUpdated = piece.IsPopular ? "Popular piece" : "Standard piece"
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")] // Restricționăm crearea la utilizatori autentificați cu roluri specifice
        public IActionResult Create() => View(new ArtPiece());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ArtPiece model)
        {
            if (!ModelState.IsValid) return View(model);

            await _artPieceService.CreateArtPieceAsync(model);
            TempData["StatusMessage"] = "Art piece created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var piece = await _artPieceService.GetArtPieceByIdAsync(id);
            return piece == null ? NotFound() : View(piece);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ArtPiece model)
        {
            if (!ModelState.IsValid) return View(model);

            var success = await _artPieceService.UpdateArtPieceAsync(model);
            if (!success) return NotFound();

            TempData["StatusMessage"] = "Art piece updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")] // Doar Adminul poate șterge definitiv
        public async Task<IActionResult> Delete(int id)
        {
            var piece = await _artPieceService.GetArtPieceByIdAsync(id);
            return piece == null ? NotFound() : View(piece);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var success = await _artPieceService.DeleteArtPieceAsync(id);
            if (!success) return NotFound();

            TempData["StatusMessage"] = "Art piece deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}