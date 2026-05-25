using ArtClub.Models.Entities;

namespace ArtClub.DataAccess.Interfaces
{
    public interface IPaymentRepository
    {
        Task<decimal> GetSumAsync(int month, int year, bool isIncome);
        Task<decimal> GetTotalSumAsync(bool isIncome);
        Task<List<Payment>> GetPaymentsByPeriodAsync(int month, int year);
        Task<List<Payment>> GetAllOrderedByDateAsync();
        Task<Payment?> GetByIdAsync(int id);
        Task AddAsync(Payment payment);
        void Remove(Payment payment);
        Task<bool> SaveChangesAsync();
    }
}