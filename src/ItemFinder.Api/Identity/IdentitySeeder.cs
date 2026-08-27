using Microsoft.AspNetCore.Identity;

namespace ItemFinder.Api.Identity;

/// <summary>
/// Makes the identity state match the configuration on every start: the configured
/// account exists with exactly the configured password (rotated when it changed), and
/// it is the only holder of the Admin role — a previously configured admin is demoted
/// to a plain user. Registration is open to everyone, so administration rights come
/// only from this configuration — never from self-service signup.
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
                LogAdminCreateFailed(logger, Describe(created));
                return;
            }
        }
        else if (!await userManager.CheckPasswordAsync(admin, password))
        {
            // The configured credentials are authoritative: rotate the stored password
            // to match them. The reset token path validates the new password before
            // anything changes, so a rejected value leaves the old password intact.
            var resetToken = await userManager.GeneratePasswordResetTokenAsync(admin);
            var reset = await userManager.ResetPasswordAsync(admin, resetToken, password);
            if (!reset.Succeeded)
            {
                LogAdminPasswordRotationFailed(logger, Describe(reset));
            }
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            await userManager.AddToRoleAsync(admin, AdminRole);
        }

        foreach (var other in await userManager.GetUsersInRoleAsync(AdminRole))
        {
            if (other.Id != admin.Id)
            {
                await userManager.RemoveFromRoleAsync(other, AdminRole);
                LogDemotedPreviousAdmin(logger, other.Email ?? other.Id);
            }
        }
    }

    private static string Describe(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => error.Description));

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Admin not seeded: set {EmailKey} and {PasswordKey} (user secrets or environment) to enable data-file administration.")]
    private static partial void LogNoAdminConfig(ILogger logger, string emailKey, string passwordKey);

    [LoggerMessage(Level = LogLevel.Error, Message = "Admin account could not be created: {Errors}")]
    private static partial void LogAdminCreateFailed(ILogger logger, string errors);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Admin password could not be rotated to the configured value: {Errors}")]
    private static partial void LogAdminPasswordRotationFailed(ILogger logger, string errors);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Demoted previous admin {Email}: only the configured account holds the Admin role.")]
    private static partial void LogDemotedPreviousAdmin(ILogger logger, string email);
}