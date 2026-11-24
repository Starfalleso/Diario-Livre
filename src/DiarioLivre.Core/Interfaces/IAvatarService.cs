namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Service for processing, resizing, and resolving local student avatar images.
/// </summary>
public interface IAvatarService
{
    Task<string> SaveAvatarAsync(string sourceFilePath, CancellationToken ct = default);

    Task<string> SaveAvatarAsync(Stream imageStream, CancellationToken ct = default);

    string? GetAvatarFullPath(string? photoFileName);

    void DeleteAvatar(string? photoFileName);
}
