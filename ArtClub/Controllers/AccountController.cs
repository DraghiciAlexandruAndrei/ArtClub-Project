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

        public AccountController(IUserService userService)
        {
            _userService = userService;
        }

        // Listare utilizatori (Accesibilă doar Admin-ului în mod normal)
        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // 1. Autentificare (ideal ar fi cu Password Hashing)
            var success = await _userService.AuthenticateAsync(model.Email, model.Password);
            if (!success)
            {
                ModelState.AddModelError("", "Email sau parolă incorectă.");
                return View(model);
            }

            // 2. Preluare date utilizator pentru sesiune
            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError("", "Contul este inactiv sau nu a fost găsit.");
                return View(model);
            }

            // 3. Setare Sesiune
            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("UserRole", user.Role.ToString());

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Instanțiem clasa concretă Member pentru un utilizator nou
            var member = new Member
            {
                UserName = $"{model.FirstName} {model.LastName}".Trim(),
                Email = model.Email,
                Role = UserRole.Member,
                IsActive = true,
                MembershipDate = DateTime.Now,
                EventCreationLimit = 5 // Limita default stabilită de tine
            };

            var created = await _userService.RegisterUserAsync(member, model.Password);
            if (!created)
            {
                ModelState.AddModelError("Email", "Această adresă de email este deja înregistrată.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Înregistrare reușită! Acum te poți loga.";
            return RedirectToAction(nameof(Login));
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Detalii utilizator (Folosește polimorfismul pentru a afișa date specifice în View)
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }
    }
}