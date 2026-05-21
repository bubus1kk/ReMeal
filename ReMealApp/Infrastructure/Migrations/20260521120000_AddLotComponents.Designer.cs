using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ReMealDbContext))]
    [Migration("20260521120000_AddLotComponents")]
    partial class AddLotComponents
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0");

            ReMealDbContextModelSnapshot.BuildTargetModel(modelBuilder);
        }
    }
}
