using ArtClub.Models.Entities;

namespace ArtClub.DataAccess.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<List<User>> GetAllOrderedByNameAsync();
        Task AddAsync(User user);
        void Remove(User user);
        Task UpdateAsync(User user);
        Task<bool> SaveChangesAsync();
    }
}