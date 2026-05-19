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
            return _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
        }

        public Task<User?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
        {
            return _dbContext.Users.FirstOrDefaultAsync(user => user.Login == login, cancellationToken);
        }

        public Task<bool> LoginExistsAsync(string login, CancellationToken cancellationToken = default)
        {
            return _dbContext.Users.AnyAsync(user => user.Login == login, cancellationToken);
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
        }

        public void Update(User user)
        {
            _dbContext.Users.Update(user);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
