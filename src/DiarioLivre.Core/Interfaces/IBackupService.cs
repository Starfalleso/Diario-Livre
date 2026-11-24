using DiarioLivre.Core.Models;

namespace DiarioLivre.Core.Interfaces;

/// <summary>
/// Service for inspecting local offline storage and exporting/restoring full .zip backups.
/// </summary>
public interface IBackupService
{
    string DataDirectory { get; }

    string DatabasePath { get; }

    Task<BackupInfo> GetStorageInfoAsync(CancellationToken ct = default);

    Task ExportBackupZipAsync(string destinationZipPath, CancellationToken ct = default);

    Task RestoreBackupZipAsync(string sourceZipPath, CancellationToken ct = default);

    Task<AutoBackupEntry?> CreateRollingAutoBackupAsync(int maxSnapshots = 5, CancellationToken ct = default);

    Task<IReadOnlyList<AutoBackupEntry>> GetAutoBackupsAsync(CancellationToken ct = default);
}
