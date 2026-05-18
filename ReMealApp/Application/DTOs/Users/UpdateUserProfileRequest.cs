namespace Application.DTOs.Users
{
    public sealed class UpdateUserProfileRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }
}
