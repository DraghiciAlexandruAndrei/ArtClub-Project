using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFinanceService _financeService;

        // CORECT: Injectăm ambele servicii prin constructor pentru a respecta DI
        public AccountController(IUserService userService, IFinanceService financeService)
        {
            _userService = userService;
            _financeService = financeService;
        }

        // ==========================================
        // 1. LISTARE UTILIZATORI (ADMIN ONLY)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        // ==========================================
        // 2. LOGICĂ DE LOGIN
        // ==========================================
        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var success = await _userService.AuthenticateAsync(model.Email, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", "Email sau parolă incorectă.");
                return View(model);
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Contul este inactiv.");
                return View(model);
            }

            // Setare Sesiune
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.UserName ?? "User");
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            if (user is Member member)
            {
                HttpContext.Session.SetString("IsMembershipActive", member.IsMembershipActive.ToString());
            }

            return RedirectToAction("Index", "Home");
        }

        // ==========================================
        // 3. LOGICĂ DE ÎNREGISTRARE (REGISTER)
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View("Create", new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("Create", model);

            var member = new Member
            {
                UserName = $"{model.FirstName} {model.LastName}".Trim(),
                Email = model.Email,
                Role = UserRole.Member,
                IsActive = true,
                IsMembershipActive = false,
                MembershipDate = DateTime.Now,
                EventCreationLimit = 1 // REQ-5: 1 eveniment cadou
            };

            var created = await _userService.RegisterUserAsync(member, model.Password);
            if (!created)
            {
                ModelState.AddModelError("Email", "Email deja existent.");
                return View("Create", model);
            }

            return RedirectToAction(nameof(Login));
        }

        // ==========================================
        // 4. PROFIL ȘI EDITARE (MEMBRI)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId.Value);
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId.Value);
            var model = new RegisterViewModel { Email = user.Email }; // Simplificat pentru editare
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction(nameof(Login));

            var user = await _userService.GetUserByIdAsync(userId.Value);
            user.Email = model.Email;
            user.UserName = $"{model.FirstName} {model.LastName}".Trim();

            await _userService.UpdateUserAsync(user);
            HttpContext.Session.SetString("UserName", user.UserName);

            return RedirectToAction(nameof(Profile));
        }

        // ==========================================
        // 5. UPGRADE STATUS (REQ-5)
        // ==========================================
        [HttpGet]
        public IActionResult Upgrade() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUpgrade()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null) return RedirectToAction(nameof(Login));

            // Procesare financiară (100 lei conform cerinței)
            var success = await _financeService.ProcessMembershipUpgradeAsync(userId.Value, 100);

            if (success)
            {
                HttpContext.Session.SetString("IsMembershipActive", "True");
                TempData["StatusMessage"] = "Abonament activat! Limită ridicată la 5 evenimente.";
                return RedirectToAction("Index", "Event");
            }

            ModelState.AddModelError("", "Eroare la procesarea plății.");
            return View("Upgrade");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}