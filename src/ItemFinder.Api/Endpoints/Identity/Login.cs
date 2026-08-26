using ItemFinder.Api.Identity;

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace ItemFinder.Api.Endpoints.Identity;

/// <summary>
/// Exchange credentials for a bearer token. Signs into the bearer scheme, so the
/// framework's token response shape and SignInManager's lockout handling are preserved.
/// </summary>
public sealed class Login : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/login", Handle)
            .WithName("Login")
            .WithSummary("Exchange credentials for a bearer token.")
            .Produces<AccessTokenResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

    private static async Task<IResult> Handle(
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