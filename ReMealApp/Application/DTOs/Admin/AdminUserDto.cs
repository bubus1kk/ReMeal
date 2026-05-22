using Domain.Enums;

namespace Application.DTOs.Admin
{
    public sealed class AdminUserDto
    {
        public Guid Id { get; init; }

        public string Login { get; init; } = string.Empty;

        public string FullName { get; init; } = string.Empty;

        public string Email { get; init; } = string.Empty;

        public string Phone { get; init; } = string.Empty;

        public UserRole Role { get; init; }

        public bool? IsActive { get; init; }

        public string RoleText => Role switch
        {
            UserRole.StudentCustomer => "Покупатель",
            UserRole.FoodPointRepresentative => "Представитель точки",
            UserRole.Administrator => "Администратор",
            _ => Role.ToString()
        };

        public string ActivityStatusText => IsActive switch
        {
            true => "Активен",
            false => "Деактивирован",
            null => "Не хранится"
        };
    }
}
