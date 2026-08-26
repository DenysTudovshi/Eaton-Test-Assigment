using System.ComponentModel.DataAnnotations;

using ItemFinder.Api.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;

namespace ItemFinder.Api.Endpoints.Identity;

/// <summary>Create an account. New accounts hold no roles — administration stays with the seeded admin.</summary>
public sealed class Register : IEndpoint
{
    private static readonly EmailAddressAttribute EmailFormat = new();

    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/register", Handle)
            .WithName("Register")
            .WithSummary("Create an account; new accounts hold no roles.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem();

    private static async Task<IResult> Handle(RegisterRequest request, UserManager<ApiUser> userManager)
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
}