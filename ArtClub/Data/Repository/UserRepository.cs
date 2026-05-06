using ArtClub.DataAccess;
using ArtClub.DataAccess.Interfaces;
using ArtClub.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ArtClub.DataAccess.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) => _context = context;

        public async Task<bool> ExistsByEmailAsync(string email) =>
            await _context.Users.AnyAsync(u => u.Email == email);

        public async Task<User?> GetByEmailAsync(string email) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<User?> GetByIdAsync(int id) =>
            await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<List<User>> GetAllOrderedByNameAsync() =>
            await _context.Users.OrderBy(u => u.UserName).ToListAsync();

        public async Task AddAsync(User user) => await _context.Users.AddAsync(user);

        public void Remove(User user) => _context.Users.Remove(user);

        public async Task<bool> SaveChangesAsync() => await _context.SaveChangesAsync() > 0;

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}