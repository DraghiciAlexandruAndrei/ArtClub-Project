using ArtClub.DataAccess;
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
        private readonly IReservationRepository _reservationRepo;
        private readonly ApplicationDbContext _context;

        public EventService(
            IEventRepository eventRepo,
            IReservationService reservationService,
            IFinanceService financeService,
            INotificationService notificationService,
            IUserRepository userRepo,
            IReservationRepository reservationRepo,
            ApplicationDbContext context)
        {
            _eventRepo = eventRepo;
            _reservationService = reservationService;
            _financeService = financeService;
            _notificationService = notificationService;
            _userRepo = userRepo;
            _reservationRepo = reservationRepo;
            _context = context;
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

            // Reject immediately when the time slot is not available
            if (!isAvailable)
            {
                return false;
            }

            // Calculăm costul (Budget-ul devine baza pentru Payment)
            int artCount = model.EventArtPieces?.Count ?? 0;
            int days = (model.Reservation.EndTime - model.Reservation.StartTime).Days;
            if (days <= 0) days = 1;

            model.Budget = (days * 300) + (days * artCount * 200);
            model.IsPaid = false;

            // 1. Creăm strategia de execuție pentru a suporta tranzacții alături de EnableRetryOnFailure
            var strategy = _context.Database.CreateExecutionStrategy();

            // 2. Executăm tot blocul ce implică tranzacția prin intermediul strategiei
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    if (adminUserId.HasValue)
                    {
                        var conflictingReservations = await _reservationRepo.GetAllConflictingReservationsAsync(
                            model.Reservation.ResourceId,
                            model.Reservation.StartTime,
                            model.Reservation.EndTime);

                        foreach (var conflictingRes in conflictingReservations)
                        {
                            if (conflictingRes.Event != null)
                            {
                                if (conflictingRes.Event.IsPaid)
                                {
                                    await _financeService.CreatePaymentAsync(new Payment
                                    {
                                        UserId = conflictingRes.Event.OrganizerId,
                                        Amount = conflictingRes.Event.Budget,
                                        Date = DateTime.Now,
                                        IsIncome = false,
                                        Type = PaymentType.Expense,
                                        Description = $"Refund for cancelled event: {conflictingRes.Event.Title}"
                                    });
                                }

                                _context.EventArtPieces.RemoveRange(_context.EventArtPieces.Where(eap => eap.EventId == conflictingRes.Event.Id));
                                _context.Invitations.RemoveRange(_context.Invitations.Where(i => i.EventId == conflictingRes.Event.Id));
                                _eventRepo.Remove(conflictingRes.Event);
                            }
                            else
                            {
                                conflictingRes.Status = ReservationStatus.Cancelled;
                            }
                        }

                        model.Reservation.IsAdminOverride = true;
                        model.Reservation.AdminOverrideById = adminUserId;
                        model.Reservation.Status = ReservationStatus.Confirmed;
                        model.Reservation.OverrideCreatedAt = DateTime.UtcNow;
                    }

                    await _eventRepo.AddAsync(model);
                    var success = await _eventRepo.SaveChangesAsync();

                    if (!success)
                    {
                        await transaction.RollbackAsync();
                        return false;
                    }

                    // Dacă am ajuns aici, totul e salvat cu succes în DB
                    await transaction.CommitAsync();

                    // Notificarea se trimite după ce tranzacția e cu siguranță confirmată
                    await _notificationService.SendEmailAsync(user.Email, "Eveniment Creat",
                        $"Evenimentul '{model.Title}' a fost creat. Taxă datorată: {model.Budget} lei.");

                    return true;
                }
                catch (Exception ex)
                {
                    // Dacă apare o eroare, anulăm tranzacția
                    await transaction.RollbackAsync();

                    System.Diagnostics.Debug.WriteLine($"Eroare la creare eveniment: {ex.Message}");
                    throw; // Re-aruncăm eroarea (astfel încât strategia EF să știe dacă e cazul să reîncerce)
                }
            });
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

            int artCount = ev.EventArtPieces?.Count ?? 0;
            int days = (ev.Reservation.EndTime - ev.Reservation.StartTime).Days;
            if (days <= 0) days = 1;
            ev.Budget = (days * 300) + (days * artCount * 200);

            return await _eventRepo.SaveChangesAsync();
        }

        public async Task<bool> UpdateEventAsync(string originalTitle, Event model, int? adminUserId)
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

            int artCount = ev.EventArtPieces?.Count ?? 0;
            int days = (ev.Reservation.EndTime - ev.Reservation.StartTime).Days;
            if (days <= 0) days = 1;
            ev.Budget = (days * 300) + (days * artCount * 200);

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