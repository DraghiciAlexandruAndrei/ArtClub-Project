using ArtClub.Models.ViewModels.Reservation;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class ReservationController : Controller
    {
        private readonly IReservationService _reservationService;
        private readonly INotificationService _notificationService;

        public ReservationController(
            IReservationService reservationService,
            INotificationService notificationService)
        {
            _reservationService = reservationService;
            _notificationService = notificationService;
        }

        // GET: Admin/Reservation/PendingOverrides
        public async Task<IActionResult> PendingOverrides()
        {
            var manualPendingReservations = await _reservationService.GetPendingOverridesAsync();

            var model = manualPendingReservations.Select(r => new ReservationStatusViewModel
            {
                ReservationId = r.Id,
                ResourceId = r.ResourceId,
                ResourceName = r.Resource?.Name ?? "Unknown",
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Status = r.Status,
                IsAdminOverride = r.IsAdminOverride,
                AdminOverrideById = r.AdminOverrideById,
                OverrideCreatedAt = r.OverrideCreatedAt
            }).OrderByDescending(r => r.OverrideCreatedAt).ToList();

            return View(model);
        }

        // POST: Admin/Reservation/ApprovePending/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePending(int reservationId)
        {
            var success = await _reservationService.ApprovePendingOverrideAsync(reservationId);

            if (success)
            {
                TempData["StatusMessage"] = "Reservation override approved successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to approve reservation.";
            }

            return RedirectToAction(nameof(PendingOverrides));
        }

        // POST: Admin/Reservation/RejectPending/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPending(int reservationId)
        {
            var success = await _reservationService.RejectPendingOverrideAsync(reservationId);

            if (success)
            {
                TempData["StatusMessage"] = "Reservation override marked for user reschedule.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to reject reservation.";
            }

            return RedirectToAction(nameof(PendingOverrides));
        }

        // GET: Admin/Reservation/RequiringReschedule
        public async Task<IActionResult> RequiringReschedule()
        {
            var allReservations = await _reservationService.GetRequiringRescheduleAsync();
            var reschedulingReservations = allReservations
                .Select(r => new ReservationStatusViewModel
                {
                    ReservationId = r.Id,
                    ResourceId = r.ResourceId,
                    ResourceName = r.Resource?.Name ?? "Unknown",
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    Status = r.Status,
                    IsAdminOverride = r.IsAdminOverride,
                    AdminOverrideById = r.AdminOverrideById,
                    OverrideCreatedAt = r.OverrideCreatedAt
                })
                .OrderByDescending(r => r.StartTime)
                .ToList();

            return View(reschedulingReservations);
        }
    }
}

