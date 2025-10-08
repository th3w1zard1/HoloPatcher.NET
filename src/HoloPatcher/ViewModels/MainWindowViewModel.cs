using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TSLPatcher.Core.Config;
using TSLPatcher.Core.Installation;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Namespaces;
using TSLPatcher.Core.Patcher;
using TSLPatcher.Core.Reader;

namespace HoloPatcher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly PatchLogger _logger = new();
    private readonly StringBuilder _logTextBuilder = new();

    [ObservableProperty]
    private string _logText = string.Empty;

    [ObservableProperty]
    private bool _isTaskRunning;

    [ObservableProperty]
    private int _progressValue;

    [ObservableProperty]
    private int _progressMaximum = 100;

    [ObservableProperty]
    private string? _selectedNamespace;

    [ObservableProperty]
    private string? _selectedGamePath;

    [ObservableProperty]
    private string _modPath = string.Empty;

    private List<PatcherNamespace> _loadedNamespaces = new();
    private CancellationTokenSource? _cancellationTokenSource;

    public ObservableCollection<string> Namespaces { get; } = new();
    public ObservableCollection<string> GamePaths { get; } = new();

    public bool CanInstall => !IsTaskRunning &&
                              !string.IsNullOrEmpty(SelectedNamespace) &&
                              !string.IsNullOrEmpty(SelectedGamePath);

    public MainWindowViewModel()
    {
        // Subscribe to logger events
        _logger.VerboseLogged += OnLogEntry;
        _logger.NoteLogged += OnLogEntry;
        _logger.WarningLogged += OnLogEntry;
        _logger.ErrorLogged += OnLogEntry;

        // Initialize with welcome message
        AddLogEntry("Welcome to HoloPatcher!");
        AddLogEntry("Select a mod and your KOTOR directory to begin.");

        // Try to detect KOTOR installations
        DetectGamePaths();
    }

    private void OnLogEntry(object? sender, PatchLog log)
    {
        AddLogEntry(log.FormattedMessage);
    }

    private void AddLogEntry(string message)
    {
        _logTextBuilder.AppendLine(message);
        LogText = _logTextBuilder.ToString();
    }

    [RelayCommand]
    private async Task BrowseMod()
    {
        var window = GetMainWindow();
        if (window == null) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Mod Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            await LoadModFromPath(path);
        }
    }

    [RelayCommand]
    private async Task BrowseGamePath()
    {
        var window = GetMainWindow();
        if (window == null) return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select KOTOR Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            var path = folders[0].Path.LocalPath;
            if (!GamePaths.Contains(path))
            {
                GamePaths.Add(path);
            }
            SelectedGamePath = path;
        }
    }

    [RelayCommand]
    private async Task Install()
    {
        if (string.IsNullOrEmpty(SelectedGamePath) || string.IsNullOrEmpty(SelectedNamespace))
        {
            AddLogEntry("[ERROR] Please select both a mod and game directory.");
            return;
        }

        IsTaskRunning = true;
        ProgressValue = 0;
        _cancellationTokenSource = new CancellationTokenSource();

        try
        {
            AddLogEntry("Starting installation...");

            await Task.Run(() =>
            {
                var selectedNs = _loadedNamespaces.FirstOrDefault(ns => ns.Name == SelectedNamespace);
                if (selectedNs == null)
                {
                    throw new InvalidOperationException("Selected namespace not found.");
                }

                var tslPatchDataPath = Path.Combine(ModPath, "tslpatchdata");
                var iniFilePath = Path.Combine(tslPatchDataPath, selectedNs.ChangesFilePath());

                if (!File.Exists(iniFilePath))
                {
                    throw new FileNotFoundException($"Changes INI file not found: {iniFilePath}");
                }

                var installer = new ModInstaller(ModPath, SelectedGamePath, iniFilePath, _logger)
                {
                    TslPatchDataPath = tslPatchDataPath
                };

                var config = installer.Config();
                ProgressMaximum = config.InstallList.Count + config.Patches2DA.Count +
                                 config.PatchesGFF.Count + config.PatchesNSS.Count +
                                 config.PatchesSSF.Count;

                installer.Install(
                    _cancellationTokenSource.Token,
                    progress => ProgressValue = progress
                );
            }, _cancellationTokenSource.Token);

            AddLogEntry("Installation complete!");
        }
        catch (OperationCanceledException)
        {
            AddLogEntry("[WARNING] Installation was cancelled.");
        }
        catch (Exception ex)
        {
            AddLogEntry($"[ERROR] Installation failed: {ex.Message}");
            AddLogEntry($"[ERROR] {ex.StackTrace}");
        }
        finally
        {
            IsTaskRunning = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        var window = GetMainWindow();
        window?.Close();
    }

    [RelayCommand]
    private void ValidateIni()
    {
        AddLogEntry("Validating INI...");
        // TODO: Implement validation
    }

    [RelayCommand]
    private void UninstallMod()
    {
        AddLogEntry("Uninstall functionality not yet implemented.");
        // TODO: Implement uninstall
    }

    [RelayCommand]
    private void FixPermissions()
    {
        AddLogEntry("Fix permissions functionality not yet implemented.");
        // TODO: Implement permission fixing
    }

    [RelayCommand]
    private void FixCaseSensitivity()
    {
        AddLogEntry("Fix case sensitivity functionality not yet implemented.");
        // TODO: Implement case sensitivity fixing
    }

    [RelayCommand]
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            AddLogEntry($"[ERROR] Failed to open URL: {ex.Message}");
        }
    }

    [RelayCommand]
    private void CheckUpdates()
    {
        AddLogEntry("Check for updates functionality not yet implemented.");
        // TODO: Implement update checking
    }

    [RelayCommand]
    private void ShowNamespaceInfo()
    {
        if (string.IsNullOrEmpty(SelectedNamespace))
        {
            AddLogEntry("[INFO] No namespace selected.");
            return;
        }
        // TODO: Show namespace description dialog
    }

    private async Task LoadModFromPath(string path)
    {
        ModPath = path;
        AddLogEntry($"Loading mod from: {path}");

        await Task.Run(() =>
        {
            try
            {
                // Check for tslpatchdata folder
                var tslPatchDataPath = Path.Combine(path, "tslpatchdata");
                if (!Directory.Exists(tslPatchDataPath))
                {
                    // Try the path itself
                    if (!File.Exists(Path.Combine(path, "changes.ini")))
                    {
                        AddLogEntry("[ERROR] Could not find tslpatchdata folder or changes.ini file.");
                        return;
                    }
                    tslPatchDataPath = path;
                }

                // Try to load namespaces.ini
                var namespacesIniPath = Path.Combine(tslPatchDataPath, "namespaces.ini");
                if (File.Exists(namespacesIniPath))
                {
                    _loadedNamespaces = NamespaceReader.FromFilePath(namespacesIniPath);
                    AddLogEntry($"Loaded {_loadedNamespaces.Count} namespace(s) from namespaces.ini");
                }
                else
                {
                    // Fall back to changes.ini
                    var changesIniPath = Path.Combine(tslPatchDataPath, "changes.ini");
                    if (File.Exists(changesIniPath))
                    {
                        _loadedNamespaces = new List<PatcherNamespace>
                        {
                            new PatcherNamespace("changes.ini", "info.rtf")
                            {
                                Name = "Default",
                                Description = "Default installation"
                            }
                        };
                        AddLogEntry("Using default changes.ini");
                    }
                    else
                    {
                        AddLogEntry("[ERROR] Could not find namespaces.ini or changes.ini");
                        return;
                    }
                }

                // Update UI on main thread
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    Namespaces.Clear();
                    foreach (var ns in _loadedNamespaces)
                    {
                        var displayName = !string.IsNullOrWhiteSpace(ns.Name) ? ns.Name : ns.ChangesFilePath();
                        Namespaces.Add(displayName);
                    }

                    if (Namespaces.Count > 0)
                    {
                        SelectedNamespace = Namespaces[0];
                    }
                });
            }
            catch (Exception ex)
            {
                AddLogEntry($"[ERROR] Failed to load mod: {ex.Message}");
            }
        });
    }

    private void DetectGamePaths()
    {
        // Common installation paths for KOTOR
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Knights of the Old Republic II",
            @"C:\Program Files (x86)\Steam\steamapps\common\swkotor",
            @"C:\Program Files\Steam\steamapps\common\Knights of the Old Republic II",
            @"C:\Program Files\Steam\steamapps\common\swkotor",
            @"C:\GOG Games\Star Wars - KotOR",
            @"C:\GOG Games\Star Wars - KotOR2",
        };

        foreach (var path in commonPaths)
        {
            if (Directory.Exists(path) && Installation.IsValidInstallation(path))
            {
                if (!GamePaths.Contains(path))
                {
                    GamePaths.Add(path);
                }
            }
        }

        if (GamePaths.Count > 0)
        {
            SelectedGamePath = GamePaths[0];
            AddLogEntry($"Detected {GamePaths.Count} KOTOR installation(s).");
        }
    }

    private Window? GetMainWindow()
    {
        return App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }

    partial void OnIsTaskRunningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnSelectedNamespaceChanged(string? value)
    {
        OnPropertyChanged(nameof(CanInstall));
    }

    partial void OnSelectedGamePathChanged(string? value)
    {
        OnPropertyChanged(nameof(CanInstall));
    }
}

