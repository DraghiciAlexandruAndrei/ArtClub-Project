using System.Collections.Generic;
using ArtClub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class ResourceController : Controller
    {
        public IActionResult Index()
        {
            var model = new List<ResourceOverviewViewModel>
            {
                new ResourceOverviewViewModel { Name = "Main Hall", Type = "Exhibition", Capacity = 120, Location = "Ground Floor", Status = "Reserved" },
                new ResourceOverviewViewModel { Name = "Conference Room", Type = "Meeting", Capacity = 30, Location = "Second Floor", Status = "Available" },
                new ResourceOverviewViewModel { Name = "Studio 2", Type = "Workshop", Capacity = 18, Location = "Annex Building", Status = "Unavailable" }
            };

            return View(model);
        }

        public IActionResult Calendar()
        {
            return View();
        }

        public IActionResult Create() => View();

        [HttpPost]
        public IActionResult Create(ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            TempData["StatusMessage"] = "Resource created.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Edit(int id) => View();

        [HttpPost]
        public IActionResult Edit(int id, ResourceCreateViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            TempData["StatusMessage"] = "Resource updated.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Delete(int id)
        {
            TempData["StatusMessage"] = "Resource removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}