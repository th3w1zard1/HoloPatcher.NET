using System.Collections.Generic;
using TSLPatcher.Core.Mods;
using TSLPatcher.Core.Mods.TwoDA;
using TSLPatcher.Core.Mods.GFF;
using TSLPatcher.Core.Mods.SSF;
using TSLPatcher.Core.Mods.TLK;
using TSLPatcher.Core.Mods.NSS;

namespace TSLPatcher.Core.Config;

/// <summary>
/// Configuration for TSLPatcher operations
/// </summary>
public class PatcherConfig
{
    public string WindowTitle { get; set; } = string.Empty;
    public string ConfirmMessage { get; set; } = string.Empty;
    public int? GameNumber { get; set; }

    public List<string[]> RequiredFiles { get; set; } = new();
    public List<string> RequiredMessages { get; set; } = new();
    public int SaveProcessedScripts { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Warnings;

    // Optional HoloPatcher features
    public bool IgnoreFileExtensions { get; set; }

    // Patch lists
    public List<InstallFile> InstallList { get; set; } = new();
    public List<Modifications2DA> Patches2DA { get; set; } = new();
    public List<ModificationsGFF> PatchesGFF { get; set; } = new();
    public List<ModificationsSSF> PatchesSSF { get; set; } = new();
    public List<ModificationsNSS> PatchesNSS { get; set; } = new();
    public List<ModificationsNCS> PatchesNCS { get; set; } = new();
    public ModificationsTLK PatchesTLK { get; set; } = new();

    public int PatchCount()
    {
        return InstallList.Count +
               Patches2DA.Count +
               PatchesGFF.Count +
               PatchesSSF.Count +
               PatchesNSS.Count +
               PatchesNCS.Count;
    }
}

