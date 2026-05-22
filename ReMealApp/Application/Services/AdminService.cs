using Application.DTOs.Admin;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;

        public AdminService(
            IAuthService authService,
            IUserRepository userRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
        }

        public async Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (currentUser.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
        }

        public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var users = await _userRepository.GetAllAsync(cancellationToken);
            return users
                .Select(MapUser)
                .ToList();
        }

        private static AdminUserDto MapUser(User user)
        {
            return new AdminUserDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = null
            };
        }
    }
}
