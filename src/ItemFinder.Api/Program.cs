using System.Threading.RateLimiting;

using FluentValidation;

using ItemFinder.Api;
using ItemFinder.Api.Endpoints;
using ItemFinder.Api.ExceptionHandling;
using ItemFinder.Api.Identity;
using ItemFinder.Application.Behaviors;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Options;
using ItemFinder.Application.Services;
using ItemFinder.Infrastructure.Parsing;
using ItemFinder.Infrastructure.Storage;

using MediatR;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Item Finder API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        In = ParameterLocation.Header,
        Description = "Paste the accessToken returned by the identity login endpoint.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Null members stay out of responses, so projected items carry only the fields asked for.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull);

builder.Services.AddMediatR(configuration =>
{
    configuration.RegisterServicesFromAssemblyContaining<ItemDirectoryLoader>();
    configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssemblyContaining<ItemDirectoryLoader>();

builder.Services.Configure<DataFileOptions>(builder.Configuration.GetSection(DataFileOptions.SectionName));
builder.Services.PostConfigure<DataFileOptions>(options =>
{
    // Relative paths resolve against the binaries so the bundled seed works in publish output.
    options.StoragePath = Path.GetFullPath(options.StoragePath, AppContext.BaseDirectory);
    if (options.SeedPath is { Length: > 0 } seedPath)
    {
        options.SeedPath = Path.GetFullPath(seedPath, AppContext.BaseDirectory);
    }
});
builder.Services.AddSingleton<IDataFileParser, DataFileParser>();
builder.Services.AddSingleton<IManagedDataFileStore>(provider => new ManagedDataFileStore(
    provider.GetRequiredService<IOptions<DataFileOptions>>().Value,
    provider.GetRequiredService<IDataFileParser>()));

builder.Services.AddDbContext<ApiDbContext>((provider, options) =>
{
    // Resolved lazily: test hosts override Identity:DbPath after this registration runs.
    var configuration = provider.GetRequiredService<IConfiguration>();
    var identityDbPath = Path.GetFullPath(
        configuration["Identity:DbPath"] ?? "App_Data/identity.db", AppContext.BaseDirectory);
    Directory.CreateDirectory(Path.GetDirectoryName(identityDbPath)!);
    options.UseSqlite($"Data Source={identityDbPath}");
});

// Bearer tokens are encrypted with these keys; keeping them under App_Data (the
// Docker volume) lets tokens survive container recreation instead of dying with it.
var dataProtectionKeysPath = Path.GetFullPath(
    builder.Configuration["DataProtection:KeysPath"] ?? "App_Data/keys", AppContext.BaseDirectory);
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

builder.Services.AddAuthorization();
builder.Services.AddIdentityApiEndpoints<ApiUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApiDbContext>();

// Brakes credential guessing alongside Identity's lockout; keyed per caller address.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Identity, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var identityDb = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
    await identityDb.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(scope.ServiceProvider);
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapEndpoints();

await app.RunAsync();

/// <summary>Entry-point marker so integration tests can host the app via WebApplicationFactory.</summary>
public partial class Program
{
}