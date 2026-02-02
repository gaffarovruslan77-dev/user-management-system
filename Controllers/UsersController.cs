using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;

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

            var users = await _context.Users.ToListAsync();
            return View(users);
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

            if (ids.Contains(userId.Value))
            {
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

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
            foreach (var user in users)
            {
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
    }
}