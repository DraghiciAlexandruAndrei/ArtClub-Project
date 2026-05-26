using ArtClub.Models.Entities;
using ArtClub.Models.ViewModels.Account;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtClub.Controllers
{
    [Authorize] // Protejează controller-ul; doar utilizatorii logați pot interacționa cu invitațiile
    public class InvitationController : Controller
    {
        private readonly IInvitationService _invitationService;
        private readonly UserManager<User> _userManager;

        public InvitationController(IInvitationService invitationService, UserManager<User> userManager)
        {
            _invitationService = invitationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var invitations = await _invitationService.GetUserInboxAsync(user.Id);

            var model = invitations.Select(i => new InvitationInboxViewModel
            {
                InvitationId = i.Id,
                EventTitle = i.Event?.Title ?? "Eveniment",
                OrganizerName = i.Event?.Organizer?.UserName ?? "Artist",
                EventDate = i.Event?.Reservation?.StartTime ?? DateTime.Now,
                Description = i.Event?.Description
            }).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(int eventId, int inviteeId, string eventTitle)
        {
            // Verifică dacă utilizatorul curent are dreptul să trimită invitații (logica internă în service)
            var success = await _invitationService.SendInvitationAsync(eventId, inviteeId);

            if (success)
                TempData["StatusMessage"] = "Invitația a fost trimisă cu succes!";
            else
                TempData["ErrorMessage"] = "Utilizatorul este deja invitat sau nu poate fi invitat.";

            return RedirectToAction("Details", "Event", new { title = eventTitle });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id)
        {
            await _invitationService.AcceptInvitationAsync(id);
            TempData["StatusMessage"] = "Ai acceptat invitația!";
            return RedirectToAction("MyProfile", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decline(int id)
        {
            await _invitationService.DeclineInvitationAsync(id);
            TempData["StatusMessage"] = "Ai refuzat invitația.";
            return RedirectToAction("MyProfile", "Account");
        }
    }
}