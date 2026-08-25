using MediatR;

namespace ItemFinder.Application.Commands.DeleteDataFile;

/// <summary>Removes the managed data file; succeeds whether or not one is stored.</summary>
public sealed record DeleteDataFileCommand : IRequest;