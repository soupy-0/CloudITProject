using Microsoft.EntityFrameworkCore;
using MeetSync.Models;

namespace MeetSync.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserInterest> UserInterests { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(256);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Workplace).HasMaxLength(200);
                entity.Property(e => e.AboutSection).HasMaxLength(1000);
            });

            // Configure UserInterest entity
            modelBuilder.Entity<UserInterest>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Interest).IsRequired().HasMaxLength(100);
                entity.HasIndex(e => e.UserId);
            });
        }
    }
}