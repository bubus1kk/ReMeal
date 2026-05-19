using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;

namespace Tests.TestSupport
{
    internal sealed class TestAuthService : IAuthService
    {
        private UserProfileDto? _currentUser;

        public Guid? CurrentUserId => _currentUser?.Id;

        public Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use service-specific test setup instead.");
        }

        public Task<AuthResult> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use service-specific test setup instead.");
        }

        public Task<UserProfileDto?> TryRestoreRememberedUserAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_currentUser);
        }

        public Task<UserProfileDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_currentUser);
        }

        public void SetCurrentUser(User user)
        {
            _currentUser = new UserProfileDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role
            };
        }

        public void SetCurrentUser(Guid id, UserRole role)
        {
            _currentUser = new UserProfileDto
            {
                Id = id,
                Login = $"user-{id:N}",
                FullName = "Test User",
                Email = $"user-{id:N}@example.test",
                Phone = "+10000000000",
                Role = role
            };
        }

        public void Logout()
        {
            _currentUser = null;
        }
    }
}
