using System.ComponentModel.DataAnnotations;
namespace UserManagementApp.Models
{
    public class User
    {
        public int Id { get; set; }
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        
        public DateTime RegistrationTime { get; set; }
        
        public DateTime? LastLoginTime { get; set; }
        
        public bool IsBlocked { get; set; }
        
        public bool IsDeleted { get; set; }
        
        public bool IsEmailVerified { get; set; }
        
        public string? EmailVerificationToken { get; set; }
        
        // Новое поле для хранения статуса блокировки перед удалением
        public bool WasBlockedBeforeDelete { get; set; }
    }
}