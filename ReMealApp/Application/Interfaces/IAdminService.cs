namespace Application.Interfaces
{
    public interface IAdminService
    {
        Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default);
    }
}
