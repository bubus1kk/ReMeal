using Domain.Entities;
using Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly ReMealDbContext _dbContext;

        public UserRepository(ReMealDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken),
                "получить пользователя по id");
        }

        public Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.Users.FirstOrDefaultAsync(user => user.Login == login, cancellationToken),
                "получить пользователя по логину");
        }

        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.Users.AnyAsync(user => user.Login == login, cancellationToken),
                "проверить логин пользователя");
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                async () => await _dbContext.Users.AddAsync(user, cancellationToken),
                "добавить пользователя");
        }

        public void Update(User user)
        {
            DataAccessGuard.Execute(
                () => _dbContext.Users.Update(user),
                "обновить пользователя");
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return DataAccessGuard.ExecuteAsync(
                () => _dbContext.SaveChangesAsync(cancellationToken),
                "сохранить изменения пользователя");
        }
    }
}
