using ItemFinder.Application.Interfaces;

using MediatR;

namespace ItemFinder.Application.Queries.GetDataFile;

/// <summary>Serves the raw stored file for download.</summary>
public sealed class GetDataFileQueryHandler(IManagedDataFileStore store)
    : IRequestHandler<GetDataFileQuery, string?>
{
    public Task<string?> Handle(GetDataFileQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(store.ReadContent());
}