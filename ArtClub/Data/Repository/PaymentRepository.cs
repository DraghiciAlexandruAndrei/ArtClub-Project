using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.DataAccess.Repositories
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly ApplicationDbContext _context;

        public PaymentRepository(ApplicationDbContext context) => _context = context;

        public async Task<decimal> GetSumAsync(int month, int year, bool isIncome) =>
            await _context.Payments
                .Where(p => p.Date.Month == month && p.Date.Year == year && p.IsIncome == isIncome)
                .SumAsync(p => p.Amount);

        public async Task<decimal> GetTotalSumAsync(bool isIncome) =>
            await _context.Payments
                .Where(p => p.IsIncome == isIncome)
                .SumAsync(p => p.Amount);

        public async Task<List<Payment>> GetPaymentsByPeriodAsync(int month, int year) =>
            await _context.Payments
                .Where(p => p.Date.Month == month && p.Date.Year == year)
                .OrderBy(p => p.Date)
                .ToListAsync();

        public async Task<List<Payment>> GetAllOrderedByDateAsync() =>
            await _context.Payments.OrderByDescending(p => p.Date).ToListAsync();

        public async Task<Payment?> GetByIdAsync(int id) =>
            await _context.Payments.FirstOrDefaultAsync(p => p.Id == id);

        public async Task AddAsync(Payment payment) => await _context.Payments.AddAsync(payment);

        public void Remove(Payment payment) => _context.Payments.Remove(payment);

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;
    }
}