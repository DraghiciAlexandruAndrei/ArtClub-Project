using ArtClub.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index() => View("Index");

        public IActionResult Login() => View("Index");

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View("Index", model);
            TempData["StatusMessage"] = "Login completed. Sample role dashboard opened.";
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Register() => View("Create");

        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("Create", model);
            TempData["StatusMessage"] = "Registration completed. Welcome to the club.";
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            var model = new AccountDashboardViewModel
            {
                DisplayName = "Guest Member",
                CurrentRole = "Member",
                MembershipStatus = "Active",
                Email = "member@artclub.local",
                Notifications = new List<string>
                {
                    "Invitation received for Summer Exhibition.",
                    "Your profile is up to date.",
                    "Membership fee is valid for this month."
                },
                QuickActions = new List<string>
                {
                    "Create event",
                    "Reserve resource",
                    "Open invitation inbox",
                    "View club reports"
                }
            };

            return View(model);
        }

        public IActionResult Logout()
        {
            TempData["StatusMessage"] = "You have been signed out.";
            return RedirectToAction("Index", "Home");
        }
    }
}