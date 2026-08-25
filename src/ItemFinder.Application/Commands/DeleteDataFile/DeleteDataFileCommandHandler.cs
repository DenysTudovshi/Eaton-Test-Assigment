using ItemFinder.Application.Interfaces;

using MediatR;

namespace ItemFinder.Application.Commands.DeleteDataFile;

/// <summary>Idempotent delete over the managed store.</summary>
public sealed class DeleteDataFileCommandHandler(IManagedDataFileStore store)
    : IRequestHandler<DeleteDataFileCommand>
{
    public Task Handle(DeleteDataFileCommand request, CancellationToken cancellationToken)
    {
        store.Delete();
        return Task.CompletedTask;
    }
}