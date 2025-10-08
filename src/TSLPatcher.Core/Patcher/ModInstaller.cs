using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Config;
using TSLPatcher.Core.Installation;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;
using TSLPatcher.Core.Mods;
using TSLPatcher.Core.Reader;

namespace TSLPatcher.Core.Patcher;

/// <summary>
/// Main orchestrator for installing TSLPatcher mods.
/// </summary>
public class ModInstaller
{
    private readonly string _modPath;
    private readonly string _gamePath;
    private readonly string _changesIniPath;
    private readonly PatchLogger _log;

    private PatcherConfig? _config;
    private string? _backup;
    private readonly HashSet<string> _processedBackupFiles = new();

    public Common.Game? Game { get; private set; }
    public string? TslPatchDataPath { get; set; }

    public ModInstaller(
        string modPath,
        string gamePath,
        string changesIniPath,
        PatchLogger? logger = null)
    {
        _modPath = modPath ?? throw new ArgumentNullException(nameof(modPath));
        _gamePath = gamePath ?? throw new ArgumentNullException(nameof(gamePath));
        _changesIniPath = changesIniPath ?? throw new ArgumentNullException(nameof(changesIniPath));
        _log = logger ?? new PatchLogger();

        Game = Installation.Installation.DetermineGame(_gamePath);

        // Handle legacy syntax - look for changes.ini in various locations
        if (!File.Exists(_changesIniPath))
        {
            var fileName = Path.GetFileName(_changesIniPath);
            _changesIniPath = Path.Combine(_modPath, fileName);

            if (!File.Exists(_changesIniPath))
            {
                _changesIniPath = Path.Combine(_modPath, "tslpatchdata", fileName);
            }

            if (!File.Exists(_changesIniPath))
            {
                throw new FileNotFoundException(
                    "Could not find the changes ini file on disk.",
                    _changesIniPath);
            }
        }
    }

    /// <summary>
    /// Gets the patcher configuration, loading it if necessary.
    /// </summary>
    public PatcherConfig Config()
    {
        if (_config != null)
        {
            return _config;
        }

        if (!File.Exists(_changesIniPath))
        {
            throw new FileNotFoundException($"Changes INI file not found: {_changesIniPath}");
        }

        var reader = ConfigReader.FromFilePath(_changesIniPath, _log);
        _config = reader.Load(new PatcherConfig());

        // Check required files
        if (_config.RequiredFiles.Count > 0)
        {
            for (int i = 0; i < _config.RequiredFiles.Count; i++)
            {
                var files = _config.RequiredFiles[i];
                foreach (var file in files)
                {
                    var requiredFilePath = Path.Combine(_gamePath, "Override", file);
                    if (!File.Exists(requiredFilePath))
                    {
                        var message = i < _config.RequiredMessages.Count
                            ? _config.RequiredMessages[i].Trim()
                            : "cannot install - missing a required mod";
                        throw new InvalidOperationException(message);
                    }
                }
            }
        }

        return _config;
    }

    /// <summary>
    /// Creates a backup directory and returns its path.
    /// </summary>
    public (string backupPath, HashSet<string> processedFiles) GetBackup()
    {
        if (_backup != null)
        {
            return (_backup, _processedBackupFiles);
        }

        var backupDir = _modPath;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH.mm.ss");

        // Find the root directory containing tslpatchdata
        while (!Directory.Exists(Path.Combine(backupDir, "tslpatchdata")) &&
               !string.IsNullOrEmpty(Path.GetDirectoryName(backupDir)))
        {
            backupDir = Path.GetDirectoryName(backupDir) ?? backupDir;
        }

        // Remove old uninstall directory if it exists
        var uninstallDir = Path.Combine(backupDir, "uninstall");
        if (Directory.Exists(uninstallDir))
        {
            try
            {
                Directory.Delete(uninstallDir, recursive: true);
            }
            catch (Exception ex)
            {
                _log.AddWarning($"Could not initialize uninstall directory: {ex.Message}");
            }
        }

        // Create new backup directory
        backupDir = Path.Combine(backupDir, "backup", timestamp);
        try
        {
            Directory.CreateDirectory(backupDir);
        }
        catch (Exception ex)
        {
            _log.AddWarning($"Could not create backup folder: {ex.Message}");
        }

        _log.AddNote($"Using backup directory: '{backupDir}'");
        _backup = backupDir;

        return (_backup, _processedBackupFiles);
    }

    /// <summary>
    /// Installs the mod by applying all patches.
    /// </summary>
    public void Install(
        CancellationToken? cancellationToken = null,
        Action<int>? progressCallback = null)
    {
        if (Game == null)
        {
            throw new InvalidOperationException(
                "Chosen KOTOR directory is not a valid installation - cannot initialize ModInstaller.");
        }

        var memory = new PatcherMemory();
        var config = Config();

        var patchesList = new List<PatcherModifications>();
        patchesList.AddRange(config.InstallList);
        // Note: TSLPatcher executes [InstallList] after [TLKList]
        patchesList.AddRange(GetTLKPatches(config));
        patchesList.AddRange(config.Patches2DA);
        patchesList.AddRange(config.PatchesGFF);
        patchesList.AddRange(config.PatchesNSS);
        // Note: TSLPatcher executes [CompileList] after [HACKList]
        patchesList.AddRange(config.PatchesNCS);
        patchesList.AddRange(config.PatchesSSF);

        int totalPatches = patchesList.Count;
        int currentPatch = 0;

        _log.AddNote($"Starting installation of {totalPatches} patches...");

        foreach (var patch in patchesList)
        {
            cancellationToken?.ThrowIfCancellationRequested();

            currentPatch++;
            progressCallback?.Invoke(currentPatch);

            try
            {
                // TODO: Apply the patch
                // This requires implementing the actual patching logic
                _log.AddNote($"[{currentPatch}/{totalPatches}] Processing: {patch}");
            }
            catch (Exception ex)
            {
                _log.AddError($"Error applying patch: {ex.Message}");
                throw;
            }
        }

        _log.AddNote("Installation complete!");
    }

    /// <summary>
    /// Gets TLK patches from the configuration.
    /// </summary>
    private List<PatcherModifications> GetTLKPatches(PatcherConfig config)
    {
        // TODO: Implement TLK patch extraction
        return new List<PatcherModifications>();
    }
}

