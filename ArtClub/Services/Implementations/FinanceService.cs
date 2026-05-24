using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ArtClub.Services.Implementations
{
    public class FinanceService : IFinanceService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IUserRepository _userRepo;
        private readonly ApplicationDbContext _context;

        public FinanceService(IPaymentRepository paymentRepo, IUserRepository userRepo, ApplicationDbContext context)
        {
            _paymentRepo = paymentRepo;
            _userRepo = userRepo;
            _context = context;
        }

        // ============================================================
        // CLUBSETTINGS MANAGEMENT (REQ-44)
        // ============================================================

        /// <summary>
        /// Gets or creates the singleton ClubSettings entity.
        /// </summary>
        private async Task<ClubSettings> GetOrCreateClubSettingsAsync()
        {
            var settings = await _context.ClubSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new ClubSettings();
                await _context.ClubSettings.AddAsync(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        // ============================================================
        // LOGICA REQ-5 ȘI CHELTUIELI AUTOMATE
        // ============================================================

        public async Task<bool> ProcessMembershipUpgradeAsync(int userId, decimal amount)
        {
            // 1. Înregistrăm plata ca venit
            var payment = new Payment
            {
                Amount = amount,
                Date = DateTime.Now,
                IsIncome = true,
                Description = $"Upgrade Membership - User ID: {userId}"
            };
            await _paymentRepo.AddAsync(payment);

            // 2. Deblocăm contul membrului conform REQ-5
            // Schimbare: Nu mai facem cast "as Member", proprietățile sunt în User
            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsMembershipActive = true;
            user.MembershipDate = DateTime.Now; // Setăm data activării
            user.EventCreationLimit = 5; // Limita ridicată conform REQ-5

            // Actualizăm utilizatorul folosind repository-ul (care lucrează acum cu DbSet<User>)
            await _userRepo.UpdateAsync(user);

            // Salvăm ambele modificări (plata și statusul userului)
            return await _paymentRepo.SaveChangesAsync();
        }

        public async Task<bool> RegisterEventExpensesAsync(int eventId, decimal amount)
        {
            var expense = new Payment
            {
                Amount = amount,
                Date = DateTime.Now,
                IsIncome = false,
                Type = PaymentType.Expense,
                Description = $"Cheltuieli automate Eveniment ID: {eventId}"
            };

            await _paymentRepo.AddAsync(expense);
            return await _paymentRepo.SaveChangesAsync();
        }

        public decimal CalculateNonMemberReservationFee(int days)
        {
            // 400 lei/zi conform cerințelor pentru non-membri
            return days * 400;
        }

        /// <summary>
        /// REQ-44: Calculates non-member reservation fee using configurable ClubSettings.
        /// </summary>
        public async Task<decimal> CalculateNonMemberReservationFeeAsync(int days)
        {
            var settings = await GetOrCreateClubSettingsAsync();
            return days * settings.NonMemberReservationFeePerDay;
        }

        // ==========================================
        // REQ-47: MONTHLY DEFICIT RULE
        // ==========================================

        /// <summary>
        /// REQ-47: Checks if member reservations are blocked due to monthly deficit.
        /// Returns true if last month's expenses exceed income (blocked for non-admins).
        /// </summary>
        public async Task<bool> IsReservationBlockedForMembersAsync()
        {
            var now = DateTime.Now;
            var lastMonth = now.Month == 1 ? 12 : now.Month - 1;
            var lastMonthYear = now.Month == 1 ? now.Year - 1 : now.Year;

            var income = await _paymentRepo.GetSumAsync(lastMonth, lastMonthYear, true);
            var expenses = await _paymentRepo.GetSumAsync(lastMonth, lastMonthYear, false);

            return expenses > income;
        }

        // ==========================================
        // LOGICĂ MANAGEMENT PLĂȚI ȘI RAPOARTE
        // ==========================================

        public async Task<List<Payment>> GetAllPaymentsAsync() =>
            await _paymentRepo.GetAllOrderedByDateAsync();

        public async Task<decimal> GetTotalExpensesAsync() =>
            await _paymentRepo.GetTotalSumAsync(false);

        public async Task<decimal> GetTotalIncomeAsync() =>
            await _paymentRepo.GetTotalSumAsync(true);

        public async Task<decimal> CalculateMonthlyBalanceAsync()
        {
            var now = DateTime.Now;
            var income = await _paymentRepo.GetSumAsync(now.Month, now.Year, true);
            var expenses = await _paymentRepo.GetSumAsync(now.Month, now.Year, false);
            return income - expenses;
        }

        public async Task<bool> HasClubSufficientFundsAsync(decimal projectedCost)
        {
            var balance = await GetTotalIncomeAsync() - await GetTotalExpensesAsync();
            return balance >= projectedCost;
        }

        public async Task<byte[]> GenerateMonthlyReportAsync(int month, int year)
        {
            var income = await _paymentRepo.GetSumAsync(month, year, true);
            var expenses = await _paymentRepo.GetSumAsync(month, year, false);
            var payments = await _paymentRepo.GetPaymentsByPeriodAsync(month, year);

            using (var document = new PdfDocument())
            {
                var page = document.AddPage();
                var gfx = XGraphics.FromPdfPage(page);
                var fontTitle = new XFont("Arial", 20, XFontStyleEx.Bold);
                var fontText = new XFont("Arial", 12, XFontStyleEx.Regular);

                gfx.DrawString($"Raport Financiar {month}/{year}", fontTitle, XBrushes.Black, new XRect(0, 40, page.Width, 40), XStringFormats.Center);

                int yPos = 100;
                gfx.DrawString($"Total Venituri: {income} lei", fontText, XBrushes.Green, 50, yPos);
                gfx.DrawString($"Total Cheltuieli: {expenses} lei", fontText, XBrushes.Red, 50, yPos + 20);
                gfx.DrawString($"Bilanț: {income - expenses} lei", fontText, XBrushes.Black, 50, yPos + 40);

                yPos += 80;
                foreach (var p in payments)
                {
                    yPos += 20;
                    gfx.DrawString($"{p.Date.ToShortDateString()} | {p.Amount} lei | {(p.IsIncome ? "Venit" : "Cheltuială")}", fontText, XBrushes.Black, 50, yPos);
                    if (yPos > page.Height - 50) break;
                }

                using (var stream = new MemoryStream())
                {
                    document.Save(stream, false);
                    return stream.ToArray();
                }
            }
        }

        public async Task<Payment?> GetPaymentByIdAsync(int id) => await _paymentRepo.GetByIdAsync(id);

        public async Task CreatePaymentAsync(Payment payment)
        {
            await _paymentRepo.AddAsync(payment);
            await _paymentRepo.SaveChangesAsync();
        }

        public async Task<bool> UpdatePaymentAsync(Payment payment)
        {
            var existing = await _paymentRepo.GetByIdAsync(payment.Id);
            if (existing == null) return false;

            existing.Amount = payment.Amount;
            existing.Date = payment.Date;
            existing.IsIncome = payment.IsIncome;
            existing.Description = payment.Description;

            return await _paymentRepo.SaveChangesAsync();
        }

        public async Task<bool> DeletePaymentAsync(int id)
        {
            var payment = await _paymentRepo.GetByIdAsync(id);
            if (payment == null) return false;

            _paymentRepo.Remove(payment);
            return await _paymentRepo.SaveChangesAsync();
        }
    }
}