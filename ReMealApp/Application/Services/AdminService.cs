using Application.Interfaces;
using Domain.Enums;

namespace Application.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IAuthService _authService;

        public AdminService(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (currentUser.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
        }
    }
}
