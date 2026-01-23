using Microsoft.EntityFrameworkCore;
using ZaloOA.Domain.Entities;

namespace ZaloOA.Infrastructure.Persistence;

public class ZaloOADbContext : DbContext
{
    public ZaloOADbContext(DbContextOptions<ZaloOADbContext> options) : base(options)
    {
    }

    public DbSet<ZaloOAAccount> ZaloOAAccounts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ZaloOAAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.OAId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(256);
            entity.Property(e => e.AvatarUrl).HasMaxLength(512);
            entity.Property(e => e.AccessToken).IsRequired();
            entity.Property(e => e.RefreshToken);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.OAId }).IsUnique();
        });
    }
}
