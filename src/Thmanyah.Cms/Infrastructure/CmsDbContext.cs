using Microsoft.EntityFrameworkCore;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Cms.Infrastructure
{
    /// <summary>
    /// CMS module DbContext. Owns Programs and Episodes.
    /// Separate from MediaIngestion and Discovery DbContexts.
    /// </summary>
    public class CmsDbContext : DbContext
    {
        public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
        {
        }

        public DbSet<Program> Programs => Set<Program>();
        public DbSet<Episode> Episodes => Set<Episode>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Programs
            modelBuilder.Entity<Program>().HasKey(p => p.Id);
            modelBuilder.Entity<Program>().Property(p => p.RowVersion).IsRowVersion();
            modelBuilder.Entity<Program>().HasIndex(p => p.Title);

            // Configure Episodes
            modelBuilder.Entity<Episode>().HasKey(e => e.Id);
            modelBuilder.Entity<Episode>().Property(e => e.RowVersion).IsRowVersion();
            modelBuilder.Entity<Episode>()
                .HasOne<Program>()
                .WithMany()
                .HasForeignKey(e => e.ProgramId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Episode>().HasIndex(e => new { e.ProgramId, e.Title });
        }
    }
}
