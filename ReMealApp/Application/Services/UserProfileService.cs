using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class UserProfileService : IUserProfileService
    {
        private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".webp",
            ".bmp"
        };

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

        public async Task<UserProfileDto?> UpdateCurrentAvatarAsync(UpdateUserAvatarRequest request, CancellationToken cancellationToken = default)
        {
            if (_authService.CurrentUserId is null)
                return null;

            var user = await _userRepository.GetByIdAsync(_authService.CurrentUserId.Value, cancellationToken);
            if (user is null)
                return null;

            if (string.IsNullOrWhiteSpace(request.SourceFilePath))
                throw new ArgumentException("Выберите изображение профиля.", nameof(request));

            if (!File.Exists(request.SourceFilePath))
                throw new FileNotFoundException("Файл изображения не найден.", request.SourceFilePath);

            var extension = Path.GetExtension(request.SourceFilePath);
            if (!AllowedAvatarExtensions.Contains(extension))
                throw new ArgumentException("Поддерживаются изображения PNG, JPG, JPEG, WEBP и BMP.", nameof(request));

            var avatarDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ReMeal",
                "Avatars");

            Directory.CreateDirectory(avatarDirectory);

            var avatarPath = Path.Combine(avatarDirectory, $"{user.Id:N}{extension.ToLowerInvariant()}");
            if (!string.Equals(
                    Path.GetFullPath(request.SourceFilePath),
                    Path.GetFullPath(avatarPath),
                    StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(request.SourceFilePath, avatarPath, overwrite: true);
            }

            user.AvatarPath = avatarPath;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);

            return AuthService.MapToProfile(user);
        }
    }
}
