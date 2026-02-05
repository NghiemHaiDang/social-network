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
    public DbSet<ZaloUser> ZaloUsers => Set<ZaloUser>();
    public DbSet<ZaloConversation> ZaloConversations => Set<ZaloConversation>();
    public DbSet<ZaloMessage> ZaloMessages => Set<ZaloMessage>();

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

        modelBuilder.Entity<ZaloUser>(entity =>
        {
            entity.ToTable("ZaloUsers");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ZaloUserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.OAId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.DisplayName)
                .HasMaxLength(500);

            entity.Property(x => x.AvatarUrl)
                .HasMaxLength(2000);

            entity.HasIndex(x => new { x.ZaloUserId, x.OAId }).IsUnique();
            entity.HasIndex(x => x.OAId);
        });

        modelBuilder.Entity<ZaloConversation>(entity =>
        {
            entity.ToTable("ZaloConversations");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.LastMessagePreview)
                .HasMaxLength(200);

            entity.HasOne(x => x.OAAccount)
                .WithMany()
                .HasForeignKey(x => x.OAAccountId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.ZaloUser)
                .WithMany()
                .HasForeignKey(x => x.ZaloUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.OAAccountId, x.ZaloUserId }).IsUnique();
            entity.HasIndex(x => x.OAAccountId);
            entity.HasIndex(x => x.LastMessageAt);
        });

        modelBuilder.Entity<ZaloMessage>(entity =>
        {
            entity.ToTable("ZaloMessages");
            entity.HasKey(x => x.Id);

            entity.Property(x => x.ZaloMessageId)
                .HasMaxLength(100);

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.AttachmentUrl)
                .HasMaxLength(2000);

            entity.Property(x => x.AttachmentName)
                .HasMaxLength(500);

            entity.Property(x => x.ThumbnailUrl)
                .HasMaxLength(2000);

            entity.Property(x => x.ErrorMessage)
                .HasMaxLength(1000);

            entity.HasOne(x => x.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.ConversationId);
            entity.HasIndex(x => x.ZaloMessageId);
            entity.HasIndex(x => x.SentAt);
        });
    }
}
