using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepo;
        private readonly IReservationService _reservationService;
        private readonly IFinanceService _financeService;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepo;

        public EventService(
            IEventRepository eventRepo,
            IReservationService reservationService,
            IFinanceService financeService,
            INotificationService notificationService,
            IUserRepository userRepo)
        {
            _eventRepo = eventRepo;
            _reservationService = reservationService;
            _financeService = financeService;
            _notificationService = notificationService;
            _userRepo = userRepo;
        }

        public async Task<bool> CreateEventAsync(Event model)
        {
            if (model == null || model.Reservation == null) return false;

            // 1. Verificăm limita de evenimente conform profilului (REQ-5)
            var user = await _userRepo.GetByIdAsync(model.OrganizerId) as Member;
            var currentEvents = await _eventRepo.GetAllWithDetailsAsync();
            var organizerEventsCount = currentEvents.Count(e => e.OrganizerId == model.OrganizerId);

            if (user == null || organizerEventsCount >= user.EventCreationLimit) return false;

            // 2. Calculăm costul estimat (300 lei/zi locație + 200 lei/zi/piesă)
            // NU verificăm fondurile aici, deoarece plata se face la "Details"
            int artCount = model.EventArtPieces?.Count ?? 0;
            int days = (model.Reservation.EndTime - model.Reservation.StartTime).Days;
            if (days <= 0) days = 1;

            model.Budget = (days * 300) + (days * artCount * 200);

            try
            {
                await _eventRepo.AddAsync(model);
                var success = await _eventRepo.SaveChangesAsync();

                if (success)
                {
                    await _notificationService.SendEmailAsync(user.Email, "Eveniment Creat",
                        $"Evenimentul '{model.Title}' a fost creat. Cost estimat: {model.Budget} lei. Te rugăm să finalizezi plata.");
                }
                return success;
            }
            catch { return false; }
        }

        public async Task<bool> UpdateEventAsync(string originalTitle, Event model)
        {
            var ev = await _eventRepo.GetByTitleWithDetailsAsync(originalTitle);
            if (ev == null) return false;

            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.ResourceId = model.ResourceId;

            if (ev.Reservation != null && model.Reservation != null)
            {
                ev.Reservation.ResourceId = model.Reservation.ResourceId;
                ev.Reservation.StartTime = model.Reservation.StartTime;
                ev.Reservation.EndTime = model.Reservation.EndTime;
            }

            // Actualizăm și piesele de artă dacă au fost modificate
            if (model.EventArtPieces != null)
            {
                ev.EventArtPieces = model.EventArtPieces;
            }

            // Recalculăm bugetul în caz că s-a schimbat durata sau nr. de piese
            int artCount = ev.EventArtPieces?.Count ?? 0;
            int days = (ev.Reservation.EndTime - ev.Reservation.StartTime).Days;
            if (days <= 0) days = 1;
            ev.Budget = (days * 300) + (days * artCount * 200);

            return await _eventRepo.SaveChangesAsync();
        }

        // --- Metodele de suport rămase intacte conform cerinței ---

        public async Task<bool> CancelEventAsync(int eventId)
        {
            var eventToDelete = await _eventRepo.GetByIdWithReservationAsync(eventId);
            if (eventToDelete == null) return false;

            _eventRepo.Remove(eventToDelete);
            return await _eventRepo.SaveChangesAsync();
        }

        public async Task<List<Event>> GetAllEventsAsync()
        {
            return await _eventRepo.GetAllWithDetailsAsync();
        }

        public async Task<Event?> GetEventByTitleAsync(string title)
        {
            return await _eventRepo.GetByTitleWithDetailsAsync(title);
        }

        public async Task<Resource?> GetResourceByNameAsync(string resourceName)
        {
            return await _eventRepo.GetResourceByNameAsync(resourceName);
        }

        public async Task<bool> DeleteEventByTitleAsync(string title)
        {
            var ev = await _eventRepo.GetByTitleWithDetailsAsync(title);
            if (ev == null) return false;

            return await CancelEventAsync(ev.Id);
        }

        public async Task SendInvitationAsync(int eventId, int inviteeId)
        {
            var user = await _userRepo.GetByIdAsync(inviteeId);
            var ev = await _eventRepo.GetByIdWithReservationAsync(eventId);

            if (user != null && ev != null)
            {
                var invitation = new Invitation { EventId = eventId, InviteeId = inviteeId };
                await _eventRepo.AddInvitationAsync(invitation);
                await _eventRepo.SaveChangesAsync();
                await _notificationService.SendEmailAsync(user.Email, "Invitație", $"Ai fost invitat la {ev.Title}!");
            }
        }

        public async Task RespondToInvitationAsync(int invitationId, bool accept)
        {
            var inv = await _eventRepo.GetInvitationByIdAsync(invitationId);
            if (inv != null)
            {
                if (accept) inv.Accept(); else inv.Decline();
                await _eventRepo.SaveChangesAsync();
            }
        }

        public async Task<int?> GetDefaultOrganizerIdAsync()
        {
            return await _eventRepo.GetFirstUserIdAsync();
        }

        public async Task<List<Resource>> GetAllResourcesAsync()
        {
            return await _eventRepo.GetAllResourcesAsync();
        }

        public async Task<List<User>> GetAllMembersAsync()
        {
            var users = await _userRepo.GetAllOrderedByNameAsync();
            return users.Where(u => u.Role == UserRole.Member).ToList();
        }
    }
}