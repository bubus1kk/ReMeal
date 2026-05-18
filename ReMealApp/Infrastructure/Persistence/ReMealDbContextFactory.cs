using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence
{
    public sealed class ReMealDbContextFactory : IDesignTimeDbContextFactory<ReMealDbContext>
    {
        public ReMealDbContext CreateDbContext(string[] args)
        {
            var options = new DbContextOptionsBuilder<ReMealDbContext>()
                .UseSqlite($"Data Source={ReMealDatabasePath.GetDefaultPath()}")
                .Options;

            return new ReMealDbContext(options);
        }
    }
}
