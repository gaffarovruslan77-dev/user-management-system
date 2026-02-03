using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using UserManagementApp.Models;
using UserManagementApp.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace UserManagementApp.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly EmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(ApplicationDbContext context, EmailService emailService, ILogger<AccountController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Users");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == model.Email))
        {
            ModelState.AddModelError("Email", "This email is already registered");
            return View(model);
        }

        // Create new user with Unverified status
        var user = new User
        {
            Name = model.Name,
            Email = model.Email,
            Organization = model.Organization,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            RegistrationTime = DateTime.UtcNow,
            LastLoginTime = DateTime.UtcNow,
            IsBlocked = false,
            IsEmailVerified = false,  // Unverified status
            EmailVerificationToken = Guid.NewGuid().ToString(),
            PasswordResetToken = null,
            PasswordResetTokenExpiry = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // ✅ ГЕНЕРИРУЕМ URL ДО Task.Run() - иначе HttpContext будет disposed
        var verificationUrl = Url.Action(
            "VerifyEmail",
            "Account",
            new { token = user.EmailVerificationToken },
            Request.Scheme
        );

        // Send verification email (fire-and-forget)
        _ = Task.Run(async () =>
        {
            try
            {
                await _emailService.SendConfirmationEmailAsync(user.Email, user.Name, verificationUrl!);
                _logger.LogInformation($"Verification email sent to {user.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to {user.Email}");
            }
        });

        // Redirect to login page with success message
        TempData["SuccessMessage"] = "Registration successful! Please sign in with your credentials.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Invalid verification link";
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Verification link is invalid";
            return RedirectToAction("Login");
        }

        // Update user status to Active (IsEmailVerified = true)
        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Email verified successfully! Your status is now Active.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Users");
        }
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password");
            return View(model);
        }

        // Check if user is blocked
        if (user.IsBlocked)
        {
            ModelState.AddModelError(string.Empty, "Your account has been blocked");
            return View(model);
        }

        // Update last login time
        user.LastLoginTime = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Create claims and sign in
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties
        );

        // ВАЖНО: Устанавливаем Session для совместимости с UsersController
        HttpContext.Session.SetInt32("UserId", user.Id);

        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
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
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

        if (user != null && !user.IsBlocked)
        {
            // Generate reset token
            user.PasswordResetToken = Guid.NewGuid().ToString();
            user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // ✅ ГЕНЕРИРУЕМ URL ДО Task.Run() - иначе HttpContext будет disposed
            var resetUrl = Url.Action(
                "ResetPassword",
                "Account",
                new { token = user.PasswordResetToken },
                Request.Scheme
            );

            // Send reset email (fire-and-forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendPasswordResetEmailAsync(user.Email, user.Name, resetUrl!);
                    _logger.LogInformation($"Password reset email sent to {user.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send password reset email to {user.Email}");
                }
            });
        }

        // Always show success message (security best practice)
        TempData["SuccessMessage"] = "If an account exists with this email, a password reset link has been sent.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Invalid reset link";
            return RedirectToAction("Login");
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.PasswordResetToken == token && 
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Reset link is invalid or expired";
            return RedirectToAction("Login");
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => 
            u.PasswordResetToken == model.Token && 
            u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Reset link is invalid or expired";
            return RedirectToAction("Login");
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Password reset successfully! You can now sign in.";
        return RedirectToAction("Login");
    }
}