using Domain.Enums;

namespace Application.DTOs.Profile
{
    public sealed class AdminRoleDistributionDto
    {
        public UserRole Role { get; set; }

        public string RoleText { get; set; } = string.Empty;

        public int Count { get; set; }

        public double Percentage { get; set; }
    }
}
