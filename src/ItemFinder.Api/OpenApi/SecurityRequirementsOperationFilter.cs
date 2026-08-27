using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ItemFinder.Api.OpenApi;

/// <summary>
/// Marks an operation as secured in the OpenAPI document only when its endpoint
/// actually requires authorization, so Swagger UI's padlock appears exactly on the
/// protected endpoints instead of on everything. Secured operations also document
/// their 401/403 outcomes.
/// </summary>
public sealed class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requiresAuthorization =
            metadata.Any(item => item is IAuthorizeData or AuthorizationPolicy)
            && !metadata.Any(item => item is IAllowAnonymous);

        if (!requiresAuthorization)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    Array.Empty<string>()
                },
            },
        ];

        operation.Responses.TryAdd(
            "401", new OpenApiResponse { Description = "Not authenticated" });
        operation.Responses.TryAdd(
            "403", new OpenApiResponse { Description = "Authenticated without the required role" });
    }
}