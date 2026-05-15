using Halen.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Halen.Infrastructure.Storage;

public class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private string BasePath => configuration["FileStorage:BasePath"] ?? "./uploads";

    public async Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct)
    {
        var directory = Path.Combine(BasePath, folder);
        Directory.CreateDirectory(directory);

        var safeName = Path.GetFileName(fileName);
        var uniqueName = $"{Guid.NewGuid()}_{safeName}";
        var filePath = Path.Combine(directory, uniqueName);
        ValidateWithinBase(filePath);

        await using var fileStream = new FileStream(filePath, FileMode.Create);
        await content.CopyToAsync(fileStream, ct);

        return filePath;
    }

    public Task<Stream> ReadAsync(string filePath, CancellationToken ct)
    {
        ValidateWithinBase(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("File not found on disk.", filePath);

        Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string filePath, CancellationToken ct)
    {
        ValidateWithinBase(filePath);

        if (File.Exists(filePath))
            File.Delete(filePath);

        return Task.CompletedTask;
    }

    private void ValidateWithinBase(string filePath)
    {
        var fullPath = Path.GetFullPath(filePath);
        var fullBase = Path.GetFullPath(BasePath);
        if (!fullPath.StartsWith(fullBase, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("File path is outside the allowed storage directory.");
    }
}
