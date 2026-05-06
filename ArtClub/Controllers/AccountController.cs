using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;

using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IFinanceService _financeService;
        private readonly IInvitationService _invitationService;
        private readonly IEventService _eventService;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IFinanceService financeService,
            IInvitationService invitationService,
            IEventService eventService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _financeService = financeService;
            _invitationService = invitationService;
            _eventService = eventService;
        }

        // 1. LISTARE (Folosim direct UserManager)
        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        // 2. LOGIN (Folosim SignInManager)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Identity verifică automat PasswordHash și IsActive
            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, isPersistent: false, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email sau parolă incorectă.");
            return View(model);
        }

        // 3. REGISTER (Folosim UserManager.CreateAsync)
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View("Create", new RegisterViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("Create", model);

            var user = new User // Am folosit clasa User actualizată
            {
                UserName = model.Email,
                Email = model.Email,
                Role = UserRole.Member,
                IsActive = true,
                IsMembershipActive = false,
                MembershipDate = DateTime.Now,
                EventCreationLimit = 1
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View("Create", model);
        }

        // 4. PROFIL ȘI EDITARE
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new RegisterViewModel { Email = user.Email };
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            user.Email = model.Email;
            user.UserName = model.Email; // UserName trebuie să rămână sincronizat de regulă

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Profile));
            }
            return View(model);
        }

        // 5. UPGRADE STATUS (Păstrăm logica de business)
        [HttpGet]
        [Authorize]
        public IActionResult Upgrade() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUpgrade()
        {
            var user = await _userManager.GetUserAsync(User);

            // Procesare financiară
            var success = await _financeService.ProcessMembershipUpgradeAsync(user.Id, 100);

            if (success)
            {
                // Actualizăm direct pe obiectul user Identity
                user.IsMembershipActive = true;
                user.EventCreationLimit = 5;
                await _userManager.UpdateAsync(user);

                TempData["StatusMessage"] = "Abonament activat! Limită ridicată la 5 evenimente.";
                return RedirectToAction("Index", "Event");
            }

            ModelState.AddModelError("", "Eroare la procesarea plății.");
            return View("Upgrade");
        }

        // 6. MY PROFILE (Versiunea cu Invitatii)
        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Obținem ID-ul ca string pentru Evenimente și ca int pentru Invitații
            string userIdString = _userManager.GetUserId(User);
            int userIdInt = int.Parse(userIdString);

            // 2. Apelăm serviciile folosind denumirile corecte din interfețele tale
            var organizedEvents = await _eventService.GetEventsByOrganizerIdAsync(userIdString);
            var inboxInvitations = await _invitationService.GetUserInboxAsync(userIdInt);

            var model = new MyProfileViewModel
            {

                UserName = user.UserName,
                Email = user.Email,
                JoinedDate = user.MembershipDate ?? DateTime.Now,
                IsMembershipActive = user.IsMembershipActive,

                // Mapare Evenimente
                MyOrganizedEvents = organizedEvents.Select(e => new EventSummaryViewModel
                {
                    Title = e.Title,
                    ResourceName = e.Resource?.Name ?? "N/A",
                    StartDate = e.Reservation?.StartTime ?? DateTime.Now,
                    Status = e.Reservation?.StartTime > DateTime.Now ? "Viitor" : "Încheiat"
                }).ToList(),

                // Mapare Invitații (GetUserInboxAsync returnează List<Invitation>)
                PendingInvitations = inboxInvitations.Select(i => new InvitationInboxViewModel
                {
                    InvitationId = i.Id,
                    EventTitle = i.Event?.Title ?? "Eveniment fără titlu",
                    OrganizerName = i.Event?.Organizer?.UserName ?? "Organizator",
                    EventDate = i.Event?.Reservation?.StartTime ?? DateTime.Now,
                    Description = i.Event?.Description ?? "Fără descriere"
                }).ToList()
            };

            return View(model);
        }

        // Metoda pentru procesarea plății (simulată)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeMember()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            // Simulăm procesarea plății prin FinanceService dacă este necesar
            // var success = await _financeService.ProcessMembershipUpgradeAsync(user.Id, 400);

            user.IsMembershipActive = true;
            user.EventCreationLimit = 5; // Upgrade automat la limită

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Plată confirmată! Acum ești membru activ ArtClub și poți crea până la 5 evenimente.";
            }
            else
            {
                TempData["ErrorMessage"] = "A apărut o eroare la actualizarea profilului.";
            }

            return RedirectToAction(nameof(MyProfile));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HandleInvitation(int id, bool accept)
        {
            bool result;
            if (accept)
            {
                result = await _invitationService.AcceptInvitationAsync(id);
                if (result) TempData["StatusMessage"] = "Ai acceptat invitația cu succes!";
            }
            else
            {
                result = await _invitationService.DeclineInvitationAsync(id);
                if (result) TempData["StatusMessage"] = "Ai refuzat invitația.";
            }

            if (!result)
            {
                TempData["ErrorMessage"] = "A apărut o eroare la procesarea invitației.";
            }

            return RedirectToAction(nameof(MyProfile));
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // POST: Account/DeleteConfirmed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id) // Schimbat din 'title' în 'id'
        {
            // 1. Căutăm utilizatorul în baza de date
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost găsit.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Verificare de siguranță: să nu se șteargă singur adminul logat
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Nu îți poți șterge propriul cont din această interfață.";
                return RedirectToAction(nameof(Index));
            }

            // 3. Ștergerea propriu-zisă
            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = $"Contul lui {user.UserName} a fost eliminat definitiv.";
                return RedirectToAction(nameof(Index));
            }

            // Dacă apar erori (ex: restricții de bază de date)
            TempData["ErrorMessage"] = "Eroare la ștergere: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        //Dashboard pentru membri
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Preluăm evenimentele organizate
            var organized = await _eventService.GetEventsByOrganizerIdAsync(user.Id.ToString());

            // Preluăm invitațiile (folosind serviciul tău)
            var invitations = await _invitationService.GetUserInboxAsync(user.Id);

            var model = new MemberDashboardViewModel
            {
                UserName = user.UserName,
                IsMembershipActive = user.IsMembershipActive,
                RemainingEventLimit = user.EventCreationLimit - organized.Count(e => e.Reservation?.StartTime > DateTime.Now),
                EventsOrganizedCount = organized.Count,
                PendingInvitationsCount = invitations.Count(i => i.Status == InvitationStatus.Pending),

                RecentInvitations = invitations.Where(i => i.Status == InvitationStatus.Pending)
                    .Take(3)
                    .Select(i => new InvitationInboxViewModel
                    {
                        InvitationId = i.Id,
                        EventTitle = i.Event?.Title,
                        OrganizerName = i.Event?.Organizer?.UserName,
                        EventDate = i.Event?.Reservation?.StartTime ?? DateTime.Now
                    }).ToList(),

                UpcomingEvents = organized.Where(e => e.Reservation?.StartTime > DateTime.Now)
                    .Select(e => new EventSummaryViewModel
                    {
                        Title = e.Title,
                        StartDate = e.Reservation?.StartTime ?? DateTime.Now,
                        ResourceName = e.Resource?.Name
                    }).ToList()
            };

            return View(model);
        }
    }

}