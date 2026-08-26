using ItemFinder.Api.Endpoints.DataFile;
using ItemFinder.Api.Endpoints.Identity;
using ItemFinder.Api.Endpoints.Items;
using ItemFinder.Api.Identity;

namespace ItemFinder.Api.Endpoints;

/// <summary>
/// Every route the API serves. Group-level concerns — tags, the Admin requirement on
/// the data file, rate limiting on identity — are declared once here; each endpoint
/// class owns only its own route and handler.
/// </summary>
public static class EndpointRegistration
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var identity = app.MapGroup(ApiRoutes.Identity)
            .WithTags("Identity")
            .RequireRateLimiting(RateLimitPolicies.Identity);
        Map<Register>(identity);
        Map<Login>(identity);

        var items = app.MapGroup(ApiRoutes.Items)
            .WithTags("Items");
        Map<GetItems>(items);
        Map<GetItemByName>(items);

        var dataFile = app.MapGroup(ApiRoutes.DataFile)
            .WithTags("Data file")
            .RequireAuthorization(policy => policy.RequireRole(IdentitySeeder.AdminRole));
        Map<DownloadDataFile>(dataFile);
        Map<UploadDataFile>(dataFile);
        Map<RemoveDataFile>(dataFile);

        return app;
    }

    private static void Map<TEndpoint>(IEndpointRouteBuilder builder)
        where TEndpoint : IEndpoint =>
        TEndpoint.Map(builder);
}