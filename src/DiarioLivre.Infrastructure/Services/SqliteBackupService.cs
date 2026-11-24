using System.IO.Compression;
using DiarioLivre.Core.Interfaces;
using DiarioLivre.Core.Models;
using DiarioLivre.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DiarioLivre.Infrastructure.Services;

/// <summary>
/// Manages local SQLite + avatars zip backups, restoration, and storage statistics.
/// </summary>
internal sealed class SqliteBackupService : IBackupService
{
    private readonly IDbContextFactory<DiarioLivreDbContext> _contextFactory;
    private readonly string _databasePath;
    private readonly string _avatarsDirectory;

    public SqliteBackupService(
        IDbContextFactory<DiarioLivreDbContext> contextFactory,
        string databasePath,
        string avatarsDirectory)
    {
        _contextFactory = contextFactory;
        _databasePath = databasePath;
        _avatarsDirectory = avatarsDirectory;
        DataDirectory = Path.GetDirectoryName(databasePath) ?? avatarsDirectory;
    }

    public string DataDirectory { get; }

    public string DatabasePath => _databasePath;

    public async Task<BackupInfo> GetStorageInfoAsync(CancellationToken ct = default)
    {
        await using DiarioLivreDbContext context = await _contextFactory.CreateDbContextAsync(ct);

        int yearsCount = await context.AcademicYears.CountAsync(ct);
        int classroomsCount = await context.Classrooms.CountAsync(ct);
        int studentsCount = await context.Students.CountAsync(ct);
        int gradesCount = await context.Grades.CountAsync(ct);
        int sessionsCount = await context.AttendanceSessions.CountAsync(ct);
        int observationsCount = await context.StudentObservations.CountAsync(ct);

        long dbBytes = File.Exists(_databasePath) ? new FileInfo(_databasePath).Length : 0;
        string walPath = _databasePath + "-wal";
        if (File.Exists(walPath))
        {
            dbBytes += new FileInfo(walPath).Length;
        }

        int avatarFiles = Directory.Exists(_avatarsDirectory)
            ? Directory.GetFiles(_avatarsDirectory).Length
            : 0;

        return new BackupInfo(
            DatabasePath: _databasePath,
            DatabaseSizeBytes: dbBytes,
            AvatarFilesCount: avatarFiles,
            AcademicYearsCount: yearsCount,
            ClassroomsCount: classroomsCount,
            StudentsCount: studentsCount,
            GradesCount: gradesCount,
            AttendanceSessionsCount: sessionsCount,
            ObservationsCount: observationsCount);
    }

    public async Task ExportBackupZipAsync(string destinationZipPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(destinationZipPath))
        {
            throw new ArgumentException("Backup path cannot be empty.", nameof(destinationZipPath));
        }

        string tempRoot = Path.Combine(Path.GetTempPath(), $"diariolivre-backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            if (File.Exists(_databasePath))
            {
                string snapshotPath = Path.Combine(tempRoot, "diariolivre.db");
                await using var source = new SqliteConnection($"Data Source={_databasePath};Mode=ReadOnly");
                await using var destination = new SqliteConnection($"Data Source={snapshotPath}");
                await source.OpenAsync(ct);
                await destination.OpenAsync(ct);
                source.BackupDatabase(destination);
                SqliteConnection.ClearPool(destination);
                SqliteConnection.ClearPool(source);
            }

            if (Directory.Exists(_avatarsDirectory))
            {
                string targetAvatarsDir = Path.Combine(tempRoot, "avatars");
                Directory.CreateDirectory(targetAvatarsDir);
                foreach (string file in Directory.GetFiles(_avatarsDirectory))
                {
                    string destFile = Path.Combine(targetAvatarsDir, Path.GetFileName(file));
                    File.Copy(file, destFile, overwrite: true);
                }
            }

            string? zipDir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrWhiteSpace(zipDir))
            {
                Directory.CreateDirectory(zipDir);
            }

            if (File.Exists(destinationZipPath))
            {
                File.Delete(destinationZipPath);
            }

            ZipFile.CreateFromDirectory(tempRoot, destinationZipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch
                {
                    // Best-effort temp cleanup.
                }
            }
        }
    }

    public async Task<AutoBackupEntry?> CreateRollingAutoBackupAsync(int maxSnapshots = 5, CancellationToken ct = default)
    {
        if (!File.Exists(_databasePath))
        {
            return null;
        }

        int keep = Math.Clamp(maxSnapshots, 1, 30);
        string autoDir = Path.Combine(DataDirectory, "auto-backups");
        Directory.CreateDirectory(autoDir);

        string fileName = $"AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
        string fullPath = Path.Combine(autoDir, fileName);

        await ExportBackupZipAsync(fullPath, ct);

        FileInfo[] snapshots = new DirectoryInfo(autoDir)
            .GetFiles("AutoBackup_*.zip")
            .OrderByDescending(f => f.CreationTimeUtc)
            .ThenByDescending(f => f.Name)
            .ToArray();

        for (int i = keep; i < snapshots.Length; i++)
        {
            try
            {
                snapshots[i].Delete();
            }
            catch
            {
                // Best-effort old snapshot pruning.
            }
        }

        var createdInfo = new FileInfo(fullPath);
        return new AutoBackupEntry(
            createdInfo.Name,
            createdInfo.FullName,
            createdInfo.CreationTime,
            createdInfo.Exists ? createdInfo.Length : 0);
    }

    public Task<IReadOnlyList<AutoBackupEntry>> GetAutoBackupsAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        string autoDir = Path.Combine(DataDirectory, "auto-backups");
        if (!Directory.Exists(autoDir))
        {
            return Task.FromResult<IReadOnlyList<AutoBackupEntry>>(Array.Empty<AutoBackupEntry>());
        }

        List<AutoBackupEntry> entries = new DirectoryInfo(autoDir)
            .GetFiles("AutoBackup_*.zip")
            .OrderByDescending(f => f.CreationTimeUtc)
            .ThenByDescending(f => f.Name)
            .Select(f => new AutoBackupEntry(f.Name, f.FullName, f.CreationTime, f.Length))
            .ToList();

        return Task.FromResult<IReadOnlyList<AutoBackupEntry>>(entries);
    }

    public async Task RestoreBackupZipAsync(string sourceZipPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceZipPath) || !File.Exists(sourceZipPath))
        {
            throw new FileNotFoundException("Backup archive could not be found.", sourceZipPath);
        }

        string tempRoot = Path.Combine(Path.GetTempPath(), $"diariolivre-restore-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            ZipFile.ExtractToDirectory(sourceZipPath, tempRoot, overwriteFiles: true);
            string extractedDb = Path.Combine(tempRoot, "diariolivre.db");
            if (!File.Exists(extractedDb))
            {
                throw new InvalidDataException("Invalid Diário Livre backup archive: diariolivre.db is missing.");
            }

            // Validate SQLite schema before overwriting live data
            await using (var probe = new SqliteConnection($"Data Source={extractedDb};Mode=ReadOnly"))
            {
                await probe.OpenAsync(ct);
                await using SqliteCommand cmd = probe.CreateCommand();
                cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='AcademicYears';";
                long tableExists = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
                SqliteConnection.ClearPool(probe);
                if (tableExists == 0)
                {
                    throw new InvalidDataException("Backup database is missing required Diário Livre tables.");
                }
            }

            SqliteConnection.ClearAllPools();

            string? dbDir = Path.GetDirectoryName(_databasePath);
            if (!string.IsNullOrWhiteSpace(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            foreach (string sidecar in new[] { _databasePath + "-wal", _databasePath + "-shm" })
            {
                if (File.Exists(sidecar))
                {
                    File.Delete(sidecar);
                }
            }

            File.Copy(extractedDb, _databasePath, overwrite: true);

            string extractedAvatars = Path.Combine(tempRoot, "avatars");
            if (Directory.Exists(extractedAvatars))
            {
                Directory.CreateDirectory(_avatarsDirectory);
                foreach (string avatarFile in Directory.GetFiles(extractedAvatars))
                {
                    string target = Path.Combine(_avatarsDirectory, Path.GetFileName(avatarFile));
                    File.Copy(avatarFile, target, overwrite: true);
                }
            }

            await using DiarioLivreDbContext migratedContext = await _contextFactory.CreateDbContextAsync(ct);
            await DatabaseInitializer.EnsureSchemaUpToDateAsync(migratedContext, ct);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                try
                {
                    Directory.Delete(tempRoot, recursive: true);
                }
                catch
                {
                    // Best-effort temp cleanup.
                }
            }
        }
    }
}
