namespace Application.DTOs.Auth
{
    public sealed class LoginUserRequest
    {
        public string Login { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
