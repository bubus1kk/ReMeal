using Application.DTOs.Admin;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default);

        Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    }
}
