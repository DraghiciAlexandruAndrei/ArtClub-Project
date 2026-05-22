using ArtClub.DataAccess.Interfaces;
using ArtClub.DataAccess.Repositories;
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
        private readonly IReservationRepository _reservationRepo;

        public EventService(
            IEventRepository eventRepo,
            IReservationService reservationService,
            IFinanceService financeService,
            INotificationService notificationService,
            IUserRepository userRepo,
            IReservationRepository reservationRepo)
        {
            _eventRepo = eventRepo;
            _reservationService = reservationService;
            _financeService = financeService;
            _notificationService = notificationService;
            _userRepo = userRepo;
            _reservationRepo = reservationRepo;
        }

        public async Task<bool> CreateEventAsync(Event model, int? adminUserId = null)
        {
            if (model == null || model.Reservation == null) return false;

            var user = await _userRepo.GetByIdAsync(model.OrganizerId);
            if (user == null) return false;

            var currentEvents = await _eventRepo.GetAllWithDetailsAsync();
            var organizerEventsCount = currentEvents.Count(e => e.OrganizerId == model.OrganizerId);

            // Use role-based limit
            int eventLimit = user.GetEventCreationLimit();
            if (organizerEventsCount >= eventLimit) return false;

            // Check resource availability (with admin override if user is admin)
            var isAvailable = await _reservationService.CheckAvailabilityAsync(
                model.Reservation.ResourceId,
                model.Reservation.StartTime,
                model.Reservation.EndTime,
                adminUserId);

            // If not available and NOT admin, reject immediately
            if (!isAvailable && !adminUserId.HasValue)
            {
                return false;
            }

            // Calculăm costul (Budget-ul devine baza pentru Payment)
            int artCount = model.EventArtPieces?.Count ?? 0;
            int days = (model.Reservation.EndTime - model.Reservation.StartTime).Days;
            if (days <= 0) days = 1;

            model.Budget = (days * 300) + (days * artCount * 200);
            model.IsPaid = false; // Evenimentul începe ca "neplătit" până la apăsarea butonului

            try
            {
                // If admin, check for conflicts and cancel them
                if (adminUserId.HasValue)
                {
                    var conflictingReservations = await _reservationRepo.GetAllConflictingReservationsAsync(
                        model.Reservation.ResourceId,
                        model.Reservation.StartTime,
                        model.Reservation.EndTime);

                    if (conflictingReservations.Count > 0)
                    {
                        foreach (var conflictingRes in conflictingReservations)
                        {
                            // Cancel conflicting reservations
                            conflictingRes.Status = ReservationStatus.Cancelled;

                            // Cancel the associated event if it exists
                            if (conflictingRes.Event != null)
                            {
                                conflictingRes.Event.Cancel();
                            }
                        }

                        // Save the cancellations
                        await _reservationRepo.SaveChangesAsync();
                    }
                }

                // Create the event
                await _eventRepo.AddAsync(model);
                var success = await _eventRepo.SaveChangesAsync();

                if (success)
                {
                    // Now create the reservation separately with proper admin handling
                    await _reservationService.CreateReservationAsync(
                        model.Reservation,
                        adminUserId);

                    await _notificationService.SendEmailAsync(user.Email, "Eveniment Creat",
                        $"Evenimentul '{model.Title}' a fost creat. Taxă datorată: {model.Budget} lei.");
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

            if (model.EventArtPieces != null)
            {
                ev.EventArtPieces = model.EventArtPieces;
            }

            // Recalculăm bugetul
            int artCount = ev.EventArtPieces?.Count ?? 0;
            int days = (ev.Reservation.EndTime - ev.Reservation.StartTime).Days;
            if (days <= 0) days = 1;
            ev.Budget = (days * 300) + (days * artCount * 200);

            return await _eventRepo.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an event and cascades changes to conflicting reservations if admin.
        /// If adminUserId is provided and the event reservation time changed, all conflicting
        /// reservations with Pending or OverrideRequired status are marked as Confirmed.
        /// </summary>
        public async Task<bool> UpdateEventAsync(string originalTitle, Event model, int? adminUserId)
        {
            var ev = await _eventRepo.GetByTitleWithDetailsAsync(originalTitle);
            if (ev == null) return false;

            // Store old reservation times to detect changes
            var oldReservation = ev.Reservation != null 
                ? new { ev.Reservation.StartTime, ev.Reservation.EndTime, ev.Reservation.ResourceId }
                : null;

            ev.Title = model.Title;
            ev.Description = model.Description;
            ev.ResourceId = model.ResourceId;

            if (ev.Reservation != null && model.Reservation != null)
            {
                ev.Reservation.ResourceId = model.Reservation.ResourceId;
                ev.Reservation.StartTime = model.Reservation.StartTime;
                ev.Reservation.EndTime = model.Reservation.EndTime;
            }

            if (model.EventArtPieces != null)
            {
                ev.EventArtPieces = model.EventArtPieces;
            }

            // Recalculăm bugetul
            int artCount = ev.EventArtPieces?.Count ?? 0;
            int days = (ev.Reservation.EndTime - ev.Reservation.StartTime).Days;
            if (days <= 0) days = 1;
            ev.Budget = (days * 300) + (days * artCount * 200);

            // If admin updated the event and reservation time changed, cascade to conflicting reservations
            if (adminUserId.HasValue && oldReservation != null && 
                (oldReservation.StartTime != model.Reservation.StartTime || 
                 oldReservation.EndTime != model.Reservation.EndTime ||
                 oldReservation.ResourceId != model.Reservation.ResourceId))
            {
                // Find all conflicting reservations with Pending or OverrideRequired status
                var conflictingReservations = await _reservationService.GetExternalUserConflictingReservationsAsync(
                    ev.Reservation.ResourceId,
                    ev.Reservation.BufferStart,
                    ev.Reservation.BufferEnd);

                foreach (var conflictingRes in conflictingReservations)
                {
                    // If the conflicting reservation was marked for override, approve it now
                    if (conflictingRes.Status == ReservationStatus.OverrideRequired)
                    {
                        conflictingRes.Status = ReservationStatus.Confirmed;
                    }
                    else if (conflictingRes.Status == ReservationStatus.PendingApproval)
                    {
                        conflictingRes.Status = ReservationStatus.Confirmed;
                    }
                }

                await _reservationService.SaveChangesAsync();
            }

            return await _eventRepo.SaveChangesAsync();
        }

        // --- Metodele de suport rămase intacte, dar folosind clasa User ---

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
            // Filtrăm după rolul din clasa User
            return users.Where(u => u.Role == UserRole.Member).ToList();
        }
        // EventService.cs
        public async Task<List<Event>> GetEventsByOrganizerIdAsync(string userId)
        {
            // Service-ul doar cere datele de la repository
            return await _eventRepo.GetByOrganizerIdAsync(userId);
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _eventRepo.GetByIdAsync(id);
        }

        public async Task<bool> MarkEventAsPaidAsync(int eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null) return false;

            ev.IsPaid = true; // Presupunând că ai proprietatea IsPaid în entitatea Event
            return await _eventRepo.SaveChangesAsync();
        }

        
    }
}