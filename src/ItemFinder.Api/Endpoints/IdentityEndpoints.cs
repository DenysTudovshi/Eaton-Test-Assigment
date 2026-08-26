using System.ComponentModel.DataAnnotations;

using ItemFinder.Api.Identity;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace ItemFinder.Api.Endpoints;

/// <summary>
/// The whole identity surface: register and log in, nothing else. Login signs into the
/// bearer scheme, so the framework's token response shape (and lockout handling via
/// SignInManager) is preserved without mapping the rest of the Identity API.
/// </summary>
public static class IdentityEndpoints
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app, string rateLimitPolicy)
    {
        var group = app.MapGroup(ApiRoutes.Identity)
            .WithTags("Identity")
            .RequireRateLimiting(rateLimitPolicy);

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Create an account; new accounts hold no roles.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Exchange credentials for a bearer token.")
            .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    private static async Task<IResult> Register(RegisterRequest request, UserManager<ApiUser> userManager)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Email) || !EmailFormat.IsValid(request.Email))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["Email"] = ["A valid email address is required."],
            });
        }

        var user = new ApiUser { UserName = request.Email, Email = request.Email };
        var created = await userManager.CreateAsync(user, request.Password);
        if (created.Succeeded)
        {
            return TypedResults.Ok();
        }

        return TypedResults.ValidationProblem(created.Errors
            .GroupBy(error => error.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray(),
                StringComparer.Ordinal));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApiUser> userManager,
        SignInManager<ApiUser> signInManager)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            var check = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (check.Succeeded)
            {
                var principal = await signInManager.CreateUserPrincipalAsync(user);
                return TypedResults.SignIn(principal, authenticationScheme: IdentityConstants.BearerScheme);
            }
        }

        // One answer for wrong password, unknown account, and lockout: nothing to enumerate.
        return TypedResults.Problem(
            title: "Invalid email or password.",
            statusCode: StatusCodes.Status401Unauthorized);
    }
}