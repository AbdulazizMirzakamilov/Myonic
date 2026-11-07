using Myonic.Core.Models;

namespace Myonic.Core.Services;

public interface IDataBackupService
{
    Task<string> ExportToJsonAsync(string? targetFilePath = null);
    Task<BackupSummary> ImportFromJsonAsync(string jsonContent);
    Task<BackupFileInfo> CreateSqliteBackupAsync(string? targetDirectory = null);
    Task<bool> RestoreSqliteBackupAsync(string backupFilePath);
    Task<IReadOnlyList<BackupFileInfo>> GetAvailableBackupsAsync(string? directory = null);
    Task<bool> DeleteBackupFileAsync(string backupFilePath);
    Task ClearAllUserDataAsync(bool keepDefaultExercises = true);
}
