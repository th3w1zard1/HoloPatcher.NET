using System.Collections.Generic;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Formats.TLK;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods.TLK;

/// <summary>
/// Container for TLK (talk table) modifications.
/// 1:1 port from Python ModificationsTLK in pykotor/tslpatcher/mods/tlk.py
/// </summary>
public class ModificationsTLK : PatcherModification
{
    public new const string DEFAULT_DESTINATION = ".";
    public const string DEFAULT_SOURCEFILE = "append.tlk";
    public const string DEFAULT_SOURCEFILE_F = "appendf.tlk";
    public const string DEFAULT_SAVEAS_FILE = "dialog.tlk";
    public const string DEFAULT_SAVEAS_FILE_F = "dialogf.tlk";

    public List<ModifyTLK> Modifiers { get; set; } = new();
    public string SourcefileF { get; set; } = DEFAULT_SOURCEFILE_F;
    public new string SaveAs { get; set; } = DEFAULT_SAVEAS_FILE;

    public ModificationsTLK(string filename = DEFAULT_SOURCEFILE, bool replaceFile = false, List<ModifyTLK>? modifiers = null)
        : base(filename, replaceFile)
    {
        Destination = DEFAULT_DESTINATION;
        Modifiers = modifiers ?? new List<ModifyTLK>();
    }

    public override object PatchResource(
        byte[] source,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        var reader = new TLKBinaryReader(source);
        var dialog = reader.Load();
        Apply(dialog, memory, logger, game);

        var writer = new TLKBinaryWriter(dialog);
        return writer.Write();
    }

    public override void Apply(
        object mutableData,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        if (mutableData is Formats.TLK.TLK dialog)
        {
            foreach (var modifier in Modifiers)
            {
                modifier.Apply(dialog, memory);
                logger.CompletePatch();
            }
        }
        else
        {
            logger.AddError($"Expected TLK object for ModificationsTLK, but got {mutableData.GetType().Name}");
        }
    }
}

/// <summary>
/// Represents a single TLK string modification.
/// 1:1 port from Python ModifyTLK in pykotor/tslpatcher/mods/tlk.py
/// </summary>
public class ModifyTLK
{
    public string? TlkFilepath { get; set; }
    public string Text { get; set; } = "";
    public ResRef Sound { get; set; } = ResRef.FromBlank();

    public int ModIndex { get; set; }
    public int TokenId { get; set; }
    public bool IsReplacement { get; set; }

    public ModifyTLK(int tokenId, bool isReplacement = false)
    {
        TokenId = tokenId;
        ModIndex = tokenId;
        IsReplacement = isReplacement;
    }

    public void Apply(Formats.TLK.TLK dialog, PatcherMemory memory)
    {
        Load();
        if (IsReplacement)
        {
            dialog.Replace(TokenId, Text, Sound.ToString());
            memory.MemoryStr[TokenId] = TokenId;
        }
        else
        {
            int stringref = dialog.Add(Text, Sound.ToString());
            memory.MemoryStr[TokenId] = stringref;
        }
    }

    private void Load()
    {
        if (string.IsNullOrEmpty(TlkFilepath))
        {
            return;
        }

        // TODO: Implement TalkTable lookup if TlkFilepath is specified
        // This would require implementing the TalkTable class
        // For now, just use the text and sound as-is
    }
}
