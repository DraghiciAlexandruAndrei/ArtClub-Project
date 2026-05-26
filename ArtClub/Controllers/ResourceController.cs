using ArtClub.Models.Entities;
using ArtClub.Models.ViewModels.Resource;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    [Authorize] // Protejează controller-ul; necesită autentificare pentru orice acțiune
    public class ResourceController : Controller
    {
        private readonly IReservationService _reservationService;

        public ResourceController(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [AllowAnonymous] // Permite vizualizarea resurselor și fără logare (opțional)
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
                Status = r.Reservations.Any(res => res.StartTime.AddDays(-1) <= DateTime.Now && res.EndTime.AddDays(1) >= DateTime.Now)
                    ? "Reserved" : "Available"
            }).ToList();

            return View(model);
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);
            return resource == null ? NotFound() : View(resource);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View(new ResourceCreateViewModel());

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            await _reservationService.CreateResourceAsync(new Resource
            {
                Name = model.Name,
                Description = model.Type,
                Capacity = model.Capacity,
                BasePrice = 0
            });

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);
            return resource == null ? NotFound() : View(resource);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string originalName, Resource model)
        {
            if (!ModelState.IsValid) return View(model);
            if (!await _reservationService.UpdateResourceAsync(originalName, model)) return NotFound();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(string name)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);
            return resource == null ? NotFound() : View(resource);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string name)
        {
            if (!await _reservationService.DeleteResourceAsync(name)) return NotFound();
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public async Task<IActionResult> Calendar() => View(await _reservationService.GetReservationCalendarAsync());

        [AllowAnonymous]
        public async Task<IActionResult> ReservationDatesReport(string name, DateTime? startDate, DateTime? endDate)
        {
            var resource = await _reservationService.GetResourceByNameAsync(name);
            if (resource == null) return NotFound();

            var start = startDate ?? DateTime.Now.AddMonths(-1);
            var end = endDate ?? DateTime.Now.AddMonths(1);

            return View(new ResourceReservationDatesReportViewModel
            {
                ResourceName = resource.Name,
                StartDate = start,
                EndDate = end,
                Reservations = resource.Reservations
                    .Where(r => r.StartTime < end && r.EndTime > start && r.Status != Models.Enums.ReservationStatus.Cancelled)
                    .OrderBy(r => r.StartTime).ToList()
            });
        }
    }
}