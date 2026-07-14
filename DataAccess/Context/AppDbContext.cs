using DTO.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUserEntity> AppUsers => Set<AppUserEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("app");

        modelBuilder.Entity<AppUserEntity>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Username);
        });
    }
}
