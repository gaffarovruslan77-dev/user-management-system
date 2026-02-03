using UserManagementApp.Models;
using BCrypt.Net;

namespace UserManagementApp.Data
{
    public class DatabaseSeeder
    {
        public static void SeedUsers(ApplicationDbContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var users = new List<User>
            {
                new User
                {
                    Name = "Alice Johnson",
                    Email = "alice@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RegistrationTime = DateTime.UtcNow.AddDays(-30),
                    IsBlocked = false,
                    IsEmailVerified = true
                },
                new User
                {
                    Name = "Bob Smith",
                    Email = "bob@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RegistrationTime = DateTime.UtcNow.AddDays(-20),
                    IsBlocked = false,
                    IsEmailVerified = true
                },
                new User
                {
                    Name = "Charlie Brown",
                    Email = "charlie@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    RegistrationTime = DateTime.UtcNow.AddDays(-10),
                    IsBlocked = false,
                    IsEmailVerified = false
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }
    }
}