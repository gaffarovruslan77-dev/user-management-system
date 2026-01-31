using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;

namespace UserManagementApp.Controllers
{
    [Authorize]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.RegistrationTime)
                .ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> Block(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            
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
            if (ids == null || ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

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
            if (ids == null || ids.Length == 0)
            {
                return RedirectToAction("Index");
            }

            var users = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Index");
        }
    }
}