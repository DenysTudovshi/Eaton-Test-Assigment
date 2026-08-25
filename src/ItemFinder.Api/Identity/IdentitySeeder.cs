using Microsoft.AspNetCore.Identity;

namespace ItemFinder.Api.Identity;

/// <summary>
/// Creates the Admin role and one admin account from configuration at startup.
/// Registration is open to everyone, so administration rights come only from this
/// seeded account — never from self-service signup.
/// </summary>
internal static partial class IdentitySeeder
{
    public const string AdminRole = "Admin";
    public const string AdminEmailKey = "ITEMFINDER_ADMIN_EMAIL";
    public const string AdminPasswordKey = "ITEMFINDER_ADMIN_PASSWORD";

    public static async Task SeedAsync(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(IdentitySeeder));
        var configuration = services.GetRequiredService<IConfiguration>();
        var email = configuration[AdminEmailKey];
        var password = configuration[AdminPasswordKey];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            LogNoAdminConfig(logger, AdminEmailKey, AdminPasswordKey);
            return;
        }

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            await roleManager.CreateAsync(new IdentityRole(AdminRole));
        }

        var userManager = services.GetRequiredService<UserManager<ApiUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApiUser { UserName = email, Email = email, EmailConfirmed = true };
            var created = await userManager.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                LogAdminCreateFailed(logger, string.Join("; ", created.Errors.Select(error => error.Description)));
                return;
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Admin not seeded: set {EmailKey} and {PasswordKey} (user secrets or environment) to enable data-file administration.")]
    private static partial void LogNoAdminConfig(ILogger logger, string emailKey, string passwordKey);

    [LoggerMessage(Level = LogLevel.Error, Message = "Admin account could not be created: {Errors}")]
    private static partial void LogAdminCreateFailed(ILogger logger, string errors);
}