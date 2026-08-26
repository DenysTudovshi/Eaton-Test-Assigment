namespace ItemFinder.Api.Endpoints;

/// <summary>One HTTP endpoint: its route, metadata, and handler together in one class.</summary>
public interface IEndpoint
{
    /// <summary>Maps this endpoint onto <paramref name="app"/> — typically its resource group.</summary>
    static abstract void Map(IEndpointRouteBuilder app);
}