namespace Halen.Application.Interfaces;

public interface IFileStorage
{
    Task<string> SaveAsync(string folder, string fileName, Stream content, CancellationToken ct);
    Task<Stream> ReadAsync(string filePath, CancellationToken ct);
    Task DeleteAsync(string filePath, CancellationToken ct);
}
