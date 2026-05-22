using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ReMealDbContext))]
    [Migration("20260522120000_AddBookings")]
    partial class AddBookings
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            ReMealDbContextModelSnapshot.BuildTargetModel(modelBuilder);
        }
    }
}
