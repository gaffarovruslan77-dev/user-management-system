using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace UserManagementApp.Controllers
{
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var currentUser = await _context.Users.FindAsync(userId.Value);
            if (currentUser == null || currentUser.IsBlocked)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            var allUsers = await _context.Users
                .Select(u => new UserListViewModel
                {
                    Id = u.Id,
                    Name = u.Name,
                    Email = u.Email,
                    Organization = string.IsNullOrWhiteSpace(u.Organization) ? "N/A" : u.Organization,
                    RegistrationTime = u.RegistrationTime,
                    LastLoginTime = u.LastLoginTime,
                    IsBlocked = u.IsBlocked,
                    IsEmailVerified = u.IsEmailVerified
                })
                .ToListAsync();
            
            // Сортировка: текущий пользователь первый, затем остальные по статусу и алфавиту
            var sortedUsers = allUsers
                .OrderBy(u => u.Id != userId.Value ? 0 : -1)
                .ThenBy(u => u.Id == userId.Value ? 0 : GetStatusOrder(u))
                .ThenBy(u => u.Id == userId.Value ? "" : u.Name)
                .ToList();

            ViewBag.CurrentUserId = userId.Value;
            return View(sortedUsers);
        }

        private int GetStatusOrder(UserListViewModel user)
        {
            if (user.IsBlocked) return 3;
            if (!user.IsEmailVerified) return 2;
            return 1; // Active
        }

        [HttpPost]
        public async Task<IActionResult> Block(int[] ids)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (ids == null || ids.Length == 0)
                return RedirectToAction("Index");

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
                user.IsBlocked = true;
            }

            await _context.SaveChangesAsync();

            // Если пользователь заблокировал себя - выходим
            if (ids.Contains(userId.Value))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Unblock(int[] ids)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (ids == null || ids.Length == 0)
                return RedirectToAction("Index");

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
                user.IsBlocked = false;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int[] ids)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (ids == null || ids.Length == 0)
                return RedirectToAction("Index");

            // Если пользователь удаляет себя - СНАЧАЛА выходим, ПОТОМ удаляем
            if (ids.Contains(userId.Value))
            {
                // РЕАЛЬНОЕ УДАЛЕНИЕ из базы данных
                var usersToDelete = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
                _context.Users.RemoveRange(usersToDelete);
                await _context.SaveChangesAsync();

                // ✅ ВАЖНО: SignOut ПОСЛЕ удаления из базы, чтобы избежать редиректов
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            // Обычное удаление других пользователей
            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllUnverified()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            // Находим всех unverified пользователей
            var unverifiedUsers = await _context.Users
                .Where(u => !u.IsEmailVerified)
                .ToListAsync();

            if (unverifiedUsers.Any())
            {
                _context.Users.RemoveRange(unverifiedUsers);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }
    }
}