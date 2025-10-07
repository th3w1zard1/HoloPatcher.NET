using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IniParser;
using IniParser.Model;
using TSLPatcher.Core.Config;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Mods;
using TSLPatcher.Core.Mods.TwoDA;
using TSLPatcher.Core.Mods.GFF;
using TSLPatcher.Core.Mods.TLK;
using TSLPatcher.Core.Mods.SSF;
using TSLPatcher.Core.Mods.NSS;

namespace TSLPatcher.Core.Reader;

/// <summary>
/// Reads and parses TSLPatcher configuration files (changes.ini).
/// </summary>
public class ConfigReader
{
    private readonly IniData _ini;
    private readonly string _modPath;
    private readonly PatchLogger _log;
    private readonly HashSet<string> _previouslyParsedSections = new();
    private string? _tslPatchDataPath;

    public PatcherConfig Config { get; private set; }

    public ConfigReader(
        IniData ini,
        string modPath,
        PatchLogger? logger = null,
        string? tslPatchDataPath = null)
    {
        _ini = ini ?? throw new ArgumentNullException(nameof(ini));
        _modPath = modPath ?? throw new ArgumentNullException(nameof(modPath));
        _log = logger ?? new PatchLogger();
        _tslPatchDataPath = tslPatchDataPath;
        Config = new PatcherConfig();
    }

    /// <summary>
    /// Load PatcherConfig from an INI file path.
    /// </summary>
    public static ConfigReader FromFilePath(string filePath, PatchLogger? logger = null)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}", filePath);
        }

        var parser = new IniParser.Parser.IniDataParser();
        parser.Configuration.AllowDuplicateKeys = true;
        parser.Configuration.AllowDuplicateSections = true;
        parser.Configuration.CaseInsensitive = false; // Config files are case-sensitive

        var iniText = File.ReadAllText(filePath);
        var ini = parser.Parse(iniText);

        var modPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        return new ConfigReader(ini, modPath, logger);
    }

    /// <summary>
    /// Load all configuration sections.
    /// </summary>
    public PatcherConfig Load(PatcherConfig config)
    {
        Config = config;
        _previouslyParsedSections.Clear();

        LoadSettings();
        LoadTLKList();
        LoadInstallList();
        Load2DAList();
        LoadGFFList();
        LoadCompileList();
        LoadHackList();
        LoadSSFList();

        _log.AddNote("The ConfigReader finished loading the INI");

        // Check for orphaned sections
        var allSections = _ini.Sections.Select(s => s.SectionName).ToHashSet();
        var orphanedSections = allSections.Except(_previouslyParsedSections);
        if (orphanedSections.Any())
        {
            var orphanedStr = string.Join("\n", orphanedSections);
            _log.AddNote($"There are some orphaned ini sections found in the changes:\n{orphanedStr}");
        }

        return Config;
    }

    private string? GetSectionName(string sectionName)
    {
        var section = _ini.Sections.FirstOrDefault(s =>
            s.SectionName.Equals(sectionName, StringComparison.OrdinalIgnoreCase));

        if (section != null)
        {
            _previouslyParsedSections.Add(section.SectionName);
        }

        return section?.SectionName;
    }

    private void LoadSettings()
    {
        var settingsSection = GetSectionName("Settings");
        if (settingsSection == null)
        {
            _log.AddWarning("[Settings] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [Settings] section from ini...");
        var settings = _ini[settingsSection];

        Config.WindowTitle = GetValue(settings, "WindowCaption") ?? string.Empty;
        Config.ConfirmMessage = GetValue(settings, "ConfirmMessage") ?? string.Empty;

        // Parse Required files
        foreach (var key in settings)
        {
            var lowerKey = key.KeyName.ToLower();
            if (lowerKey == "required" || (lowerKey.StartsWith("required") && !lowerKey.StartsWith("requiredmsg")))
            {
                var files = key.Value.Split(',').Select(f => f.Trim()).ToList();
                Config.RequiredFiles.Add(files);
            }
            else if (lowerKey == "requiredmsg" || lowerKey.StartsWith("requiredmsg"))
            {
                Config.RequiredMessages.Add(key.Value.Trim());
            }
        }

        if (Config.RequiredFiles.Count != Config.RequiredMessages.Count)
        {
            throw new InvalidOperationException(
                $"Required files definitions must match required msg count " +
                $"({Config.RequiredFiles.Count}/{Config.RequiredMessages.Count})");
        }

        Config.SaveProcessedScripts = int.TryParse(GetValue(settings, "SaveProcessedScripts"), out var sps) ? sps : 0;
        Config.LogLevel = Enum.TryParse<LogLevel>(GetValue(settings, "LogLevel"), out var logLevel)
            ? logLevel
            : LogLevel.Warnings;

        Config.IgnoreFileExtensions = bool.TryParse(GetValue(settings, "IgnoreExtensions"), out var ign) && ign;

        var lookupGameNumber = GetValue(settings, "LookupGameNumber");
        if (!string.IsNullOrWhiteSpace(lookupGameNumber))
        {
            if (lookupGameNumber != "1" && lookupGameNumber != "2")
            {
                throw new InvalidOperationException(
                    $"Invalid: 'LookupGameNumber={lookupGameNumber}' in [Settings], must be 1 or 2 representing the KOTOR game.");
            }
            Config.GameNumber = int.Parse(lookupGameNumber);
        }
    }

    private void LoadTLKList()
    {
        var tlkListSection = GetSectionName("TLKList");
        if (tlkListSection == null)
        {
            _log.AddNote("[TLKList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [TLKList] patches from ini...");
        // TODO: Implement TLK list parsing
        // This requires parsing the TLK modification format from the INI
    }

    private void LoadInstallList()
    {
        var installListSection = GetSectionName("InstallList");
        if (installListSection == null)
        {
            _log.AddNote("[InstallList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [InstallList] patches from ini...");
        // TODO: Implement install list parsing
        // This requires parsing file installation instructions
    }

    private void Load2DAList()
    {
        var twodaListSection = GetSectionName("2DAList");
        if (twodaListSection == null)
        {
            _log.AddNote("[2DAList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [2DAList] patches from ini...");
        // TODO: Implement 2DA list parsing
    }

    private void LoadGFFList()
    {
        var gffListSection = GetSectionName("GFFList");
        if (gffListSection == null)
        {
            _log.AddNote("[GFFList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [GFFList] patches from ini...");
        // TODO: Implement GFF list parsing
    }

    private void LoadCompileList()
    {
        var compileListSection = GetSectionName("CompileList");
        if (compileListSection == null)
        {
            _log.AddNote("[CompileList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [CompileList] patches from ini...");
        // TODO: Implement compile list parsing
    }

    private void LoadHackList()
    {
        var hackListSection = GetSectionName("HACKList");
        if (hackListSection == null)
        {
            _log.AddNote("[HACKList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [HACKList] patches from ini...");
        // TODO: Implement hack list parsing
    }

    private void LoadSSFList()
    {
        var ssfListSection = GetSectionName("SSFList");
        if (ssfListSection == null)
        {
            _log.AddNote("[SSFList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [SSFList] patches from ini...");
        // TODO: Implement SSF list parsing
    }

    private static string? GetValue(KeyDataCollection section, string key)
    {
        return section.FirstOrDefault(k =>
            k.KeyName.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
    }
}

