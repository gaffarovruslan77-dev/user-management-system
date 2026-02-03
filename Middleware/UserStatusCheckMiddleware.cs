using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace UserManagementApp.Middleware
{
    public class UserStatusCheckMiddleware
    {
        private readonly RequestDelegate _next;
        
        // Список публичных путей, которые не требуют проверки
        private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Account/Login",
            "/Account/Register",
            "/Account/Logout",
            "/Account/ForgotPassword",
            "/Account/ResetPassword",
            "/Account/VerifyEmail",
            "/Home/Privacy",
            "/Home/Error"
        };

        public UserStatusCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value ?? string.Empty;

            // Пропускаем публичные пути и статические файлы
            if (IsPublicPath(path) || path.StartsWith("/css") || path.StartsWith("/js") || 
                path.StartsWith("/lib") || path.StartsWith("/images") || path.StartsWith("/favicon"))
            {
                await _next(context);
                return;
            }

            // Проверяем только аутентифицированных пользователей
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
                
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                    
                    // ✅ ИСПРАВЛЕНИЕ: Если пользователь НЕ НАЙДЕН (удалён) или заблокирован - разлогиниваем
                    if (user == null)
                    {
                        // Пользователь был удалён из базы - выходим
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Session.Clear();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                    
                    if (user.IsBlocked)
                    {
                        // Пользователь заблокирован - выходим
                        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        context.Session.Clear();
                        context.Response.Redirect("/Account/Login?blocked=true");
                        return;
                    }
                }
            }

            await _next(context);
        }

        private bool IsPublicPath(string path)
        {
            return PublicPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}