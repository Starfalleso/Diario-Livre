namespace DiarioLivre.Core.Models;

/// <summary>
/// Metadata for a locally stored rolling auto-backup archive.
/// </summary>
public sealed record AutoBackupEntry(
    string FileName,
    string FullPath,
    DateTime CreatedAtLocal,
    long SizeBytes);
