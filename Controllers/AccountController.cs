using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using BCrypt.Net;

namespace UserManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists");
                return View(model);
            }

            var user = new User
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Organization = string.IsNullOrWhiteSpace(model.Organization) ? null : model.Organization,
                RegistrationTime = DateTime.UtcNow.AddHours(5),
                IsBlocked = false,
                IsDeleted = false,
                IsEmailVerified = true,
                EmailVerificationToken = null
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Registration successful! You can now login.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password");
                return View(model);
            }

            if (user.IsBlocked)
            {
                ModelState.AddModelError(string.Empty, "Your account has been blocked");
                return View(model);
            }

            if (user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "Your account has been deleted");
                return View(model);
            }

            user.LastLoginTime = DateTime.UtcNow.AddHours(5);
            await _context.SaveChangesAsync();

            HttpContext.Session.SetInt32("UserId", user.Id);
            HttpContext.Session.SetString("UserEmail", user.Email);

            return RedirectToAction("Index", "Users");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                TempData["Message"] = "If your email exists in our system, you will receive a password reset link shortly.";
                return RedirectToAction("Login");
            }

            if (user.IsDeleted)
            {
                ModelState.AddModelError(string.Empty, "This account has been deleted");
                return View(model);
            }

            // Генерируем токен восстановления
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(6); // UTC+5 = 6 hours from UTC

            await _context.SaveChangesAsync();

            // Формируем ссылку для восстановления
            var resetUrl = Url.Action("ResetPassword", "Account", 
                new { token = user.PasswordResetToken }, 
                Request.Scheme);

            // ============================================================
            // ВРЕМЕННЫЙ РЕЖИМ: Вывод ссылки в консоль вместо отправки email
            // ============================================================
            Console.WriteLine("=================================================");
            Console.WriteLine("PASSWORD RESET REQUEST");
            Console.WriteLine("=================================================");
            Console.WriteLine($"User: {user.Name} ({user.Email})");
            Console.WriteLine($"Reset URL: {resetUrl}");
            Console.WriteLine("=================================================");
            Console.WriteLine("⚠️ Email отправка отключена - скопируйте ссылку выше");
            Console.WriteLine("=================================================");

            // Закомментировано для тестирования без email
            /*
            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetUrl);
                TempData["Message"] = "Password reset link has been sent to your email.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email Error: {ex.Message}");
                TempData["Error"] = "Failed to send email. Please try again later.";
            }
            */

            TempData["Message"] = "Password reset link generated! Check the console for the link (temporary mode).";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

            if (user == null || user.PasswordResetTokenExpiry == null || 
                user.PasswordResetTokenExpiry < DateTime.UtcNow.AddHours(5))
            {
                TempData["Error"] = "Invalid or expired password reset token.";
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == model.Token);

            if (user == null || user.PasswordResetTokenExpiry == null || 
                user.PasswordResetTokenExpiry < DateTime.UtcNow.AddHours(5))
            {
                ModelState.AddModelError(string.Empty, "Invalid or expired password reset token.");
                return View(model);
            }

            // Обновляем пароль
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            Console.WriteLine("=================================================");
            Console.WriteLine($"✅ Password successfully reset for: {user.Email}");
            Console.WriteLine("=================================================");

            TempData["Message"] = "Your password has been reset successfully! You can now login with your new password.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}