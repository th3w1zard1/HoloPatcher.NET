using System;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods;

/// <summary>
/// Represents a file to be installed/copied during patching.
/// </summary>
public class InstallFile : PatcherModification
{
    public InstallFile(string filename, bool replaceExisting = false)
        : base(filename, replaceExisting)
    {
        Action = "Copy ";
        SkipIfNotReplace = true;
    }

    public override object PatchResource(
        byte[] source,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        Apply(source, memory, logger, game);
        return source;
    }

    public override void Apply(
        object mutableData,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        // InstallFile doesn't modify the file, just copies it
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Destination, SaveAs, ReplaceFile);
    }

    public override string ToString()
    {
        return $"{Action}{SourceFile} -> {Destination}/{SaveAs}";
    }
}

