using ArtClub.Models.Entities;
using ArtClub.Models.ViewModels;
using ArtClub.Models.ViewModels.Resource;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class ResourceController : Controller
    {
        private readonly IReservationService _reservationService;

        public ResourceController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        public async Task<IActionResult> Index(string? searchTerm)
        {
            var resources = await _reservationService.GetAllResourcesAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                resources = resources.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(term)) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)) ||
                    r.Capacity.ToString().Contains(term)).ToList();
            }

            ViewBag.SearchTerm = searchTerm;

            var model = resources.Select(r => new ResourceOverviewViewModel
            {
                Name = r.Name,
                Type = r.Description,
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

        public async Task<IActionResult> Details(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);

            if (resource == null)
                return NotFound();

            return View(resource);
        }

        public IActionResult Create()
        {
            return View(new ResourceCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resource = new Resource
            {
                Name = model.Name,
                Description = model.Type,
                Capacity = model.Capacity,
                BasePrice = 0
            };

            await _reservationService.CreateResourceAsync(resource);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);

            if (resource == null)
                return NotFound();

            return View(resource);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string originalName, Resource model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var success = await _reservationService.UpdateResourceAsync(originalName, model);

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

        public async Task<IActionResult> ReservationDatesReport(string name, DateTime? startDate, DateTime? endDate)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return NotFound();
            }

            var resource = await _reservationService.GetResourceByNameAsync(name);
            if (resource == null)
            {
                return NotFound();
            }

            var start = startDate ?? DateTime.Now.AddMonths(-1);
            var end = endDate ?? DateTime.Now.AddMonths(1);

            var reservations = resource.Reservations
                .Where(r => r.StartTime < end && r.EndTime > start && r.Status != Models.Enums.ReservationStatus.Cancelled)
                .OrderBy(r => r.StartTime)
                .ToList();

            var vm = new ResourceReservationDatesReportViewModel
            {
                ResourceName = resource.Name,
                StartDate = start,
                EndDate = end,
                Reservations = reservations
            };

            return View(vm);
        }
    }
}