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
/// Complete implementation matching Python reader.py
/// </summary>
public class ConfigReader
{
    private const string SectionNotFoundError = "The [{0}] section was not found in the ini";
    private const string ReferencesTracebackMsg = ", referenced by '{0}={1}' in [{2}]";

    private readonly IniData _ini;
    private readonly string _modPath;
    private readonly PatchLogger _log;
    private readonly HashSet<string> _previouslyParsedSections = new();
    private readonly string? _tslPatchDataPath;

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
        parser.Configuration.CaseInsensitive = false;

        var iniText = File.ReadAllText(filePath);
        var ini = parser.Parse(iniText);

        var modPath = Path.GetDirectoryName(filePath) ?? string.Empty;
        var tslPatchDataPath = Path.Combine(modPath, "tslpatchdata");

        return new ConfigReader(ini, modPath, logger,
            Directory.Exists(tslPatchDataPath) ? tslPatchDataPath : null);
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

    #region Settings Loading

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
            if (lowerKey == "required" || (lowerKey.StartsWith("required") &&
                !lowerKey.StartsWith("requiredmsg") &&
                lowerKey.Length > "required".Length))
            {
                // Validate key format
                if (lowerKey != "required" && !lowerKey.Substring("required".Length).All(char.IsDigit))
                {
                    throw new InvalidOperationException(
                        $"Key '{key.KeyName}' improperly defined in settings ini. Expected (Required) or (RequiredMsg)");
                }

                var files = key.Value.Split(',').Select(f => f.Trim()).ToList();
                Config.RequiredFiles.Add(files.ToArray());
            }
            else if (lowerKey == "requiredmsg" || (lowerKey.StartsWith("requiredmsg") &&
                lowerKey.Length > "requiredmsg".Length))
            {
                if (lowerKey != "requiredmsg" && !lowerKey.Substring("requiredmsg".Length).All(char.IsDigit))
                {
                    throw new InvalidOperationException(
                        $"Key '{key.KeyName}' improperly defined in settings ini. Expected (Required) or (RequiredMsg)");
                }
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

        var logLevelStr = GetValue(settings, "LogLevel");
        Config.LogLevel = int.TryParse(logLevelStr, out var logLevelInt)
            ? (LogLevel)logLevelInt
            : LogLevel.Warnings;

        Config.IgnoreFileExtensions = bool.TryParse(GetValue(settings, "IgnoreExtensions"), out var ign) && ign;

        var lookupGameNumber = GetValue(settings, "LookupGameNumber");
        if (!string.IsNullOrWhiteSpace(lookupGameNumber))
        {
            lookupGameNumber = lookupGameNumber.Trim();
            if (lookupGameNumber != "1" && lookupGameNumber != "2")
            {
                throw new InvalidOperationException(
                    $"Invalid: 'LookupGameNumber={lookupGameNumber}' in [Settings], must be 1 or 2 representing the KOTOR game.");
            }
            Config.GameNumber = int.Parse(lookupGameNumber);
        }
    }

    #endregion

    #region Install List Loading

    private void LoadInstallList()
    {
        var installListSection = GetSectionName("InstallList");
        if (installListSection == null)
        {
            _log.AddNote("[InstallList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [InstallList] patches from ini...");

        var installList = _ini[installListSection];
        foreach (var folderEntry in installList)
        {
            var foldername = folderEntry.Value;
            var folderKey = folderEntry.KeyName;

            var foldernameSection = GetSectionName(folderKey);
            if (foldernameSection == null)
            {
                throw new KeyNotFoundException(
                    string.Format(SectionNotFoundError, foldername) +
                    string.Format(ReferencesTracebackMsg, folderKey, foldername, installListSection));
            }

            var folderSection = _ini[foldernameSection];
            var sourcefolder = GetValue(folderSection, "!SourceFolder") ?? ".";

            foreach (var fileEntry in folderSection.Where(k =>
                !k.KeyName.Equals("!SourceFolder", StringComparison.OrdinalIgnoreCase)))
            {
                var filename = fileEntry.Value;
                var fileKey = fileEntry.KeyName;

                var fileInstall = new InstallFile(
                    filename,
                    fileKey.StartsWith("replace", StringComparison.OrdinalIgnoreCase))
                {
                    Destination = foldername,
                    SourceFolder = sourcefolder
                };

                Config.InstallList.Add(fileInstall);

                // Optional file-specific section
                var fileSection = GetSectionName(filename);
                if (fileSection != null)
                {
                    var fileSectionData = _ini[fileSection];
                    // Pop TSLPatcher variables if defined
                    fileInstall.Destination = GetValue(fileSectionData, "!Destination") ?? fileInstall.Destination;
                    fileInstall.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? fileInstall.SourceFolder;
                }
            }
        }
    }

    #endregion

    #region TLK List Loading

    private void LoadTLKList()
    {
        var tlkListSection = GetSectionName("TLKList");
        if (tlkListSection == null)
        {
            _log.AddNote("[TLKList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [TLKList] patches from ini...");
        var tlkList = _ini[tlkListSection];

        var defaultDestination = GetValue(tlkList, "!DefaultDestination") ?? ModificationsTLK.DefaultDestination;
        var defaultSourceFolder = GetValue(tlkList, "!DefaultSourceFolder") ?? ".";

        Config.PatchesTLK.Destination = defaultDestination;
        Config.PatchesTLK.SourceFolder = defaultSourceFolder;
        Config.PatchesTLK.SourceFile = GetValue(tlkList, "!SourceFile") ?? "append.tlk";

        bool syntaxErrorCaught = false;

        foreach (var entry in tlkList.Where(k => !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var key = entry.KeyName;
            var value = entry.Value;
            var lowerKey = key.ToLower();

            bool replaceFile = lowerKey.StartsWith("replace");
            bool appendFile = lowerKey.StartsWith("append");

            try
            {
                if (lowerKey.StartsWith("strref"))
                {
                    var dialogTlkIndex = int.Parse(lowerKey.Substring(6));
                    var modTlkIndex = int.Parse(value);

                    var modifier = new ModifyTLK(dialogTlkIndex, false)
                    {
                        ModIndex = modTlkIndex,
                        TlkFilePath = Path.Combine(_modPath, Config.PatchesTLK.SourceFolder, Config.PatchesTLK.SourceFile)
                    };
                    Config.PatchesTLK.Modifiers.Add(modifier);
                }
                else if (replaceFile || appendFile)
                {
                    var nextSectionName = GetSectionName(value);
                    if (nextSectionName == null)
                    {
                        syntaxErrorCaught = true;
                        throw new InvalidOperationException(
                            string.Format(SectionNotFoundError, value) +
                            string.Format(ReferencesTracebackMsg, key, value, tlkListSection));
                    }

                    var nextSection = _ini[nextSectionName];

                    // Update source file if specified
                    Config.PatchesTLK.SourceFile = GetValue(nextSection, "!SourceFile") ?? Config.PatchesTLK.SourceFile;

                    foreach (var tlkEntry in nextSection.Where(k => !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
                    {
                        var rawDialogTlkIndex = tlkEntry.KeyName;
                        var rawModTlkIndex = tlkEntry.Value;

                        var dialogTlkIndex = rawDialogTlkIndex.ToLower().StartsWith("strref")
                            ? int.Parse(rawDialogTlkIndex.Substring(6))
                            : int.Parse(rawDialogTlkIndex);

                        var modTlkIndex = rawModTlkIndex.ToLower().StartsWith("strref")
                            ? int.Parse(rawModTlkIndex.Substring(6))
                            : int.Parse(rawModTlkIndex);

                        var modifier = new ModifyTLK(dialogTlkIndex, replaceFile)
                        {
                            ModIndex = modTlkIndex,
                            TlkFilePath = Path.Combine(_modPath, Config.PatchesTLK.SourceFolder, nextSectionName)
                        };
                        Config.PatchesTLK.Modifiers.Add(modifier);
                    }
                }
                else if (lowerKey.Contains('\\') || lowerKey.Contains('/'))
                {
                    var delimiter = lowerKey.Contains('\\') ? '\\' : '/';
                    var parts = lowerKey.Split(delimiter);
                    var tokenIdStr = parts[0];
                    var propertyName = parts[1];
                    var tokenId = int.Parse(tokenIdStr);

                    if (propertyName == "text")
                    {
                        var modifier = new ModifyTLK(tokenId, true) { Text = value };
                        Config.PatchesTLK.Modifiers.Add(modifier);
                    }
                    else if (propertyName == "sound")
                    {
                        var modifier = new ModifyTLK(tokenId, true) { Sound = value };
                        Config.PatchesTLK.Modifiers.Add(modifier);
                    }
                    else
                    {
                        syntaxErrorCaught = true;
                        throw new InvalidOperationException(
                            $"Invalid [TLKList] syntax: '{key}={value}'! Expected '{key}' to be one of ['Sound', 'Text']");
                    }
                }
                else
                {
                    syntaxErrorCaught = true;
                    throw new InvalidOperationException(
                        $"Invalid syntax found in [TLKList] '{key}={value}'! Expected '{key}' to be one of " +
                        $"['AppendFile', 'ReplaceFile', '!SourceFile', 'StrRef', 'Text', 'Sound']");
                }
            }
            catch (Exception ex) when (!syntaxErrorCaught)
            {
                throw new InvalidOperationException($"Could not parse '{key}={value}' in [TLKList]", ex);
            }
        }
    }

    #endregion

    #region 2DA List Loading

    private void Load2DAList()
    {
        var twodaListSection = GetSectionName("2DAList");
        if (twodaListSection == null)
        {
            _log.AddNote("[2DAList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [2DAList] patches from ini...");
        var twodaList = _ini[twodaListSection];

        var defaultDestination = GetValue(twodaList, "!DefaultDestination") ?? Modifications2DA.DefaultDestination;
        var defaultSourceFolder = GetValue(twodaList, "!DefaultSourceFolder") ?? ".";

        foreach (var entry in twodaList.Where(k =>
            !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var identifier = entry.KeyName;
            var file = entry.Value;

            var fileSection = GetSectionName(file);
            if (fileSection == null)
            {
                throw new KeyNotFoundException(
                    string.Format(SectionNotFoundError, file) +
                    string.Format(ReferencesTracebackMsg, identifier, file, twodaListSection));
            }

            var modifications = new Modifications2DA(file)
            {
                Destination = defaultDestination,
                SourceFolder = defaultSourceFolder
            };

            var fileSectionData = _ini[fileSection];

            // Override with section-specific settings
            modifications.Destination = GetValue(fileSectionData, "!Destination") ?? modifications.Destination;
            modifications.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? modifications.SourceFolder;

            // Parse modifiers
            foreach (var modEntry in fileSectionData.Where(k =>
                !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
            {
                var key = modEntry.KeyName;
                var modificationId = modEntry.Value;

                var nextSectionName = GetSectionName(modificationId);
                if (nextSectionName == null)
                {
                    throw new KeyNotFoundException(
                        string.Format(SectionNotFoundError, modificationId) +
                        string.Format(ReferencesTracebackMsg, key, modificationId, fileSection));
                }

                var modificationIdsDict = _ini[nextSectionName];
                var manipulation = Discern2DA(key, modificationId, modificationIdsDict);
                if (manipulation != null)
                {
                    modifications.Modifiers.Add(manipulation);
                }
            }

            Config.Patches2DA.Add(modifications);
        }
    }

    #endregion

    #region SSF List Loading

    private void LoadSSFList()
    {
        var ssfListSection = GetSectionName("SSFList");
        if (ssfListSection == null)
        {
            _log.AddNote("[SSFList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [SSFList] patches from ini...");
        var ssfList = _ini[ssfListSection];

        var defaultDestination = GetValue(ssfList, "!DefaultDestination") ?? ModificationsSSF.DefaultDestination;
        var defaultSourceFolder = GetValue(ssfList, "!DefaultSourceFolder") ?? ".";

        foreach (var entry in ssfList.Where(k =>
            !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var identifier = entry.KeyName;
            var file = entry.Value;

            var ssfFileSection = GetSectionName(file);
            if (ssfFileSection == null)
            {
                throw new KeyNotFoundException(
                    string.Format(SectionNotFoundError, file) +
                    string.Format(ReferencesTracebackMsg, identifier, file, ssfListSection));
            }

            var replace = identifier.StartsWith("replace", StringComparison.OrdinalIgnoreCase);
            var modifications = new ModificationsSSF(file, replace)
            {
                Destination = defaultDestination,
                SourceFolder = defaultSourceFolder
            };

            var fileSectionData = _ini[ssfFileSection];

            // Override with section-specific settings
            modifications.Destination = GetValue(fileSectionData, "!Destination") ?? modifications.Destination;
            modifications.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? modifications.SourceFolder;

            foreach (var soundEntry in fileSectionData.Where(k =>
                !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
            {
                var name = soundEntry.KeyName;
                var value = soundEntry.Value;

                // Parse SSF sound modifications
                // TODO: Implement full SSF parsing with TokenUsage classes
                // For now, store as basic modifiers
            }

            Config.PatchesSSF.Add(modifications);
        }
    }

    #endregion

    #region GFF List Loading

    private void LoadGFFList()
    {
        var gffListSection = GetSectionName("GFFList");
        if (gffListSection == null)
        {
            _log.AddNote("[GFFList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [GFFList] patches from ini...");
        var gffList = _ini[gffListSection];

        var defaultDestination = GetValue(gffList, "!DefaultDestination") ?? ModificationsGFF.DefaultDestination;
        var defaultSourceFolder = GetValue(gffList, "!DefaultSourceFolder") ?? ".";

        foreach (var entry in gffList.Where(k =>
            !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var identifier = entry.KeyName;
            var file = entry.Value;

            var fileSectionName = GetSectionName(file);
            if (fileSectionName == null)
            {
                throw new KeyNotFoundException(
                    string.Format(SectionNotFoundError, file) +
                    string.Format(ReferencesTracebackMsg, identifier, file, gffListSection));
            }

            var replace = identifier.StartsWith("replace", StringComparison.OrdinalIgnoreCase);
            var modifications = new ModificationsGFF(file, replace)
            {
                Destination = defaultDestination,
                SourceFolder = defaultSourceFolder
            };

            var fileSectionData = _ini[fileSectionName];

            // Override with section-specific settings
            modifications.Destination = GetValue(fileSectionData, "!Destination") ?? modifications.Destination;
            modifications.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? modifications.SourceFolder;

            foreach (var modEntry in fileSectionData.Where(k =>
                !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
            {
                var key = modEntry.KeyName;
                var value = modEntry.Value;

                // TODO: Parse GFF modifications
                // This requires implementing ModifyFieldGFF, AddFieldGFF parsing
            }

            Config.PatchesGFF.Add(modifications);
        }
    }

    #endregion

    #region Compile List Loading

    private void LoadCompileList()
    {
        var compileListSection = GetSectionName("CompileList");
        if (compileListSection == null)
        {
            _log.AddNote("[CompileList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [CompileList] patches from ini...");
        var compileList = _ini[compileListSection];

        var defaultDestination = GetValue(compileList, "!DefaultDestination") ?? ModificationsNSS.DefaultDestination;
        var defaultSourceFolder = GetValue(compileList, "!DefaultSourceFolder") ?? ".";

        var nwnnsscompExePath = Path.Combine(_modPath, defaultSourceFolder, "nwnnsscomp.exe");
        if (!File.Exists(nwnnsscompExePath))
        {
            nwnnsscompExePath = _tslPatchDataPath != null
                ? Path.Combine(_tslPatchDataPath, "nwnnsscomp.exe")
                : null;
        }

        foreach (var entry in compileList.Where(k =>
            !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var identifier = entry.KeyName;
            var file = entry.Value;

            var replace = identifier.StartsWith("replace", StringComparison.OrdinalIgnoreCase);
            var modifications = new ModificationsNSS(file, replace)
            {
                Destination = defaultDestination,
                SourceFolder = defaultSourceFolder,
                NwnnsscompPath = nwnnsscompExePath
            };

            var optionalFileSection = GetSectionName(file);
            if (optionalFileSection != null)
            {
                var fileSectionData = _ini[optionalFileSection];
                modifications.Destination = GetValue(fileSectionData, "!Destination") ?? modifications.Destination;
                modifications.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? modifications.SourceFolder;
            }

            Config.PatchesNSS.Add(modifications);
        }
    }

    #endregion

    #region Hack List Loading

    private void LoadHackList()
    {
        var hackListSection = GetSectionName("HACKList");
        if (hackListSection == null)
        {
            _log.AddNote("[HACKList] section missing from ini.");
            return;
        }

        _log.AddNote("Loading [HACKList] patches from ini...");
        var hackList = _ini[hackListSection];

        var defaultDestination = GetValue(hackList, "!DefaultDestination") ?? "Override";
        var defaultSourceFolder = GetValue(hackList, "!DefaultSourceFolder") ?? ".";

        foreach (var entry in hackList.Where(k =>
            !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
        {
            var identifier = entry.KeyName;
            var file = entry.Value;

            var replace = identifier.StartsWith("replace", StringComparison.OrdinalIgnoreCase);
            var modifications = new ModificationsNCS(file, replace)
            {
                Destination = defaultDestination,
                SourceFolder = defaultSourceFolder
            };

            var fileSection = GetSectionName(file);
            if (fileSection == null)
            {
                throw new KeyNotFoundException(
                    string.Format(SectionNotFoundError, file) +
                    string.Format(ReferencesTracebackMsg, identifier, file, hackListSection));
            }

            var fileSectionData = _ini[fileSection];

            // Override with section-specific settings
            modifications.Destination = GetValue(fileSectionData, "!Destination") ?? modifications.Destination;
            modifications.SourceFolder = GetValue(fileSectionData, "!SourceFolder") ?? modifications.SourceFolder;

            foreach (var hackEntry in fileSectionData.Where(k =>
                !k.KeyName.StartsWith("!", StringComparison.Ordinal)))
            {
                var offsetStr = hackEntry.KeyName;
                var valueStr = hackEntry.Value;

                // Parse offset
                int offset = offsetStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToInt32(offsetStr, 16)
                    : int.Parse(offsetStr);

                // Parse type and value
                string typeSpecifier = "u16";
                if (valueStr.Contains(':'))
                {
                    var parts = valueStr.Split(':', 2);
                    typeSpecifier = parts[0];
                    valueStr = parts[1];
                }

                var lowerValue = valueStr.ToLower();

                if (lowerValue.StartsWith("strref"))
                {
                    var value = valueStr.Substring(6).Trim().StartsWith("0x")
                        ? Convert.ToInt32(valueStr.Substring(6).Trim(), 16)
                        : int.Parse(valueStr.Substring(6).Trim());
                    modifications.HackData.Add(("StrRef", offset, value));
                }
                else if (lowerValue.StartsWith("2damemory"))
                {
                    var value = valueStr.Substring(9).Trim().StartsWith("0x")
                        ? Convert.ToInt32(valueStr.Substring(9).Trim(), 16)
                        : int.Parse(valueStr.Substring(9).Trim());
                    modifications.HackData.Add(("2DAMEMORY", offset, value));
                }
                else if (typeSpecifier == "u8")
                {
                    var value = valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(valueStr, 16)
                        : int.Parse(valueStr);
                    modifications.HackData.Add(("UINT8", offset, value));
                }
                else if (typeSpecifier == "u32")
                {
                    var value = valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(valueStr, 16)
                        : int.Parse(valueStr);
                    modifications.HackData.Add(("UINT32", offset, value));
                }
                else
                {
                    var value = valueStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? Convert.ToInt32(valueStr, 16)
                        : int.Parse(valueStr);
                    modifications.HackData.Add(("UINT16", offset, value));
                }
            }

            Config.PatchesNCS.Add(modifications);
        }
    }

    #endregion

    #region 2DA Helper Methods

    private Modify2DA? Discern2DA(string key, string identifier, KeyDataCollection modifiers)
    {
        var lowercaseKey = key.ToLower();

        if (lowercaseKey.StartsWith("changerow"))
        {
            var target = Target2DA(identifier, modifiers);
            if (target == null) return null;

            var (cells, store2da, storeTlk) = Cells2DA(identifier, modifiers);
            return new ChangeRow2DA(identifier, target, cells, store2da, storeTlk);
        }

        if (lowercaseKey.StartsWith("addrow"))
        {
            var exclusiveColumn = GetValue(modifiers, "ExclusiveColumn");
            var rowLabel = RowLabel2DA(identifier, modifiers);
            var (cells, store2da, storeTlk) = Cells2DA(identifier, modifiers);
            return new AddRow2DA(identifier, exclusiveColumn, rowLabel, cells, store2da, storeTlk);
        }

        if (lowercaseKey.StartsWith("copyrow"))
        {
            var target = Target2DA(identifier, modifiers);
            if (target == null) return null;

            var exclusiveColumn = GetValue(modifiers, "ExclusiveColumn");
            var rowLabel = RowLabel2DA(identifier, modifiers);
            var (cells, store2da, storeTlk) = Cells2DA(identifier, modifiers);
            return new CopyRow2DA(identifier, target, exclusiveColumn, rowLabel, cells, store2da, storeTlk);
        }

        if (lowercaseKey.StartsWith("addcolumn"))
        {
            return ReadAddColumn(modifiers, identifier);
        }

        throw new KeyNotFoundException(
            $"Could not parse key '{key}={identifier}', expecting one of ['ChangeRow=', 'AddColumn=', 'AddRow=', 'CopyRow=']");
    }

    private Target? Target2DA(string identifier, KeyDataCollection modifiers)
    {
        // Check for RowIndex
        var rowIndexValue = GetValue(modifiers, "RowIndex");
        if (rowIndexValue != null)
        {
            return ParseTarget(TargetType.ROW_INDEX, rowIndexValue, true);
        }

        // Check for RowLabel
        var rowLabelValue = GetValue(modifiers, "RowLabel");
        if (rowLabelValue != null)
        {
            return ParseTarget(TargetType.ROW_LABEL, rowLabelValue, false);
        }

        // Check for LabelIndex
        var labelIndexValue = GetValue(modifiers, "LabelIndex");
        if (labelIndexValue != null)
        {
            return ParseTarget(TargetType.LABEL_COLUMN, labelIndexValue, false);
        }

        _log.AddWarning($"No line set to be modified in [{identifier}].");
        return null;
    }

    private Target ParseTarget(TargetType targetType, string rawValue, bool isInt)
    {
        var lowerRawValue = rawValue.ToLower();

        if (lowerRawValue.StartsWith("strref") && rawValue.Length > 6 && rawValue.Substring(6).All(char.IsDigit))
        {
            var tokenId = int.Parse(rawValue.Substring(6));
            return new Target(targetType, new RowValueTLKMemory(tokenId));
        }

        if (lowerRawValue.StartsWith("2damemory") && rawValue.Length > 9 && rawValue.Substring(9).All(char.IsDigit))
        {
            var tokenId = int.Parse(rawValue.Substring(9));
            return new Target(targetType, new RowValue2DAMemory(tokenId));
        }

        if (isInt)
        {
            return new Target(targetType, int.Parse(rawValue));
        }

        return new Target(targetType, rawValue);
    }

    private (Dictionary<string, RowValue>, Dictionary<int, RowValue>, Dictionary<int, RowValue>) Cells2DA(
        string identifier,
        KeyDataCollection modifiers)
    {
        var cells = new Dictionary<string, RowValue>();
        var store2da = new Dictionary<int, RowValue>();
        var storeTlk = new Dictionary<int, RowValue>();

        foreach (var modifier in modifiers)
        {
            var modifierKey = modifier.KeyName;
            var value = modifier.Value;
            var lowerModifier = modifierKey.ToLower().Trim();
            var lowerValue = value.ToLower();

            bool isStore2da = lowerModifier.StartsWith("2damemory") &&
                              modifierKey.Length > 9 &&
                              modifierKey.Substring(9).All(char.IsDigit);

            bool isStoreTlk = modifierKey.StartsWith("strref", StringComparison.OrdinalIgnoreCase) &&
                              modifierKey.Length > 6 &&
                              modifierKey.Substring(6).All(char.IsDigit);

            bool isRowLabel = lowerModifier == "rowlabel" || lowerModifier == "newrowlabel";

            RowValue? rowValue = null;

            if (lowerValue.StartsWith("2damemory"))
            {
                var tokenId = int.Parse(value.Substring(9));
                rowValue = new RowValue2DAMemory(tokenId);
            }
            else if (lowerValue.StartsWith("strref"))
            {
                var tokenId = int.Parse(value.Substring(6));
                rowValue = new RowValueTLKMemory(tokenId);
            }
            else if (lowerValue == "high()")
            {
                rowValue = modifierKey == "rowlabel" ? new RowValueHigh(null) : new RowValueHigh(modifierKey);
            }
            else if (lowerValue == "rowindex")
            {
                rowValue = new RowValueRowIndex();
            }
            else if (lowerValue == "rowlabel")
            {
                rowValue = new RowValueRowLabel();
            }
            else if (isStore2da || isStoreTlk)
            {
                rowValue = new RowValueRowCell(value);
            }
            else if (value == "****")
            {
                rowValue = new RowValueConstant(string.Empty);
            }
            else
            {
                rowValue = new RowValueConstant(value);
            }

            if (isStore2da)
            {
                var tokenId = int.Parse(modifierKey.Substring(9));
                store2da[tokenId] = rowValue;
            }
            else if (isStoreTlk)
            {
                var tokenId = int.Parse(modifierKey.Substring(6));
                storeTlk[tokenId] = rowValue;
            }
            else if (!isRowLabel)
            {
                cells[modifierKey] = rowValue;
            }
        }

        return (cells, store2da, storeTlk);
    }

    private string? RowLabel2DA(string identifier, KeyDataCollection modifiers)
    {
        return GetValue(modifiers, "RowLabel") ?? GetValue(modifiers, "NewRowLabel");
    }

    private AddColumn2DA ReadAddColumn(KeyDataCollection modifiers, string identifier)
    {
        var header = GetValue(modifiers, "ColumnLabel");
        if (header == null)
        {
            throw new KeyNotFoundException($"Missing 'ColumnLabel' in [{identifier}]");
        }

        var defaultValue = GetValue(modifiers, "DefaultValue");
        if (defaultValue == null)
        {
            throw new KeyNotFoundException($"Missing 'DefaultValue' in [{identifier}]");
        }

        defaultValue = defaultValue == "****" ? string.Empty : defaultValue;

        var (indexInsert, labelInsert, store2da) = ColumnInserts2DA(identifier, modifiers);

        return new AddColumn2DA(identifier, header, defaultValue, indexInsert, labelInsert, store2da);
    }

    private (Dictionary<int, RowValue>, Dictionary<string, RowValue>, Dictionary<int, string>) ColumnInserts2DA(
        string identifier,
        KeyDataCollection modifiers)
    {
        var indexInsert = new Dictionary<int, RowValue>();
        var labelInsert = new Dictionary<string, RowValue>();
        var store2da = new Dictionary<int, string>();

        foreach (var modifier in modifiers)
        {
            var modifierKey = modifier.KeyName;
            var value = modifier.Value;
            var modifierLowercase = modifierKey.ToLower();
            var valueLowercase = value.ToLower();

            bool isStore2da = valueLowercase.StartsWith("2damemory");
            bool isStoreTlk = valueLowercase.StartsWith("strref");

            RowValue? rowValue = null;

            if (isStore2da)
            {
                var tokenId = int.Parse(value.Substring(9));
                rowValue = new RowValue2DAMemory(tokenId);
            }
            else if (isStoreTlk)
            {
                var tokenId = int.Parse(value.Substring(6));
                rowValue = new RowValueTLKMemory(tokenId);
            }
            else
            {
                rowValue = new RowValueConstant(value);
            }

            if (modifierLowercase.StartsWith("i"))
            {
                var index = int.Parse(modifierKey.Substring(1));
                indexInsert[index] = rowValue;
            }
            else if (modifierLowercase.StartsWith("l"))
            {
                var label = modifierKey.Substring(1);
                labelInsert[label] = rowValue;
            }
            else if (modifierLowercase.StartsWith("2damemory"))
            {
                var tokenId = int.Parse(modifierKey.Substring(9));
                store2da[tokenId] = value;
            }
        }

        return (indexInsert, labelInsert, store2da);
    }

    #endregion

    #region Helper Methods

    private static string? GetValue(KeyDataCollection section, string key)
    {
        return section.FirstOrDefault(k =>
            k.KeyName.Equals(key, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private static string NormalizeTslPatcherFloat(string valueStr)
    {
        return valueStr.Replace(",", ".");
    }

    private static string NormalizeTslPatcherCRLF(string valueStr)
    {
        return valueStr.Replace("<#LF#>", "\n").Replace("<#CR#>", "\r");
    }

    #endregion
}
