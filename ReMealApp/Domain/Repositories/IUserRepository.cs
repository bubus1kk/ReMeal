using Domain.Entities;

namespace Domain.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);

        Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default);

        Task AddAsync(User user, CancellationToken cancellationToken = default);

        void Update(User user);

        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
