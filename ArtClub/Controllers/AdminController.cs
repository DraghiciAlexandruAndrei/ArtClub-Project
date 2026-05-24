using ArtClub.DataAccess;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels;
using ArtClub.Models.ViewModels.Admin;
using ArtClub.Models.ViewModels.Finance;
using ArtClub.Models.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AdminController(ApplicationDbContext context, UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalResources = await _context.Resources.CountAsync(),
                TotalEvents = await _context.Events.CountAsync(),
                TotalPayments = await _context.Payments.CountAsync(),
                MonthlyIncome = await _context.Payments
                    .Where(p => p.IsIncome && p.Date.Month == DateTime.Now.Month && p.Date.Year == DateTime.Now.Year)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0,
                MonthlyExpenses = await _context.Payments
                    .Where(p => !p.IsIncome && p.Date.Month == DateTime.Now.Month && p.Date.Year == DateTime.Now.Year)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0
            };

            return View(vm);
        }

        public async Task<IActionResult> Users(string? search, UserRole? roleFilter)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.ToLower();
                query = query.Where(u =>
                    (u.Id.ToString() == s) || // REQ-9: Search by User ID (MembershipID)
                    (u.UserName != null && u.UserName.ToLower().Contains(s)) ||
                    (u.Email != null && u.Email.ToLower().Contains(s)));
            }

            // REQ-9: Filter by role
            if (roleFilter.HasValue)
            {
                query = query.Where(u => u.Role == roleFilter.Value);
            }

            var users = await query.ToListAsync();
            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBanned = true;
            user.IsActive = false;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Utilizatorul {user.UserName} a fost suspendat.";
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnbanUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsBanned = false;
            user.IsActive = true;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = $"Utilizatorul {user.UserName} a fost reactivat.";
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            var vm = new EditUserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                CurrentRole = user.Role,
                NewRole = user.Role
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(EditUserRoleViewModel vm)
        {
            var user = await _context.Users.FindAsync(vm.UserId);
            if (user == null) return NotFound();

            var oldIdentityRoles = await _userManager.GetRolesAsync(user);
            if (oldIdentityRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, oldIdentityRoles);
            }

            user.Role = vm.NewRole;
            user.AdminLevel = vm.NewRole == UserRole.Admin ? 1 : 0;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return View(vm);
            }

            var identityRole = vm.NewRole.ToString();
            if (!await _userManager.IsInRoleAsync(user, identityRole))
            {
                var addRoleResult = await _userManager.AddToRoleAsync(user, identityRole);
                if (!addRoleResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
                    return View(vm);
                }
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                await _signInManager.RefreshSignInAsync(user);
            }

            TempData["StatusMessage"] = $"Rolul utilizatorului {user.UserName} a fost schimbat în {vm.NewRole}.";
            return RedirectToAction(nameof(Users));
        }

        public async Task<IActionResult> Payments(int? userId, DateTime? from, DateTime? to, bool? isIncome)
        {
            var query = _context.Payments.Include(p => p.User).AsQueryable();

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);
            if (from.HasValue)
                query = query.Where(p => p.Date >= from.Value);
            if (to.HasValue)
                query = query.Where(p => p.Date <= to.Value);
            if (isIncome.HasValue)
                query = query.Where(p => p.IsIncome == isIncome.Value);

            var payments = await query.OrderByDescending(p => p.Date).ToListAsync();
            return View(payments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(Payment payment)
        {
            if (!ModelState.IsValid)
                return RedirectToAction(nameof(Payments));

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Plata a fost înregistrată.";
            return RedirectToAction(nameof(Payments));
        }

        public async Task<IActionResult> Resources()
        {
            var resources = await _context.Resources.ToListAsync();
            return View(resources);
        }

        [HttpGet]
        public IActionResult CreateResource() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResource(Resource resource)
        {
            if (!ModelState.IsValid) return View(resource);

            _context.Resources.Add(resource);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Resursa a fost adăugată.";
            return RedirectToAction(nameof(Resources));
        }

        [HttpGet]
        public async Task<IActionResult> EditResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource == null) return NotFound();

            return View(resource);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditResource(Resource resource)
        {
            if (!ModelState.IsValid) return View(resource);

            _context.Resources.Update(resource);
            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Resursa a fost actualizată.";
            return RedirectToAction(nameof(Resources));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.Resources.FindAsync(id);
            if (resource != null)
            {
                _context.Resources.Remove(resource);
                await _context.SaveChangesAsync();
                TempData["StatusMessage"] = "Resursa a fost ștearsă.";
            }

            return RedirectToAction(nameof(Resources));
        }

        /// <summary>
        /// REQ-22: Admin Exhibition Halls Management - Timed resources for exhibitions.
        /// </summary>
        public async Task<IActionResult> ExhibitionHalls()
        {
            var exhibitionHalls = await _context.Resources
                .Where(r => r.IsExhibitionHall)
                .ToListAsync();
            return View(exhibitionHalls);
        }

        /// <summary>
        /// REQ-36: Admin Location Management - Affiliated event locations.
        /// </summary>
        public async Task<IActionResult> Locations()
        {
            // Locations are treated as non-exhibition resources
            var locations = await _context.Resources
                .Where(r => !r.IsExhibitionHall)
                .ToListAsync();
            return View(locations);
        }

        [HttpGet]
        public async Task<IActionResult> Reports(int? month, int? year)
        {
            var selectedMonth = month ?? DateTime.Now.Month;
            var selectedYear = year ?? DateTime.Now.Year;

            var income = await _context.Payments
                .Where(p => p.IsIncome && p.Date.Month == selectedMonth && p.Date.Year == selectedYear)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var expenses = await _context.Payments
                .Where(p => !p.IsIncome && p.Date.Month == selectedMonth && p.Date.Year == selectedYear)
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            var vm = new MonthlyReportViewModel
            {
                Month = selectedMonth,
                Year = selectedYear,
                TotalIncome = income,
                TotalExpenses = expenses,
                MembersBlocked = expenses > income
            };

            ViewBag.MonthlyMembersBlocked = vm.MembersBlocked;
            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ClubSettings()
        {
            var settings = await _context.ClubSettings.FirstOrDefaultAsync() ?? new ClubSettings();

            var vm = new ClubSettingsViewModel
            {
                NonMemberReservationFeePerDay = settings.NonMemberReservationFeePerDay,
                MembershipCost = settings.MembershipCost,
                EventCostPerArtPiece = settings.EventCostPerArtPiece,
                EventCostPerLocation = settings.EventCostPerLocation,
                PendingOverrideApprovalHours = settings.PendingOverrideApprovalHours
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClubSettings(ClubSettingsViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var settings = await _context.ClubSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new ClubSettings();
                await _context.ClubSettings.AddAsync(settings);
            }

            settings.NonMemberReservationFeePerDay = model.NonMemberReservationFeePerDay;
            settings.MembershipCost = model.MembershipCost;
            settings.EventCostPerArtPiece = model.EventCostPerArtPiece;
            settings.EventCostPerLocation = model.EventCostPerLocation;
            settings.PendingOverrideApprovalHours = model.PendingOverrideApprovalHours;
            settings.LastUpdated = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["StatusMessage"] = "Club settings saved.";
            return RedirectToAction(nameof(ClubSettings));
        }

        /// <summary>
        /// REQ-23: Resource Availability Report - Shows which resources remain available for a selected interval.
        /// </summary>
        public async Task<IActionResult> ResourceAvailabilityReport(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now;
            var end = endDate ?? DateTime.Now.AddDays(30);

            var resources = await _context.Resources.Include(r => r.Reservations).ToListAsync();

            var rows = resources.Select(resource =>
            {
                var conflictCount = resource.Reservations.Count(r =>
                    r.StartTime < end && r.EndTime > start &&
                    r.Status != Models.Enums.ReservationStatus.Cancelled);

                return new ResourceAvailabilityReportRowViewModel
                {
                    ResourceId = resource.Id,
                    ResourceName = resource.Name,
                    Capacity = resource.Capacity,
                    ConflictCount = conflictCount
                };
            })
            .Where(r => r.ConflictCount == 0)
            .OrderBy(r => r.ResourceName)
            .ToList();

            var vm = new ResourceAvailabilityReportViewModel
            {
                StartDate = start,
                EndDate = end,
                AvailableResources = rows,
                UnavailableCount = resources.Count - rows.Count
            };

            return View(vm);
        }

        /// <summary>
        /// REQ-24: Reservation Date Report - Shows all reservations for a date range grouped by date/resource.
        /// </summary>
        public async Task<IActionResult> ReservationDateReport(DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.Now;
            var end = endDate ?? DateTime.Now.AddDays(30);

            var reservations = await _context.Reservations
                .Include(r => r.Resource)
                .Include(r => r.Event)
                .Where(r => r.StartTime < end && r.EndTime > start)
                .OrderBy(r => r.StartTime)
                .ThenBy(r => r.Resource.Name)
                .ToListAsync();

            ViewBag.StartDate = start;
            ViewBag.EndDate = end;
            return View(reservations);
        }
    }
}