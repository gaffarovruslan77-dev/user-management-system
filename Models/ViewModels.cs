using System.ComponentModel.DataAnnotations;

namespace UserManagementApp.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(1, ErrorMessage = "Password must be at least 1 character")]
        public string Password { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Organization cannot exceed 200 characters")]
        public string? Organization { get; set; }
    }

    public class UserListViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Organization { get; set; } = "N/A";
        public DateTime RegistrationTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
        public bool WasBlockedBeforeDelete { get; set; }
    }
}