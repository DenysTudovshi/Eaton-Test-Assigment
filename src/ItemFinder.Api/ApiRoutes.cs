namespace ItemFinder.Api;

/// <summary>Route constants; the version prefix is fixed per URL, not negotiated.</summary>
internal static class ApiRoutes
{
    private const string Prefix = "/api/v1";

    public const string Items = $"{Prefix}/items";

    public const string Identity = $"{Prefix}/identity";

    public const string DataFile = $"{Prefix}/data-file";
}