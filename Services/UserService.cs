using MongoDB.Driver;
using Microsoft.Extensions.Options;
using BC = BCrypt.Net.BCrypt;
using cakeshop_api.Models;

namespace cakeshop_api.Services
{
    public class UserService
    {
        private readonly IMongoCollection<User> _users;

        public UserService(IMongoDatabase database)
        {
            _users = database.GetCollection<User>("Users");
        }

        public async Task<User> CreateUser(string username, string email, string password, string role, string phone, string address)
        {
            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BC.HashPassword(password),
                Role = role,
                PhoneNumber = phone,
                Address = address
            };

            await _users.InsertOneAsync(user);
            return user;
        }

        public async Task<List<User>> GetAllUsers()
        {
            return await _users.Find(user => true).ToListAsync();
        }

        public async Task<User> GetUserByEmail(string email)
        {
            return await _users.Find(user => user.Email == email).FirstOrDefaultAsync();
        }

        public Task<bool> ValidatePassword(User user, string password)
        {
            return Task.FromResult(BC.Verify(password, user.PasswordHash));
        }
    }
}