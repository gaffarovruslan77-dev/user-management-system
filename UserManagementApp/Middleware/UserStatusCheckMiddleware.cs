using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using UserManagementApp.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace UserManagementApp.Middleware
{
    public class UserStatusCheckMiddleware
    {
        private readonly RequestDelegate _next;

        public UserStatusCheckMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var userEmail = context.User.FindFirst(ClaimTypes.Email)?.Value;
                
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);
                    
                    if (user != null && user.IsBlocked)
                    {
                        await context.SignOutAsync();
                        context.Response.Redirect("/Account/Login");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}