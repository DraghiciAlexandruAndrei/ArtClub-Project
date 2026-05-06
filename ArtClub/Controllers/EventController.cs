using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArtClub.Attributes;

namespace ArtClub.Controllers
{
    public class EventController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IArtPieceService _artPieceService;

        public EventController(
            IEventService eventService,
            IArtPieceService artPieceService)
        {
            _eventService = eventService;
            _artPieceService = artPieceService;
        }

        public async Task<IActionResult> Index()
        {
            var events = await _eventService.GetAllEventsAsync();

            var model = events.Select(e => new EventSummaryViewModel
            {
                Title = e.Title,
                OrganizerName = e.Organizer != null ? e.Organizer.UserName : "Unknown organizer",
                ResourceName = e.Resource != null ? e.Resource.Name : "No resource",
                Status = e.Reservation != null && e.Reservation.StartTime > DateTime.Now
                    ? "Scheduled"
                    : "Completed",
                StartDate = e.Reservation != null ? e.Reservation.StartTime : DateTime.Now,
                InviteCount = e.Invitations != null ? e.Invitations.Count : 0
            }).ToList();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var start = DateTime.Now.AddDays(1);
            start = new DateTime(start.Year, start.Month, start.Day, 10, 0, 0);

            var resources = await _eventService.GetAllResourcesAsync();
            await PopulateArtPiecesViewBag();

            var model = new EventCreateViewModel
            {
                StartDate = start,
                EndDate = start.AddHours(2),
                AvailableResources = resources.Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            // Verificăm ModelState (SelectedArtPieceIds este acum în model)
            if (!ModelState.IsValid)
            {
                await PopulateArtPiecesViewBag();
                var resources = await _eventService.GetAllResourcesAsync();
                model.AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
                return View(model);
            }

            var organizerId = HttpContext.Session.GetInt32("UserId");
            if (organizerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var resource = await _eventService.GetResourceByNameAsync(model.ResourceName);
            if (resource == null)
            {
                ModelState.AddModelError("ResourceName", "Sala selectată nu a fost găsită.");
                await PopulateArtPiecesViewBag();
                return View(model);
            }

            // Mapăm manual Datele către Entitate
            var ev = new Event
            {
                Title = model.Title,
                Description = model.Description,
                ResourceId = resource.Id,
                OrganizerId = organizerId.Value,
                Budget = 0, // Va fi calculat automat în EventService.CreateEventAsync
                Reservation = new Reservation
                {
                    ResourceId = resource.Id,
                    StartTime = model.StartDate,
                    EndTime = model.EndDate
                },
                // Legăm piesele de artă selectate
                EventArtPieces = model.SelectedArtPieceIds?.Select(id => new EventArtPiece
                {
                    ArtPieceId = id
                }).ToList() ?? new List<EventArtPiece>()
            };

            var success = await _eventService.CreateEventAsync(ev);

            if (!success)
            {
                ModelState.AddModelError("", "Eșec: Ai atins limita de evenimente sau sala este ocupată.");
                await PopulateArtPiecesViewBag();
                var resources = await _eventService.GetAllResourcesAsync();
                model.AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
                return View(model);
            }

            TempData["StatusMessage"] = "Eveniment creat! Finalizează plata pentru confirmare.";
            // Redirecționăm către Details pentru a vedea costul calculat și a plăti
            return RedirectToAction(nameof(Details), new { title = ev.Title });
        }

        public async Task<IActionResult> Details(string title)
        {
            if (string.IsNullOrEmpty(title)) return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);
            if (ev == null) return NotFound();

            var members = await _eventService.GetAllMembersAsync();
            ViewBag.Users = members;

            var model = new EventDetailsViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                OrganizerName = ev.Organizer != null ? ev.Organizer.UserName : "Unknown organizer",
                ResourceName = ev.Resource != null ? ev.Resource.Name : "No resource",
                Date = ev.Reservation != null ? ev.Reservation.StartTime : DateTime.Now,
                AttendingCount = ev.Invitations?.Count(i => i.Status == InvitationStatus.Accepted) ?? 0,

                // Populăm numele pieselor de artă pentru afișare
                ArtPieceNames = ev.EventArtPieces?.Select(eap => eap.ArtPiece.Title).ToList() ?? new List<string>(),

                // Trimitem costul calculat în Service către View
                TotalCost = ev.Budget,

                Invitations = ev.Invitations?.ToList() ?? new List<Invitation>()
            };
            ViewBag.Users = await _eventService.GetAllMembersAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);
            if (ev == null) return NotFound();

            await PopulateArtPiecesViewBag();
            var resources = await _eventService.GetAllResourcesAsync();

            ViewBag.OriginalTitle = ev.Title;

            var model = new EventCreateViewModel
            {
                Title = ev.Title,
                Description = ev.Description,
                StartDate = ev.Reservation?.StartTime ?? DateTime.Now,
                EndDate = ev.Reservation?.EndTime ?? DateTime.Now.AddHours(1),
                ResourceName = ev.Resource?.Name,
                // Pre-selectăm piesele de artă existente
                SelectedArtPieceIds = ev.EventArtPieces?.Select(eap => eap.ArtPieceId).ToList() ?? new List<int>(),
                AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string originalTitle, EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.OriginalTitle = originalTitle;
                await PopulateArtPiecesViewBag();
                return View(model);
            }

            var resource = await _eventService.GetResourceByNameAsync(model.ResourceName);
            if (resource == null)
            {
                ModelState.AddModelError("ResourceName", "Locația nu a fost găsită.");
                return View(model);
            }

            var ev = new Event
            {
                Title = model.Title,
                Description = model.Description,
                ResourceId = resource.Id,
                Reservation = new Reservation
                {
                    ResourceId = resource.Id,
                    StartTime = model.StartDate,
                    EndTime = model.EndDate
                },
                EventArtPieces = model.SelectedArtPieceIds?.Select(id => new EventArtPiece
                {
                    ArtPieceId = id
                }).ToList() ?? new List<EventArtPiece>()
            };

            var success = await _eventService.UpdateEventAsync(originalTitle, ev);
            if (!success) return NotFound();

            TempData["StatusMessage"] = "Eveniment actualizat cu succes.";
            return RedirectToAction(nameof(Details), new { title = ev.Title });
        }

        public async Task<IActionResult> Delete(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);
            if (ev == null) return NotFound();

            var model = new EventDetailsViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                OrganizerName = ev.Organizer?.UserName ?? "Unknown",
                ResourceName = ev.Resource?.Name ?? "No resource",
                Date = ev.Reservation?.StartTime ?? DateTime.Now
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string title)
        {
            var success = await _eventService.DeleteEventByTitleAsync(title);
            if (!success) return NotFound();

            TempData["StatusMessage"] = "Eveniment șters.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateArtPiecesViewBag()
        {
            var artPieces = await _artPieceService.GetAllArtPiecesAsync();
            ViewBag.ArtPiecesList = artPieces.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Title
            }).ToList();
        }
    }
}