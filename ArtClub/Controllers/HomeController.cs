using ArtClub.Models;
using ArtClub.Models.Entities;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ArtClub.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IArtPieceService _artPieceService;
        private readonly SignInManager<User> _signInManager;

        // Injectăm serviciile necesare în constructor
        public HomeController(
            IEventService eventService,
            IArtPieceService artPieceService,
            SignInManager<User> signInManager)
        {
            _eventService = eventService;
            _artPieceService = artPieceService;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            // Dacă utilizatorul este autentificat, încărcăm datele pentru Dashboard
            if (_signInManager.IsSignedIn(User))
            {
                // 1. Preluăm toate evenimentele și le filtrăm pe cele active/viitoare
                var allEvents = await _eventService.GetAllEventsAsync();
                ViewBag.ActiveEvents = allEvents
                    .Where(e => e.Reservation != null && e.Reservation.EndTime > DateTime.Now)
                    .OrderBy(e => e.Reservation.StartTime)
                    .Take(4) // Limităm la top 4 evenimente pentru un aspect curat în interfață
                    .ToList();

                // 2. Preluăm piesele de artă populare
                var allArtPieces = await _artPieceService.GetAllArtPiecesAsync();
                ViewBag.PopularArtPieces = allArtPieces
                    .Where(a => a.IsPopular)
                    .Take(4) // Limităm la top 4 piese în panou
                    .ToList();
            }

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}