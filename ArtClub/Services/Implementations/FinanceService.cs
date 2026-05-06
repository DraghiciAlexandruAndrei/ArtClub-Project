using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using ArtClub.Services.Interfaces;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace ArtClub.Services.Implementations
{
    public class FinanceService : IFinanceService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IUserRepository _userRepo;

        // UN SINGUR CONSTRUCTOR: Acesta primește toate dependințele necesare
        public FinanceService(IPaymentRepository paymentRepo, IUserRepository userRepo)
        {
            _paymentRepo = paymentRepo;
            _userRepo = userRepo;
        }

        // ============================================================
        // LOGICA REQ-5 ȘI CHELTUIELI AUTOMATE
        // ============================================================

        public async Task<bool> ProcessMembershipUpgradeAsync(int userId, decimal amount)
        {
            // Înregistrăm plata ca venit
            var payment = new Payment
            {
                Amount = amount,
                Date = DateTime.Now,
                IsIncome = true,
                Description = $"Upgrade Membership - User ID: {userId}"
            };
            await _paymentRepo.AddAsync(payment);

            // Deblocăm contul membrului conform REQ-5
            // Folosim "as Member" pentru a accesa proprietățile specifice (IsMembershipActive)
            var user = await _userRepo.GetByIdAsync(userId) as Member;
            if (user == null) return false;

            user.IsMembershipActive = true;
            user.EventCreationLimit = 5;

            await _userRepo.UpdateAsync(user);
            return await _paymentRepo.SaveChangesAsync();
        }

        public async Task<bool> RegisterEventExpensesAsync(int eventId, decimal amount)
        {
            // Înregistrează automat cheltuielile de 200/300 lei per eveniment
            var expense = new Payment
            {
                Amount = amount,
                Date = DateTime.Now,
                IsIncome = false,
                Description = $"Cheltuieli automate Eveniment ID: {eventId}"
            };

            await _paymentRepo.AddAsync(expense);
            return await _paymentRepo.SaveChangesAsync();
        }

        public decimal CalculateNonMemberReservationFee(int days)
        {
            // Taxa pentru utilizatorii fără abonament (400 lei/zi conform cerințelor)
            return days * 400;
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
            var totalIncome = await GetTotalIncomeAsync();
            var totalExpenses = await GetTotalExpensesAsync();
            return (totalIncome - totalExpenses) >= projectedCost;
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
                var fontTitle = new XFont("Verdana", 20, XFontStyleEx.Bold);
                var fontText = new XFont("Verdana", 12, XFontStyleEx.Regular);

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