using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels.Account;
using ArtClub.Models.ViewModels.Event;
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

        public IActionResult Index()
        {
            var users = _userManager.Users.ToList();
            return View(users);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View(new LoginViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email sau parolă incorectă.");
                return View(model);
            }

            if (user.IsBanned)
            {
                ModelState.AddModelError("", "Acest cont a fost suspendat.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("", "Email sau parolă incorectă.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View("Create", new RegisterViewModel());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View("Create", model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("", "Există deja un cont cu acest email.");
                return View("Create", model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                Role = UserRole.Member,
                IsActive = true,
                IsBanned = false,
                IsMembershipActive = false,
                MembershipDate = DateTime.Now,
                EventCreationLimit = 1,
                AdminLevel = 0
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _userManager.IsInRoleAsync(user, "Member"))
                {
                    await _userManager.AddToRoleAsync(user, "Member");
                }

                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View("Create", model);
        }

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
            if (user == null) return Challenge();

            var model = new RegisterViewModel
            {
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Profile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [Authorize]
        public IActionResult Upgrade() => View();

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUpgrade()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var success = await _financeService.ProcessMembershipUpgradeAsync(user.Id, 100);

            if (success)
            {
                user.IsMembershipActive = true;
                user.Role = UserRole.Member;

                await _userManager.UpdateAsync(user);

                TempData["StatusMessage"] = "Abonament activat! Limită ridicată la 5 evenimente.";
                return RedirectToAction("Index", "Event");
            }

            ModelState.AddModelError("", "Eroare la procesarea plății.");
            return View("Upgrade");
        }

        [Authorize]
        public async Task<IActionResult> MyProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            string userIdString = _userManager.GetUserId(User);
            int userIdInt = int.Parse(userIdString);

            var organizedEvents = await _eventService.GetEventsByOrganizerIdAsync(userIdString);
            var inboxInvitations = await _invitationService.GetUserInboxAsync(userIdInt);

            var model = new MyProfileViewModel
            {
                UserName = user.UserName,
                Email = user.Email,
                JoinedDate = user.MembershipDate ?? DateTime.Now,
                IsMembershipActive = user.IsMembershipActive,
                MyOrganizedEvents = organizedEvents.Select(e => new EventSummaryViewModel
                {
                    Title = e.Title,
                    ResourceName = e.Resource?.Name ?? "N/A",
                    StartDate = e.Reservation?.StartTime ?? DateTime.Now,
                    Status = e.Reservation?.StartTime > DateTime.Now ? "Viitor" : "Încheiat"
                }).ToList(),
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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomeMember()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.IsMembershipActive = true;
            user.Role = UserRole.Member;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = "A apărut o eroare la actualizarea profilului.";
                return RedirectToAction(nameof(MyProfile));
            }

            if (!await _userManager.IsInRoleAsync(user, "Member"))
            {
                await _userManager.AddToRoleAsync(user, "Member");
            }

            TempData["StatusMessage"] = "Plată confirmată! Acum ești membru activ ArtClub și poți crea până la 5 evenimente.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = "Utilizatorul nu a fost găsit.";
                return RedirectToAction(nameof(Index));
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["ErrorMessage"] = "Nu îți poți șterge propriul cont din această interfață.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = $"Contul lui {user.UserName} a fost eliminat definitiv.";
                return RedirectToAction(nameof(Index));
            }

            TempData["ErrorMessage"] = "Eroare la ștergere: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var organized = await _eventService.GetEventsByOrganizerIdAsync(user.Id.ToString());
            var invitations = await _invitationService.GetUserInboxAsync(user.Id);

            var model = new MemberDashboardViewModel
            {
                UserName = user.UserName,
                IsMembershipActive = user.IsMembershipActive,
                RemainingEventLimit = user.GetEventCreationLimit() - organized.Count(e => e.Reservation?.StartTime > DateTime.Now),
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