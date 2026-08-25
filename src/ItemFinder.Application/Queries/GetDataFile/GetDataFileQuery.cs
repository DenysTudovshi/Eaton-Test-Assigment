using MediatR;

namespace ItemFinder.Application.Queries.GetDataFile;

/// <summary>Reads the managed data file's content; null when none is stored.</summary>
public sealed record GetDataFileQuery : IRequest<string?>;