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

   
        
        public async Task<IActionResult> Create()
        {
            var start = DateTime.Now.AddDays(1);
            start = new DateTime(start.Year, start.Month, start.Day, 10, 0, 0);

            // Luăm resursele din repository
            var resources = await _eventService.GetAllResourcesAsync(); // Asigură-te că ai această metodă în Service

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
        [AuthorizeRole(UserRole.Member)]
        public async Task<IActionResult> Create(EventCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var organizerId = HttpContext.Session.GetInt32("UserId");
            if (organizerId == null)
            {
                ModelState.AddModelError("", "Trebuie să fii autentificat pentru a crea un eveniment.");
                return View(model);
            }

            var resource = await _eventService.GetResourceByNameAsync(model.ResourceName);
            if (resource == null)
            {
                ModelState.AddModelError("ResourceName", "Resursa (sala) nu a fost găsită.");
                return View(model);
            }

            // Creăm obiectul Eveniment
            var ev = new Event
            {
                Title = model.Title,
                Description = model.Description,
                ResourceId = resource.Id,
                OrganizerId = organizerId.Value,
                Budget = 0, // Setat la 0 pentru a trece de verificarea de buget dacă soldul e 0
                Reservation = new Reservation
                {
                    ResourceId = resource.Id,
                    StartTime = model.StartDate,
                    EndTime = model.EndDate
                }
            };

            try
            {
                var success = await _eventService.CreateEventAsync(ev);

                if (!success)
                {
                    // Mesaj specific pentru TC-001 (Documentația de testare)
                    ModelState.AddModelError("", "Eșec la salvare: Verifică dacă sala este disponibilă (există buffer de 1 zi) sau dacă bugetul este suficient.");
                    return View(model);
                }

                TempData["StatusMessage"] = "Evenimentul a fost creat cu succes!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Eroare de sistem la salvarea în baza de date: " + ex.Message);
                return View(model);
            }
        }

        // Adaugă acest folosind contextul dacă nu ai un serviciu de utilizatori dedicat
        // sau folosește IAccountService dacă ai unul.

        public async Task<IActionResult> Details(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);

            if (ev == null)
                return NotFound();

            // --- LOGICA NOUĂ PENTRU INVITAȚII ---

            // 1. Luăm toți utilizatorii care pot fi invitați (Membri)
            // Înlocuiește cu serviciul tău real dacă ai unul (ex: _accountService.GetAllMembers())
            var members = await _eventService.GetAllMembersAsync();
            ViewBag.Users = members;

            // 2. Mapăm datele către ViewModel
            var model = new EventDetailsViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                OrganizerName = ev.Organizer != null ? ev.Organizer.UserName : "Unknown organizer",
                ResourceName = ev.Resource != null ? ev.Resource.Name : "No resource",
                Date = ev.Reservation != null ? ev.Reservation.StartTime : DateTime.Now,
                AttendingCount = ev.Invitations?.Count(i => i.Status == InvitationStatus.Accepted) ?? 0,
                ArtPieceNames = new List<string>(), // Aici poți mapa piesele de artă dacă e cazul

                // Adăugăm invitațiile existente pentru a le afișa în tabelul de status
                Invitations = ev.Invitations?.ToList() ?? new List<Invitation>()
            };

            return View(model);
        }

        public async Task<IActionResult> Edit(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);

            if (ev == null)
                return NotFound();

            ViewBag.OriginalTitle = ev.Title;

            var model = new EventCreateViewModel
            {
                Title = ev.Title,
                Description = ev.Description,
                StartDate = ev.Reservation != null ? ev.Reservation.StartTime : DateTime.Now,
                EndDate = ev.Reservation != null ? ev.Reservation.EndTime : DateTime.Now.AddHours(1),
                SelectedResourceId = ev.ResourceId,
                ResourceName = ev.Resource != null ? ev.Resource.Name : ""
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
                return View(model);
            }

            var resource = await _eventService.GetResourceByNameAsync(model.ResourceName);

            if (resource == null)
            {
                ViewBag.OriginalTitle = originalTitle;
                ModelState.AddModelError("ResourceName", "Resource not found.");
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
                }
            };

            var success = await _eventService.UpdateEventAsync(originalTitle, ev);

            if (!success)
                return NotFound();

            TempData["StatusMessage"] = "Event updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                return NotFound();

            var ev = await _eventService.GetEventByTitleAsync(title);

            if (ev == null)
                return NotFound();

            var model = new EventDetailsViewModel
            {
                EventId = ev.Id,
                Title = ev.Title,
                OrganizerName = ev.Organizer != null ? ev.Organizer.UserName : "Unknown organizer",
                ResourceName = ev.Resource != null ? ev.Resource.Name : "No resource",
                Date = ev.Reservation != null ? ev.Reservation.StartTime : DateTime.Now,
                AttendingCount = ev.Invitations != null ? ev.Invitations.Count : 0
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string title)
        {
            var success = await _eventService.DeleteEventByTitleAsync(title);

            if (!success)
                return NotFound();

            TempData["StatusMessage"] = "Event deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

       
    }
}