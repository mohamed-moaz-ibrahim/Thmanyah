using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thmanyah.Cms.Infrastructure
{
    public class CmsDbContextFactory : IDesignTimeDbContextFactory<CmsDbContext>
    {
        public CmsDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=Thmanyah;Username=postgres;Password=dont connect";
            var builder = new DbContextOptionsBuilder<CmsDbContext>();
            builder.UseNpgsql(conn);
            return new CmsDbContext(builder.Options);
        }
    }
}
