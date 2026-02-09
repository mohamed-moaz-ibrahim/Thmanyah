using Microsoft.AspNetCore.Identity;
using Thmanyah.Cms.Infrastructure;
using Thmanyah.Discovery.Infrastructure;
using Thmanyah.Api.Infrastructure;
using System.Threading.RateLimiting;
using System.Security.Claims;
using Microsoft.OpenApi.Models;
using Thmanyah.Shared.Configurations;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Thmanyah API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token like: Bearer {your token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        { new OpenApiSecurityScheme {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});


var redisConn = builder.Configuration["RedisConfiguration:Connection"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}
builder.Services.Configure<StorageConfiguration>(builder.Configuration.GetSection("StorageConfiguration"));
builder.Services.Configure<DiscoveryCacheConfiguration>(builder.Configuration.GetSection("DiscoveryCacheConfiguration"));

builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddCmsModule(builder.Configuration);
builder.Services.AddDiscoveryModule(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var clientKey = httpContext.User?.Identity?.IsAuthenticated == true
            ? httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon"
            : httpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";

        return RateLimitPartition.GetTokenBucketLimiter(clientKey, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 200,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 50,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            TokensPerPeriod = 50
        });
    });
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "1";
        await System.Threading.Tasks.Task.CompletedTask;
    };
});

var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();

//app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseRateLimiter();

// seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var authDb = services.GetRequiredService<AuthDbContext>();
        var cmsDb = services.GetRequiredService<CmsDbContext>();
        var discoveryDb = services.GetRequiredService<DiscoveryDbContext>();
        authDb.Database.Migrate();
        cmsDb.Database.Migrate();

        var roleMgr = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userMgr = services.GetRequiredService<UserManager<IdentityUser>>();

        string[] roles = new[] { "sysAdmin", "user", "contentManager" };
        foreach (var r in roles)
        {
            if (!roleMgr.RoleExistsAsync(r).GetAwaiter().GetResult())
            {
                roleMgr.CreateAsync(new IdentityRole(r)).GetAwaiter().GetResult();
            }
        }

        var adminEmail = builder.Configuration["Auth:SeedAdmin:Email"] ?? "admin@thmanyah.com";
        var adminPwd = builder.Configuration["Auth:SeedAdmin:Password"] ?? "Admin123!";

        var admin = userMgr.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (admin == null)
        {
            admin = new IdentityUser { UserName = adminEmail, Email = adminEmail };
            var res = userMgr.CreateAsync(admin, adminPwd).GetAwaiter().GetResult();
            if (res.Succeeded)
            {
                userMgr.AddToRoleAsync(admin, "sysAdmin").GetAwaiter().GetResult();
            }
        }
    }
    catch
    {
        // ignore errors
    }
}

app.Run();