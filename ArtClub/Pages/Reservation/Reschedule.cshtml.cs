using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ArtClub.Pages.Reservation
{
    [Authorize]
    public class RescheduleModel : PageModel
    {
        private readonly IReservationService _reservationService;

        public RescheduleModel(IReservationService reservationService)
        {
            _reservationService = reservationService;
        }

        [BindProperty]
        public int ReservationId { get; set; }

        public Models.Entities.Reservation CurrentReservation { get; set; }

        [BindProperty]
        public DateTime NewStartTime { get; set; }

        [BindProperty]
        public DateTime NewEndTime { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var reservation = await _reservationService.GetReservationByIdAsync(id);

            if (reservation == null)
                return NotFound();

            if (reservation.Status != ReservationStatus.OverrideRequired)
                return BadRequest("This reservation does not require rescheduling.");

            ReservationId = id;
            CurrentReservation = reservation;

            // Set default new times to something reasonable
            NewStartTime = CurrentReservation.StartTime.AddDays(1);
            NewEndTime = NewStartTime.AddHours((CurrentReservation.EndTime - CurrentReservation.StartTime).Hours);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                CurrentReservation = await _reservationService.GetReservationByIdAsync(ReservationId);
                return Page();
            }

            if (NewEndTime <= NewStartTime)
            {
                ModelState.AddModelError("NewEndTime", "End time must be after start time.");
                CurrentReservation = await _reservationService.GetReservationByIdAsync(ReservationId);
                return Page();
            }

            var success = await _reservationService.RescheduleReservationAsync(ReservationId, NewStartTime, NewEndTime);

            if (!success)
            {
                ModelState.AddModelError("", "The selected time slot is not available. Please choose a different time.");
                CurrentReservation = await _reservationService.GetReservationByIdAsync(ReservationId);
                return Page();
            }

            TempData["StatusMessage"] = "Reservation rescheduled successfully!";
            return RedirectToPage("/Account/Dashboard");
        }

        public async Task<IActionResult> OnPostCancelAsync()
        {
            var reservation = await _reservationService.GetReservationByIdAsync(ReservationId);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _reservationService.SaveChangesAsync();
                TempData["StatusMessage"] = "Reservation cancelled.";
            }

            return RedirectToPage("/Account/Dashboard");
        }
    }
}
