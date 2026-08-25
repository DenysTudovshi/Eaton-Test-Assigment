using ItemFinder.Application.Results;

using MediatR;

namespace ItemFinder.Application.Commands.ReplaceDataFile;

/// <summary>Replaces the managed data file with uploaded content after the parse gate approves it.</summary>
public sealed record ReplaceDataFileCommand(string Content, string FileName, long FileSize)
    : IRequest<DataFileReplaceResult>;