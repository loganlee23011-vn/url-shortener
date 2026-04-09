using Microsoft.EntityFrameworkCore;
using Shorten.Data.Models;

namespace Shorten.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<ShortenedUrl> ShortenedUrls { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ShortenedUrl>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ShortCode).IsUnique();
                entity.Property(e => e.OriginalUrl).IsRequired();
                entity.Property(e => e.ShortCode).IsRequired().HasMaxLength(10);
            });
        }
    }
}