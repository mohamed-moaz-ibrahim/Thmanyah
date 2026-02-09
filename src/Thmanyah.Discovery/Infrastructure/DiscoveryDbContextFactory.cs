using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thmanyah.Discovery.Infrastructure
{
    public class DiscoveryDbContextFactory : IDesignTimeDbContextFactory<DiscoveryDbContext>
    {
        public DiscoveryDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=Thmanyah;Username=postgres;Password=dont connect";
            var builder = new DbContextOptionsBuilder<DiscoveryDbContext>();
            builder.UseNpgsql(conn);
            return new DiscoveryDbContext(builder.Options);
        }
    }
}
