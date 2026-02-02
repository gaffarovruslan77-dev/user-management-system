using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;

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
            if (currentUser == null || currentUser.IsBlocked || currentUser.IsDeleted)
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
                    IsDeleted = u.IsDeleted,
                    WasBlockedBeforeDelete = u.WasBlockedBeforeDelete
                })
                .ToListAsync();
            
            // Сортировка: текущий пользователь первый, затем остальные по статусу и алфавиту
            var sortedUsers = allUsers
                .OrderBy(u => u.Id != userId.Value ? 0 : -1) // Текущий пользователь первый
                .ThenBy(u => u.Id == userId.Value ? 0 : GetStatusOrder(u)) // Сортировка по статусу
                .ThenBy(u => u.Id == userId.Value ? "" : u.Name) // Сортировка по имени
                .ToList();

            // Передаём ID текущего пользователя в View
            ViewBag.CurrentUserId = userId.Value;

            return View(sortedUsers);
        }

        private int GetStatusOrder(UserListViewModel user)
        {
            if (user.IsDeleted) return 3;
            if (user.IsBlocked) return 2;
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

            // Убираем текущего пользователя из списка блокировки
            var idsToBlock = ids.Where(id => id != userId.Value).ToArray();
            
            if (idsToBlock.Length == 0)
                return RedirectToAction("Index");

            var users = await _context.Users.Where(u => idsToBlock.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
                user.IsBlocked = true;
            }

            await _context.SaveChangesAsync();
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

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
                // Сохраняем статус блокировки перед удалением
                user.WasBlockedBeforeDelete = user.IsBlocked;
                user.IsDeleted = true;
            }

            await _context.SaveChangesAsync();

            if (ids.Contains(userId.Value))
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Undelete(int[] ids, bool unblockAll = false)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            if (ids == null || ids.Length == 0)
                return RedirectToAction("Index");

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
                user.IsDeleted = false;
                
                // Если unblockAll=true или пользователь не был заблокирован, делаем Active
                if (unblockAll || !user.WasBlockedBeforeDelete)
                {
                    user.IsBlocked = false;
                }
                // Иначе восстанавливаем статус блокировки
                else
                {
                    user.IsBlocked = user.WasBlockedBeforeDelete;
                }
                
                // Сбрасываем флаг после восстановления
                user.WasBlockedBeforeDelete = false;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UndeleteAll()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var deletedUsers = await _context.Users.Where(u => u.IsDeleted).ToListAsync();
            
            foreach (var user in deletedUsers)
            {
                user.IsDeleted = false;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}