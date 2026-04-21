using FACEBOOK_INTEGRATION.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FACEBOOK_INTEGRATION.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ClientEntity> Clients { get; set; }
    public DbSet<FacebookConnectionEntity> FacebookConnections { get; set; }
    public DbSet<FacebookPageEntity> FacebookPages { get; set; }
    public DbSet<FacebookPostEntity> FacebookPosts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClientEntity>(entity =>
        {
            entity.ToTable("Clients");
            entity.HasKey(e => e.ClientId);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<FacebookConnectionEntity>(entity =>
        {
            entity.ToTable("FacebookConnections");
            entity.HasKey(e => e.ClientId);
            entity.Property(e => e.UserAccessToken).IsRequired();
        });

        modelBuilder.Entity<FacebookPageEntity>(entity =>
        {
            entity.ToTable("FacebookPages");
            entity.HasKey(e => new { e.ClientId, e.PageId });
            entity.Property(e => e.PageId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PageName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PageAccessToken).IsRequired();
        });

        modelBuilder.Entity<FacebookPostEntity>(entity =>
        {
            entity.ToTable("FacebookPosts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.PageId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PostType).HasMaxLength(20).IsRequired();
            entity.Property(e => e.FacebookPostId).HasMaxLength(100);
        });

        base.OnModelCreating(modelBuilder);
    }
}
