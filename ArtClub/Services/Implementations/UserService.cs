using ArtClub.Models.Entities;
using ArtClub.Services.Interfaces;
using ArtClub.DataAccess.Interfaces;

namespace ArtClub.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<bool> RegisterUserAsync(User user, string password)
        {
            // Verificăm dacă email-ul este deja folosit via Repository
            if (await _userRepo.ExistsByEmailAsync(user.Email))
                return false;

            // Simulare password hashing
            user.PasswordHash = password;

            await _userRepo.AddAsync(user);
            return await _userRepo.SaveChangesAsync();
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null) return false;

            // Verificare simplă
            return user.PasswordHash == password;
        }

        public async Task<bool> CheckUserStatusAsync(int userId)
        {
            var user = await _userRepo.GetByIdAsync(userId);
            return user?.IsActive ?? false;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepo.GetAllOrderedByNameAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _userRepo.GetByIdAsync(id);
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            var existingUser = await _userRepo.GetByIdAsync(user.Id);

            if (existingUser == null)
                return false;

            existingUser.UserName = user.UserName;
            existingUser.Email = user.Email;
            existingUser.Role = user.Role;
            existingUser.IsActive = user.IsActive;

            return await _userRepo.SaveChangesAsync();
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);

            if (user == null)
                return false;

            _userRepo.Remove(user);
            return await _userRepo.SaveChangesAsync();
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepo.GetByEmailAsync(email);
        }
    }
}