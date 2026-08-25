using FluentValidation;

using ItemFinder.Application.Options;

using Microsoft.Extensions.Options;

namespace ItemFinder.Application.Commands.ReplaceDataFile;

/// <summary>Cheap upload checks that run before the content is ever parsed.</summary>
public sealed class ReplaceDataFileCommandValidator : AbstractValidator<ReplaceDataFileCommand>
{
    public ReplaceDataFileCommandValidator(IOptions<DataFileOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var maxSizeBytes = options.Value.MaxSizeBytes;

        RuleFor(command => command.FileName)
            .Must(fileName => Path.GetExtension(fileName).Equals(".txt", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .txt data files are accepted.");

        RuleFor(command => command.FileSize)
            .GreaterThan(0)
            .LessThanOrEqualTo(maxSizeBytes)
            .WithMessage($"The file exceeds the {maxSizeBytes / 1024} KB size limit.");
    }
}