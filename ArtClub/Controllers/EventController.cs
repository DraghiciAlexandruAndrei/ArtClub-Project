using System.Collections.Generic;
using ArtClub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class EventController : Controller
    {
        public IActionResult Index()
        {
            var model = new List<EventSummaryViewModel>
            {
                new EventSummaryViewModel { Title = "Spring Exhibition", OrganizerName = "Lorena Dobre", ResourceName = "Main Hall", Status = "Scheduled", StartDate = DateTime.Today.AddDays(7), InviteCount = 14 },
                new EventSummaryViewModel { Title = "Watercolor Workshop", OrganizerName = "Alexandru Draghici", ResourceName = "Studio 2", Status = "Pending", StartDate = DateTime.Today.AddDays(14), InviteCount = 9 },
                new EventSummaryViewModel { Title = "Art Talk", OrganizerName = "Gabriel Gherasie", ResourceName = "Conference Room", Status = "Confirmed", StartDate = DateTime.Today.AddDays(21), InviteCount = 18 }
            };

            return View(model);
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(EventCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            TempData["StatusMessage"] = "Event created and linked to a reserved resource.";
            return RedirectToAction(nameof(Details), new { id = 1 });
        }

        public IActionResult Details(int id)
        {
            var model = new EventDetailsViewModel
            {
                EventId = id,
                Title = "Spring Exhibition",
                OrganizerName = "Lorena Dobre",
                ResourceName = "Main Hall",
                Date = DateTime.Today.AddDays(7),
                ArtPieceNames = new List<string> { "Sunlit Studio", "Modern Shapes" },
                AttendingCount = 14
            };

            return View(model);
        }

        public IActionResult Inbox()
        {
            var model = new List<InvitationInboxViewModel>
            {
                new InvitationInboxViewModel { EventTitle = "Spring Exhibition", SenderName = "Lorena Dobre", Status = "Pending", SentAt = DateTime.Today.AddDays(-1) },
                new InvitationInboxViewModel { EventTitle = "Art Talk", SenderName = "Gabriel Gherasie", Status = "Accepted", SentAt = DateTime.Today.AddDays(-2) },
                new InvitationInboxViewModel { EventTitle = "Watercolor Workshop", SenderName = "Alexandru Draghici", Status = "Declined", SentAt = DateTime.Today.AddDays(-3) }
            };

            return View(model);
        }

        public IActionResult Edit(int id) => View();

        public IActionResult Delete(int id) => View();

        public IActionResult Cancel(int id)
        {
            TempData["StatusMessage"] = "Event cancellation completed.";
            return RedirectToAction(nameof(Index));
        }
    }
}