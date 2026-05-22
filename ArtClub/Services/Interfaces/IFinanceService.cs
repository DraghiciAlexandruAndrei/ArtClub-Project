using ArtClub.Models.Entities;

namespace ArtClub.Services.Interfaces
{
    public interface IFinanceService
    {
        Task<decimal> CalculateMonthlyBalanceAsync();
        Task<bool> HasClubSufficientFundsAsync(decimal projectedCost);
        Task<byte[]> GenerateMonthlyReportAsync(int month, int year);

        // Management Plăți
        Task<decimal> GetTotalIncomeAsync();
        Task<decimal> GetTotalExpensesAsync();
        Task<List<Payment>> GetAllPaymentsAsync();
        Task<Payment?> GetPaymentByIdAsync(int id);
        Task CreatePaymentAsync(Payment payment);
        Task<bool> UpdatePaymentAsync(Payment payment);
        Task<bool> DeletePaymentAsync(int id);

        /// <summary>
        /// Înregistrează automat cheltuielile unui eveniment (200 lei/artă, 300 lei/locație).
        /// Acestea sunt incluse în totalul lunii curente.
        /// </summary>
        Task<bool> RegisterEventExpensesAsync(int eventId, decimal amount);

        /// <summary>
        /// Procesează plata pentru abonament și face upgrade contului (IsMembershipActive = true).
        /// </summary>
        Task<bool> ProcessMembershipUpgradeAsync(int userId, decimal amount);

        /// <summary>
        /// Calculează taxa de rezervare pentru non-membri (400 lei/zi).
        /// </summary>
        decimal CalculateNonMemberReservationFee(int days);

        /// <summary>
        /// REQ-44: Calculates non-member reservation fee using configurable ClubSettings from database.
        /// </summary>
        Task<decimal> CalculateNonMemberReservationFeeAsync(int days);

        /// <summary>
        /// REQ-47: Checks if member reservations are blocked due to monthly deficit.
        /// Returns true if last month's expenses exceed income (blocks non-admin reservations).
        /// </summary>
        Task<bool> IsReservationBlockedForMembersAsync();
    }
}
