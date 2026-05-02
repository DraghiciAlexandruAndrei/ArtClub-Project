using System.Collections.Generic;
using ArtClub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class ArtPieceController : Controller
    {
        private static List<ArtPieceListViewModel> GetSamplePieces() => new()
        {
            new ArtPieceListViewModel { Id = 1, Title = "Sunlit Studio", ArtistName = "A. Ionescu", Style = "Impressionist" },
            new ArtPieceListViewModel { Id = 2, Title = "Modern Shapes", ArtistName = "G. Pop", Style = "Abstract" },
            new ArtPieceListViewModel { Id = 3, Title = "Gallery Night", ArtistName = "L. Marin", Style = "Contemporary" }
        };

        public IActionResult Index() => View(GetSamplePieces());

        public IActionResult Popular() => View(GetSamplePieces().GetRange(0, 2));

        public IActionResult Details(int id)
        {
            var model = new ArtPieceDetailsViewModel
            {
                Id = id,
                Title = "Sunlit Studio",
                Creator = "A. Ionescu",
                Description = "A highlighted art piece presented in the club collection with exhibition context and descriptive metadata.",
                AssociatedEvents = new List<string>
                {
                    "Spring Exhibition",
                    "Member Showcase Night"
                },
                LastUpdated = "Today"
            };

            return View(model);
        }

        public IActionResult Create() => View();

        public IActionResult Edit(int id) => View();

        public IActionResult Delete(int id) => View();
    }
}