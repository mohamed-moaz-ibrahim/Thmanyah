using Microsoft.EntityFrameworkCore;
using Thmanyah.Shared.Domain;

namespace Thmanyah.Discovery.Infrastructure
{
    /// <summary>
    /// Discovery module DbContext. Owns read models (denormalized from CMS events).
    /// Separate write&read paths: CMS publishes events → Discovery updates read models.
    /// </summary>
    public class DiscoveryDbContext : DbContext
    {
        public DiscoveryDbContext(DbContextOptions<DiscoveryDbContext> options) : base(options)
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
