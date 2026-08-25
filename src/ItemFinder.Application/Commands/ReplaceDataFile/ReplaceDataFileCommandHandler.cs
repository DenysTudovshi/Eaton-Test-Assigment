using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;

using MediatR;

namespace ItemFinder.Application.Commands.ReplaceDataFile;

/// <summary>Delegates to the store, whose replace is parse-gated and atomic.</summary>
public sealed class ReplaceDataFileCommandHandler(IManagedDataFileStore store)
    : IRequestHandler<ReplaceDataFileCommand, DataFileReplaceResult>
{
    public Task<DataFileReplaceResult> Handle(ReplaceDataFileCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Task.FromResult(store.Replace(request.Content));
    }
}