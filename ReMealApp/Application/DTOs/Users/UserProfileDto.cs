using Domain.Enums;

namespace Application.DTOs.Users
{
    public sealed class UserProfileDto
    {
        public Guid Id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public UserRole Role { get; set; }
    }
}
