using Domain.Enums;

namespace Application.DTOs.Auth
{
    public sealed class RegisterUserRequest
    {
        public string Login { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.StudentCustomer;
    }
}
