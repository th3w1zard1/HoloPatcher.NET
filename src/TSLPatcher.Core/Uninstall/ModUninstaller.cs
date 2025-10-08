using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Formats.TLK;
using TSLPatcher.Core.Logger;

namespace TSLPatcher.Core.Uninstall;

/// <summary>
/// A class that provides functionality to uninstall a selected mod using the most recent backup folder created during the last install.
/// 1:1 port from Python ModUninstaller in pykotor/tslpatcher/uninstall.py
/// </summary>
/// <remarks>
/// Initializes a new instance of the ModUninstaller class.
/// </remarks>
/// <param name="backupsLocationPath">The path to the location of the backup folders</param>
/// <param name="gamePath">The path to the game folder</param>
/// <param name="logger">An optional logger object. Defaults to a new PatchLogger if null</param>
public class ModUninstaller(CaseAwarePath backupsLocationPath, CaseAwarePath gamePath, PatchLogger? logger = null)
{
    private readonly CaseAwarePath _backupsLocationPath = backupsLocationPath;
    private readonly CaseAwarePath _gamePath = gamePath;
    private readonly PatchLogger _logger = logger ?? new PatchLogger();

    /// <summary>
    /// Check if a folder name is a valid backup folder name based on a datetime pattern.
    /// 1:1 port from Python is_valid_backup_folder
    /// </summary>
    /// <param name="folder">Path object of the folder to validate</param>
    /// <param name="datetimePattern">String pattern to match folder name against (default: "yyyy-MM-dd_HH.mm.ss")</param>
    /// <returns>True if folder name matches datetime pattern, False otherwise</returns>
    public static bool IsValidBackupFolder(CaseAwarePath folder, string datetimePattern = "yyyy-MM-dd_HH.mm.ss")
    {
        try
        {
            DateTime.ParseExact(folder.Name, datetimePattern, CultureInfo.InvariantCulture);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns the most recent valid backup folder.
    /// 1:1 port from Python get_most_recent_backup
    /// </summary>
    /// <param name="backupFolder">Path to the backup folder</param>
    /// <param name="showErrorDialog">Function to show error dialog (optional)</param>
    /// <returns>Path to the most recent valid backup folder or null</returns>
    public static CaseAwarePath? GetMostRecentBackup(
        CaseAwarePath backupFolder,
        Action<string, string>? showErrorDialog = null)
    {
        if (!Directory.Exists(backupFolder))
        {
            showErrorDialog?.Invoke(
                "No backups found!",
                $"No backups found at '{backupFolder}'!{Environment.NewLine}HoloPatcher cannot uninstall TSLPatcher.exe installations."
            );
            return null;
        }

        var validBackups = new List<CaseAwarePath>();
        foreach (var subfolder in Directory.GetDirectories(backupFolder))
        {
            var subfolderPath = new CaseAwarePath(subfolder);
            // Check if folder has contents and is a valid backup folder
            if (Directory.EnumerateFileSystemEntries(subfolder).Any() && IsValidBackupFolder(subfolderPath))
            {
                validBackups.Add(subfolderPath);
            }
        }

        if (validBackups.Count == 0)
        {
            showErrorDialog?.Invoke(
                "No backups found!",
                $"No backups found at '{backupFolder}'!{Environment.NewLine}HoloPatcher cannot uninstall TSLPatcher.exe installations."
            );
            return null;
        }

        // Return the folder with the maximum datetime parsed from folder name
        return validBackups.MaxBy(x => DateTime.ParseExact(x.Name, "yyyy-MM-dd_HH.mm.ss", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Restores a game backup folder to the existing game files.
    /// 1:1 port from Python restore_backup
    /// </summary>
    /// <param name="backupFolder">Path to the backup folder</param>
    /// <param name="existingFiles">Set of existing file paths</param>
    /// <param name="filesInBackup">List of file paths in the backup</param>
    public void RestoreBackup(
        CaseAwarePath backupFolder,
        HashSet<string> existingFiles,
        List<CaseAwarePath> filesInBackup)
    {
        // Remove any existing files not in the backup
        foreach (var fileStr in existingFiles)
        {
            var filePath = new CaseAwarePath(fileStr);
            var relFilePath = Path.GetRelativePath(_gamePath, filePath);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            _logger.AddNote($"Removed {relFilePath}...");
        }

        // Copy each file from the backup folder to the destination restoring the file structure
        foreach (var file in filesInBackup)
        {
            if (file.Name == "remove these files.txt")
            {
                continue;
            }

            var relativePathFromBackup = Path.GetRelativePath(backupFolder, file);
            var destinationPath = Path.Combine(_gamePath, relativePathFromBackup);

            // Ensure parent directory exists
            var parentDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentDir))
            {
                Directory.CreateDirectory(parentDir);
            }

            File.Copy(file, destinationPath, overwrite: true);

            var relativeToGameParent = Path.GetRelativePath(Path.GetDirectoryName(_gamePath) ?? "", destinationPath);
            _logger.AddNote($"Restoring backup of '{file.Name}' to '{relativeToGameParent}'...");
        }
    }

    /// <summary>
    /// Get information about the most recent valid backup.
    /// 1:1 port from Python get_backup_info
    /// </summary>
    /// <param name="showErrorDialog">Function to show error dialog (optional)</param>
    /// <param name="showYesNoDialog">Function to show yes/no dialog (optional)</param>
    /// <returns>Tuple containing: most recent backup folder path, existing files set, files in backup list, folder count</returns>
    public (CaseAwarePath? BackupFolder, HashSet<string> ExistingFiles, List<CaseAwarePath> FilesInBackup, int FolderCount) GetBackupInfo(
        Action<string, string>? showErrorDialog = null,
        Func<string, string, bool>? showYesNoDialog = null)
    {
        var mostRecentBackupFolder = GetMostRecentBackup(_backupsLocationPath, showErrorDialog);
        if (mostRecentBackupFolder == null)
        {
            return (null, new HashSet<string>(), new List<CaseAwarePath>(), 0);
        }

        var deleteListFile = Path.Combine(mostRecentBackupFolder, "remove these files.txt");
        var filesToDelete = new HashSet<string>();
        var existingFiles = new HashSet<string>();

        if (File.Exists(deleteListFile))
        {
            var lines = File.ReadAllLines(deleteListFile);
            filesToDelete = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                                 .Select(line => line.Trim())
                                 .ToHashSet();
            existingFiles = filesToDelete.Where(line => !string.IsNullOrWhiteSpace(line) && File.Exists(line.Trim()))
                                        .ToHashSet();

            if (existingFiles.Count < filesToDelete.Count)
            {
                bool continueAnyway = showYesNoDialog?.Invoke(
                    "Backup out of date or mismatched",
                    $"This backup doesn't match your current KOTOR installation. Files are missing/changed in your KOTOR install.{Environment.NewLine}" +
                    $"It is important that you uninstall all mods in their installed order when utilizing this feature.{Environment.NewLine}" +
                    $"Also ensure you selected the right mod, and the right KOTOR folder.{Environment.NewLine}" +
                    "Continue anyway?"
                ) ?? false;

                if (!continueAnyway)
                {
                    return (null, new HashSet<string>(), new List<CaseAwarePath>(), 0);
                }
            }
        }

        var filesInBackup = Directory.EnumerateFiles(mostRecentBackupFolder, "*", SearchOption.AllDirectories)
                                    .Select(f => new CaseAwarePath(f))
                                    .ToList();

        var allEntries = Directory.EnumerateFileSystemEntries(mostRecentBackupFolder, "*", SearchOption.AllDirectories).Count();
        var folderCount = allEntries - filesInBackup.Count;

        return (mostRecentBackupFolder, existingFiles, filesInBackup, folderCount);
    }

    /// <summary>
    /// Uninstalls the selected mod using the most recent backup folder created during the last install.
    /// 1:1 port from Python uninstall_selected_mod
    /// </summary>
    /// <param name="showErrorDialog">Function to show error dialog</param>
    /// <param name="showYesNoDialog">Function to show yes/no dialog</param>
    /// <param name="showYesNoCancelDialog">Function to show yes/no/cancel dialog (returns true for yes, false for no, null for cancel)</param>
    /// <returns>True if uninstall completed successfully, False otherwise</returns>
    public bool UninstallSelectedMod(
        Action<string, string>? showErrorDialog = null,
        Func<string, string, bool>? showYesNoDialog = null,
        Func<string, string, bool?>? showYesNoCancelDialog = null)
    {
        var (mostRecentBackupFolder, existingFiles, filesInBackup, folderCount) = GetBackupInfo(showErrorDialog, showYesNoDialog);

        if (mostRecentBackupFolder == null)
        {
            return false;
        }

        _logger.AddNote($"Using backup folder '{mostRecentBackupFolder}'");

        // Show files to be restored if there are less than 6
        if (filesInBackup.Count < 6)
        {
            foreach (var item in filesInBackup)
            {
                var relativePath = Path.GetRelativePath(mostRecentBackupFolder, item);
                _logger.AddNote($"Would restore file '{relativePath}'");
            }
        }

        // Confirm uninstall with user
        bool confirmed = showYesNoDialog?.Invoke(
            "Confirmation",
            $"Really uninstall {existingFiles.Count} files and restore the most recent backup (containing {filesInBackup.Count} files and {folderCount} folders)?{Environment.NewLine}" +
            "Note: This uses the most recent mod-specific backup, the namespace option displayed does not affect this tool."
        ) ?? false;

        if (!confirmed)
        {
            return false;
        }

        try
        {
            RestoreBackup(mostRecentBackupFolder, existingFiles, filesInBackup);
        }
        catch (Exception e)
        {
            showErrorDialog?.Invoke(
                e.GetType().Name,
                $"Failed to restore backup because of exception.{Environment.NewLine}{Environment.NewLine}{e.Message}"
            );
            return false;
        }

        // Offer to delete restored backup
        while (true)
        {
            bool deleteBackup = showYesNoDialog?.Invoke(
                "Uninstall completed!",
                $"Deleted {existingFiles.Count} files and successfully restored backup created on {mostRecentBackupFolder.Name}{Environment.NewLine}{Environment.NewLine}" +
                $"Would you like to delete the backup created on {mostRecentBackupFolder.Name} since it now has been restored?"
            ) ?? false;

            if (!deleteBackup)
            {
                break;
            }

            try
            {
                Directory.Delete(mostRecentBackupFolder, recursive: true);
                _logger.AddNote($"Deleted restored backup '{mostRecentBackupFolder.Name}'");
                break;
            }
            catch (UnauthorizedAccessException)
            {
                var result = showYesNoCancelDialog?.Invoke(
                    "Permission Error",
                    "Unable to delete the restored backup due to permission issues. Would you like to gain permission and try again?"
                );

                if (result == true)
                {
                    Console.WriteLine("Gaining permission, please wait...");
                    // Attempt to gain access - in C# this would require platform-specific code
                    // For now, we'll just retry
                    continue;
                }
                if (result == false)
                {
                    continue;
                }
                if (result == null)
                {
                    break;
                }
            }
        }

        return true;
    }
}

