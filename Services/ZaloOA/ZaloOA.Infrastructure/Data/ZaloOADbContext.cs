using Microsoft.EntityFrameworkCore;
using ZaloOA.Domain.Entities;

namespace ZaloOA.Infrastructure.Data;

public class ZaloOADbContext : DbContext
{
    public ZaloOADbContext(DbContextOptions<ZaloOADbContext> options)
        : base(options)
    {
    }

    public DbSet<ZaloOAAccount> ZaloOAAccounts => Set<ZaloOAAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ZaloOAAccount>(entity =>
        {
            entity.ToTable("ZaloOAAccounts");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(x => x.OAId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(x => x.AvatarUrl)
                .HasMaxLength(2000);

            entity.Property(x => x.AccessToken)
                .IsRequired();

            entity.Property(x => x.RefreshToken);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.OAId }).IsUnique();
        });
    }
}
