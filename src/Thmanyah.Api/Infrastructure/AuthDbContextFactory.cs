using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Thmanyah.Api.Infrastructure
{
    public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
    {
        public AuthDbContext CreateDbContext(string[] args)
        {
            var conn = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=Thmanyah;Username=postgres;Password=dont connect";
            var builder = new DbContextOptionsBuilder<AuthDbContext>();
            builder.UseNpgsql(conn);
            return new AuthDbContext(builder.Options);
        }
    }
}
