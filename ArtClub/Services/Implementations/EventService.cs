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
            // 1. Validare obiect primire
            if (model == null || model.Reservation == null) return false;

            // 2. Verificăm disponibilitatea resursei (SALA)
            // Dacă aici se întoarce false, înseamnă că buffer-ul de 1 zi blochează data aleasă.
            var isAvailable = await _reservationService.CheckAvailabilityAsync(
                model.ResourceId,
                model.Reservation.StartTime,
                model.Reservation.EndTime);

            if (!isAvailable) return false;

            // 3. Verificăm bugetul clubului
            // Dacă suma veniturilor din Payments este mai mică decât model.Budget, se va întoarce false.
            var hasFunds = await _financeService.HasClubSufficientFundsAsync(model.Budget);
            if (!hasFunds) return false;

            try
            {
                // 4. Salvare prin Repository
                // Repository-ul tău folosește _context.AddAsync(artEvent) care urmărește și Rezervarea atașată.
                await _eventRepo.AddAsync(model);
                var success = await _eventRepo.SaveChangesAsync();

                // 5. Notificare automată (fără a bloca return-ul dacă eșuează mail-ul)
                if (success)
                {
                    try
                    {
                        var organizer = await _userRepo.GetByIdAsync(model.OrganizerId);
                        if (organizer != null)
                        {
                            await _notificationService.SendEmailAsync(
                                organizer.Email,
                                "Eveniment Creat",
                                $"Evenimentul '{model.Title}' a fost aprobat și înregistrat.");
                        }
                    }
                    catch
                    {
                        // Log eroare email, dar lăsăm success = true pentru că în DB s-a salvat
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                // Aici prinzi erori de Foreign Key sau Database Constraints
                // Poți pune un breakpoint aici să vezi ex.Message
                return false;
            }
        }

        // Restul metodelor rămân la fel, fiind deja corect implementate pentru Repository
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

            return await _eventRepo.SaveChangesAsync();
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
            return await _eventRepo.GetAllResourcesAsync(); // Trebuie să existe în IEventRepository

        }
        public async Task<List<User>> GetAllMembersAsync()
        {
            // Presupunând că IUserRepository are o metodă GetAllAsync sau similară
            var users = await _userRepo.GetAllOrderedByNameAsync();

            // Putem filtra aici membrii dacă repository-ul returnează tot
            return users
                .Where(u => u.Role == UserRole.Member)
                .ToList();
        }
    }

}