using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;

namespace ItemFinder.Application.Services;

/// <summary>Use case: parse the data file and build the item directory in one step.</summary>
public sealed class ItemDirectoryLoader(IDataFileParser parser)
{
    public DirectoryLoadResult Load(string dataFilePath)
    {
        var result = parser.ParseFile(dataFilePath);
        return result.Success
            ? DirectoryLoadResult.Ok(new ItemDirectory(result.Forest))
            : DirectoryLoadResult.Failed(result.Errors);
    }
}