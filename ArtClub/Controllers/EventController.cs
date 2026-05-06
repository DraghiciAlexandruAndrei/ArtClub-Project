using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Services;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtClub.Controllers
{
    [Authorize] // Protejează toate acțiunile din acest controller
    public class EventController : Controller
    {
        private readonly IEventService _eventService;
        private readonly IArtPieceService _artPieceService;
        private readonly UserManager<User> _userManager;
        private readonly IInvitationService _invitationService;
        public EventController(
            IEventService eventService,
            IArtPieceService artPieceService,
            UserManager<User> userManager,
            IInvitationService invitationService)
        {
            _eventService = eventService;
            _artPieceService = artPieceService;
            _userManager = userManager;
            _invitationService = invitationService;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var events = await _eventService.GetAllEventsAsync();

            var model = events.Select(e => new EventSummaryViewModel
            {
                Title = e.Title,
                OrganizerName = e.Organizer?.UserName ?? "Unknown organizer",
                ResourceName = e.Resource?.Name ?? "No resource",
                Status = e.Reservation != null && e.Reservation.StartTime > DateTime.Now
                    ? "Scheduled"
                    : "Completed",
                StartDate = e.Reservation?.StartTime ?? DateTime.Now,
                InviteCount = e.Invitations?.Count ?? 0
            }).ToList();

            return View(model);
        }

        // GET: Event/Details/Titlu
        [Authorize] // Require authentication to view event details
        public async Task<IActionResult> Details(string title)
        {
            if (string.IsNullOrEmpty(title)) return NotFound();

            // Preluăm evenimentul cu toate datele incluse (Organizer, Resource, Invitations, ArtPieces)
            var ev = await _eventService.GetEventByTitleAsync(title);
            if (ev == null) return NotFound();

            var model = new EventDetailsViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                Description = ev.Description,
                OrganizerName = ev.Organizer?.UserName ?? "Unknown organizer",
                ResourceName = ev.Resource?.Name ?? "No resource",
                Date = ev.Reservation?.StartTime ?? DateTime.Now,
                AttendingCount = ev.Invitations?.Count(i => i.Status == InvitationStatus.Accepted) ?? 0,
                ArtPieceNames = ev.EventArtPieces?.Select(eap => eap.ArtPiece.Title).ToList() ?? new List<string>(),
                TotalCost = ev.Budget,
                Invitations = ev.Invitations?.ToList() ?? new List<Invitation>()
            };

            // Luăm toți membrii
            var allMembers = await _eventService.GetAllMembersAsync();

            // FILTRARE: Excludem organizatorul din listă ca să nu apară în dropdown-ul de invitații
            ViewBag.Users = allMembers.Where(u => u.Id != ev.OrganizerId).ToList();

            return View(model);
        }

        // GET: Event/Create
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

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            // Eliminăm validările pentru proprietățile care nu vin din formular
            ModelState.Remove("AvailableResources");

            if (!ModelState.IsValid)
            {
                await PopulateArtPiecesViewBag();
                var resources = await _eventService.GetAllResourcesAsync();
                model.AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
                return View(model);
            }

            // Preluăm utilizatorul logat prin Identity
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Forțează re-logarea dacă user-ul nu e găsit

            var resource = await _eventService.GetResourceByNameAsync(model.ResourceName);
            if (resource == null)
            {
                ModelState.AddModelError("ResourceName", "Sala selectată nu a fost găsită.");
                await PopulateArtPiecesViewBag();
                return View(model);
            }

            // Construim entitatea Event
            var ev = new Event
            {
                Title = model.Title,
                Description = model.Description,
                ResourceId = resource.Id,
                OrganizerId = user.Id,
                Budget = 0, // Calculat în Service
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

            var success = await _eventService.CreateEventAsync(ev);

            if (!success)
            {
                ModelState.AddModelError("", "Eroare: Verifică dacă sala este disponibilă sau dacă ai atins limita de evenimente.");
                await PopulateArtPiecesViewBag();
                var resources = await _eventService.GetAllResourcesAsync();
                model.AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList();
                return View(model);
            }

            TempData["StatusMessage"] = "Eveniment creat cu succes!";
            return RedirectToAction(nameof(Details), new { title = ev.Title });
        }

        // GET: Event/Edit/Titlu
        [HttpGet]
        public async Task<IActionResult> Edit(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);
            if (ev == null) return NotFound();

            // Verificare: doar proprietarul sau Admin-ul poate edita
            var currentUser = await _userManager.GetUserAsync(User);
            if (ev.OrganizerId != currentUser.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await PopulateArtPiecesViewBag();
            var resources = await _eventService.GetAllResourcesAsync();
            ViewBag.OriginalTitle = ev.Title;

            var model = new EventCreateViewModel
            {
                Title = ev.Title,
                Description = ev.Description,
                StartDate = ev.Reservation?.StartTime ?? DateTime.Now,
                EndDate = ev.Reservation?.EndTime ?? DateTime.Now.AddHours(2),
                ResourceName = ev.Resource?.Name,
                SelectedArtPieceIds = ev.EventArtPieces?.Select(eap => eap.ArtPieceId).ToList() ?? new List<int>(),
                AvailableResources = resources.Select(r => new SelectListItem { Value = r.Name, Text = r.Name }).ToList()
            };

            return View(model);
        }

        // POST: Event/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string originalTitle, EventCreateViewModel model)
        {
            ModelState.Remove("AvailableResources");

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
            if (!success) return BadRequest("Actualizarea a eșuat.");

            TempData["StatusMessage"] = "Eveniment actualizat cu succes.";
            return RedirectToAction(nameof(Details), new { title = ev.Title });
        }

      
        // POST: Event/Delete
        // GET: Event/Delete/TitluEveniment
        [HttpGet]
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

        // POST: Event/Delete
        [HttpPost, ActionName("Delete")] // IMPORTANTE: Această linie spune că deși metoda se numește DeleteConfirmed, ea răspunde la acțiunea "Delete"
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string title) // Parametrul trebuie să se numească EXACT ca 'name' din input-ul hidden
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest();

            var success = await _eventService.DeleteEventByTitleAsync(title);

            if (!success)
            {
                TempData["ErrorMessage"] = "Evenimentul nu a putut fi șters (posibil să aibă rezervări active).";
                return RedirectToAction(nameof(Index));
            }

            TempData["StatusMessage"] = "Evenimentul a fost șters cu succes!";
            return RedirectToAction(nameof(Index));
        }

        // Ajutor pentru popularea listei de piese de artă
        private async Task PopulateArtPiecesViewBag()
        {
            var artPieces = await _artPieceService.GetAllArtPiecesAsync();
            ViewBag.ArtPiecesList = artPieces.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = a.Title
            }).ToList();
        }

        //apelam invitatiile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvitation(int eventId, int inviteeId, string eventTitle)
        {
            // Verificăm dacă utilizatorul este deja invitat
            var alreadyInvited = await _invitationService.IsAlreadyInvitedAsync(eventId, inviteeId);

            if (alreadyInvited)
            {
                TempData["ErrorMessage"] = "Acest utilizator a primit deja o invitație pentru acest eveniment.";
                return RedirectToAction(nameof(Details), new { title = eventTitle });
            }

            var success = await _invitationService.SendInvitationAsync(eventId, inviteeId);

            if (success)
            {
                TempData["StatusMessage"] = "Invitația a fost trimisă cu succes!";
            }
            else
            {
                TempData["ErrorMessage"] = "Eroare la trimiterea invitației. Verifică dacă utilizatorul există.";
            }

            // Ne întoarcem la pagina de detalii a evenimentului
            return RedirectToAction(nameof(Details), new { title = eventTitle });
        }
    }
}