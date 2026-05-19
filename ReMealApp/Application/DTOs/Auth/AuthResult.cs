using Application.DTOs.Users;

namespace Application.DTOs.Auth
{
    public sealed class AuthResult
    {
        public bool IsSuccess { get; set; }

        public string? ErrorMessage { get; set; }

        public UserProfileDto? User { get; set; }

        public static AuthResult Success(UserProfileDto user)
        {
            return new AuthResult
            {
                IsSuccess = true,
                User = user
            };
        }

        public static AuthResult Failure(string errorMessage)
        {
            return new AuthResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
        }
    }
}
