using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class UserProfileService : IUserProfileService
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        public UserProfileService(IAuthService authService, IUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        public Task<UserProfileDto?> GetCurrentProfileAsync(CancellationToken cancellationToken = default)
        {
            return _authService.GetCurrentUserAsync(cancellationToken);
        }

        public async Task<UserProfileDto?> UpdateCurrentProfileAsync(UpdateUserProfileRequest request, CancellationToken cancellationToken = default)
        {
            if (_authService.CurrentUserId is null)
                return null;

            var user = await _userRepository.GetByIdAsync(_authService.CurrentUserId.Value, cancellationToken);
            if (user is null)
                return null;

            user.FullName = request.FullName.Trim();
            user.Email = request.Email.Trim();
            user.Phone = request.Phone.Trim();

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return AuthService.MapToProfile(user);
        }
    }
}
