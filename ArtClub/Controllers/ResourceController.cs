using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtClub.Controllers
{
    public class ResourceController : Controller
    {
        private readonly IReservationService _reservationService;

        public ResourceController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        public async Task<IActionResult> Index()
        {
            var resources = await _reservationService.GetAllResourcesAsync();

            var model = resources.Select(r => new ResourceOverviewViewModel
            {
                Name = r.Name,
                Type = GetResourceTypeDisplay(r.Type),
                Capacity = r.Capacity,
                Location = r.Type == ResourceType.ConferenceRoom || r.Type == ResourceType.ExhibitionHall ? "Club venue" : "Equipment",
                Status = r.Reservations.Any(res =>
                    res.StartTime.AddDays(-1) <= DateTime.Now &&
                    res.EndTime.AddDays(1) >= DateTime.Now)
                    ? "Reserved"
                    : "Available",
                QuantityAvailable = r.QuantityAvailable
            }).ToList();

            return View(model);
        }

        // Separate view for venues/rooms only
        public async Task<IActionResult> Venues()
        {
            var resources = await _reservationService.GetAllResourcesAsync();
            var venues = resources.Where(r => r.Type == ResourceType.ConferenceRoom || r.Type == ResourceType.ExhibitionHall);

            var model = venues.Select(r => new ResourceOverviewViewModel
            {
                Name = r.Name,
                Type = GetResourceTypeDisplay(r.Type),
                Capacity = r.Capacity,
                Location = "Club venue",
                Status = r.Reservations.Any(res =>
                    res.StartTime.AddDays(-1) <= DateTime.Now &&
                    res.EndTime.AddDays(1) >= DateTime.Now)
                    ? "Reserved"
                    : "Available"
            }).ToList();

            return View(model);
        }

        // Separate view for equipment/art pieces
        public async Task<IActionResult> Equipment()
        {
            var resources = await _reservationService.GetAllResourcesAsync();
            var equipment = resources.Where(r => r.Type == ResourceType.Equipment);

            var model = equipment.Select(r => new ResourceOverviewViewModel
            {
                Name = r.Name,
                Type = GetResourceTypeDisplay(r.Type),
                Capacity = r.Capacity,
                Location = "Equipment",
                Status = r.Reservations.Any(res =>
                    res.StartTime.AddDays(-1) <= DateTime.Now &&
                    res.EndTime.AddDays(1) >= DateTime.Now)
                    ? "Reserved"
                    : "Available",
                QuantityAvailable = r.QuantityAvailable
            }).ToList();

            return View(model);
        }

        public async Task<IActionResult> Details(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);

            if (resource == null)
                return NotFound();

            return View(resource);
        }

        public IActionResult Create()
        {
            return View(new ResourceCreateViewModel { ResourceTypes = GetResourceTypeSelectList() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResourceTypes = GetResourceTypeSelectList();
                return View(model);
            }

            var resource = new Resource
            {
                Name = model.Name,
                Description = model.Type,
                Capacity = model.Capacity,
                BasePrice = 0,
                Type = (ResourceType)model.ResourceTypeId,
                QuantityAvailable = model.QuantityAvailable > 0 ? model.QuantityAvailable : 1
            };

            await _reservationService.CreateResourceAsync(resource);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);

            if (resource == null)
                return NotFound();

            var viewModel = new ResourceCreateViewModel
            {
                Name = resource.Name,
                Type = resource.Description,
                Capacity = resource.Capacity,
                ResourceTypeId = (int)resource.Type,
                QuantityAvailable = resource.QuantityAvailable,
                ResourceTypes = GetResourceTypeSelectList()
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string originalName, ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.ResourceTypes = GetResourceTypeSelectList();
                return View(model);
            }

            var resource = await _reservationService.GetResourceByNameAsync(originalName);
            if (resource == null)
                return NotFound();

            resource.Name = model.Name;
            resource.Description = model.Type;
            resource.Capacity = model.Capacity;
            resource.Type = (ResourceType)model.ResourceTypeId;
            resource.QuantityAvailable = model.QuantityAvailable > 0 ? model.QuantityAvailable : 1;

            var success = await _reservationService.UpdateResourceAsync(originalName, resource);

            if (!success)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);

            if (resource == null)
                return NotFound();

            return View(resource);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string name)
        {
            var success = await _reservationService.DeleteResourceAsync(name);

            if (!success)
                return NotFound();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Calendar()
        {
            var reservations = await _reservationService.GetReservationCalendarAsync();

            return View(reservations);
        }

        // Helper methods
        private string GetResourceTypeDisplay(ResourceType type)
        {
            return type switch
            {
                ResourceType.ConferenceRoom => "Conference Room",
                ResourceType.ExhibitionHall => "Exhibition Hall",
                ResourceType.Equipment => "Equipment",
                ResourceType.AffiliatedLocation => "Affiliated Location",
                _ => "Unknown"
            };
        }

        private List<SelectListItem> GetResourceTypeSelectList()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "0", Text = "Conference Room" },
                new SelectListItem { Value = "1", Text = "Exhibition Hall" },
                new SelectListItem { Value = "2", Text = "Equipment" },
                new SelectListItem { Value = "3", Text = "Affiliated Location" }
            };
        }
    }
}