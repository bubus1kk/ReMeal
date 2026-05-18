using Application.DTOs.Auth;
using Application.DTOs.Users;
using Application.Interfaces;
using Domain.Entities;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IUserRepository userRepository, IPasswordHasher passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public Guid? CurrentUserId { get; private set; }

        public async Task<AuthResult> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default)
        {
            var validationError = ValidateRegistration(request);
            if (validationError is not null)
                return AuthResult.Failure(validationError);

            var normalizedLogin = Normalize(request.Login);
            if (await _userRepository.LoginExistsAsync(normalizedLogin, cancellationToken))
                return AuthResult.Failure("Пользователь с таким логином уже существует.");

            var user = new User
            {
                Login = normalizedLogin,
                PasswordHash = _passwordHasher.HashPassword(request.Password),
                FullName = request.FullName.Trim(),
                Email = request.Email.Trim(),
                Phone = request.Phone.Trim(),
                Role = request.Role
            };

            await _userRepository.AddAsync(user, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            CurrentUserId = user.Id;

            return AuthResult.Success(MapToProfile(user));
        }

        public async Task<AuthResult> LoginAsync(LoginUserRequest request, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
                return AuthResult.Failure("Введите логин и пароль.");

            var user = await _userRepository.GetByLoginAsync(Normalize(request.Login), cancellationToken);
            if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
                return AuthResult.Failure("Неверный логин или пароль.");

            CurrentUserId = user.Id;

            return AuthResult.Success(MapToProfile(user));
        }

        public async Task<UserProfileDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (CurrentUserId is null)
                return null;

            var user = await _userRepository.GetByIdAsync(CurrentUserId.Value, cancellationToken);
            return user is null ? null : MapToProfile(user);
        }

        public void Logout()
        {
            CurrentUserId = null;
        }

        internal static UserProfileDto MapToProfile(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role
            };
        }

        private static string? ValidateRegistration(RegisterUserRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Login))
                return "Логин обязателен.";

            if (string.IsNullOrWhiteSpace(request.Password))
                return "Пароль обязателен.";

            if (string.IsNullOrWhiteSpace(request.FullName))
                return "ФИО обязательно.";

            if (string.IsNullOrWhiteSpace(request.Email))
                return "Email обязателен.";

            if (string.IsNullOrWhiteSpace(request.Phone))
                return "Телефон обязателен.";

            return null;
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }
    }
}
