using Application.DTOs.Auth;
using Application.DTOs.Users;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Guid? CurrentUserId { get; }

        Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

        Task<AuthResult> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default);

        Task<UserProfileDto?> TryRestoreRememberedUserAsync(CancellationToken cancellationToken = default);

        Task<UserProfileDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

        void Logout();
    }
}
