using ArtClub.Models.Entities;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ArtClub.Models.ViewModels;
using ArtClub.Models.Enums;

[Authorize]
public class InvitationController : Controller
{
    private readonly IInvitationService _invitationService;
    private readonly UserManager<User> _userManager;

    public InvitationController(IInvitationService invitationService, UserManager<User> userManager)
    {
        _invitationService = invitationService;
        _userManager = userManager;
    }

    // Afișează invitațiile primite
    public async Task<IActionResult> Inbox()
    {
        // Presupunem că avem ID-ul utilizatorului din sesiune sau Identity
        var userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        var invitations = await _invitationService.GetUserInboxAsync(userId);

        var model = invitations.Select(i => new InvitationInboxViewModel
        {
            InvitationId = i.Id,
            EventTitle = i.Event.Title,
            OrganizerName = i.Event.Organizer?.UserName ?? "Artist",
            EventDate = i.Event.Reservation?.StartTime ?? DateTime.Now,
            Description = i.Event.Description
        }).ToList();

        return View(model);
    }

    // Procesează acceptarea
    [HttpPost]
    public async Task<IActionResult> Accept(int id)
    {
        await _invitationService.AcceptInvitationAsync(id);
        return RedirectToAction(nameof(Inbox));
    }

    // Procesează refuzul
    [HttpPost]
    public async Task<IActionResult> Decline(int id)
    {
        await _invitationService.DeclineInvitationAsync(id);
        return RedirectToAction(nameof(Inbox));
    }

    // Trimitere invitație (apelată din Event Details)
    [HttpPost]
    public async Task<IActionResult> Invite(int eventId, int inviteeId)
    {
        var success = await _invitationService.SendInvitationAsync(eventId, inviteeId);
        if (!success) TempData["Error"] = "Utilizatorul este deja invitat.";

        return RedirectToAction("Details", "Event", new { id = eventId });
    }
}