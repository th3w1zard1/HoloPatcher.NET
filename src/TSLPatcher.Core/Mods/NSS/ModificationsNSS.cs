using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods.NSS;

/// <summary>
/// Mutable string wrapper for token replacement in NSS files.
/// 1:1 port from Python MutableString in pykotor/tslpatcher/mods/nss.py
/// </summary>
public class MutableString
{
    public string Value { get; set; }

    public MutableString(string value)
    {
        Value = value;
    }

    public override string ToString() => Value;
}

/// <summary>
/// Container for NSS (script source) modifications.
/// 1:1 port from Python ModificationsNSS in pykotor/tslpatcher/mods/nss.py
/// </summary>
public class ModificationsNSS : PatcherModifications
{
    public new const string DEFAULT_DESTINATION = "Override";
    public static string DefaultDestination => DEFAULT_DESTINATION;

    public new string Action { get; set; } = "Compile";
    public new bool SkipIfNotReplace { get; set; } = true;
    public string? NwnnsscompPath { get; set; }

    public ModificationsNSS(string filename, bool replaceFile = false)
        : base(filename, replaceFile)
    {
        SaveAs = Path.ChangeExtension(filename, ".ncs");
    }

    public override object PatchResource(
        byte[] source,
        PatcherMemory memory,
        PatchLogger logger,
        Game game)
    {
        if (source == null)
        {
            logger.AddError("Invalid nss source provided to ModificationsNSS.PatchResource()");
            return true;
        }

        // Decode the NSS source bytes
        string sourceText = Encoding.GetEncoding("windows-1252").GetString(source);
        var mutableSource = new MutableString(sourceText);
        Apply(mutableSource, memory, logger, game);

        // NOTE: In Python, this would compile the script. For now, we just return the modified source
        // TODO: Implement NCS compilation if needed
        logger.AddNote($"NSS compilation not yet implemented. Returning modified source for '{SourceFile}'");
        return Encoding.GetEncoding("windows-1252").GetBytes(mutableSource.Value);
    }

    public override void Apply(
        object mutableData,
        PatcherMemory memory,
        PatchLogger logger,
        Game game)
    {
        if (mutableData is MutableString nssSource)
        {
            IterateAndReplaceTokens2DA("2DAMEMORY", memory.Memory2DA, nssSource, logger);
            IterateAndReplaceTokensStr("StrRef", memory.MemoryStr, nssSource, logger);
        }
        else
        {
            logger.AddError($"Expected MutableString for ModificationsNSS, but got {mutableData.GetType().Name}");
        }
    }

    private void IterateAndReplaceTokens2DA(string tokenName, Dictionary<int, string> memoryDict, MutableString nssSource, PatchLogger logger)
    {
        var searchPattern = $@"#{tokenName}\d+#";
        var match = Regex.Match(nssSource.Value, searchPattern);

        while (match.Success)
        {
            int start = match.Index;
            int end = start + match.Length;

            // Extract the token ID from the match (e.g., #2DAMEMORY5# -> 5)
            string tokenIdStr = nssSource.Value.Substring(start + tokenName.Length + 1, end - start - tokenName.Length - 2);
            int tokenId = int.Parse(tokenIdStr);

            if (!memoryDict.ContainsKey(tokenId))
            {
                throw new KeyError($"{tokenName}{tokenId} was not defined before use in '{SourceFile}'");
            }

            string replacementValue = memoryDict[tokenId];
            logger.AddVerbose($"{SourceFile}: Replacing '#{tokenName}{tokenId}#' with '{replacementValue}'");
            nssSource.Value = nssSource.Value.Substring(0, start) + replacementValue + nssSource.Value.Substring(end);

            match = Regex.Match(nssSource.Value, searchPattern);
        }
    }

    private void IterateAndReplaceTokensStr(string tokenName, Dictionary<int, int> memoryDict, MutableString nssSource, PatchLogger logger)
    {
        var searchPattern = $@"#{tokenName}\d+#";
        var match = Regex.Match(nssSource.Value, searchPattern);

        while (match.Success)
        {
            int start = match.Index;
            int end = start + match.Length;

            // Extract the token ID from the match (e.g., #2DAMEMORY5# -> 5)
            string tokenIdStr = nssSource.Value.Substring(start + tokenName.Length + 1, end - start - tokenName.Length - 2);
            int tokenId = int.Parse(tokenIdStr);

            if (!memoryDict.ContainsKey(tokenId))
            {
                throw new KeyError($"{tokenName}{tokenId} was not defined before use in '{SourceFile}'");
            }

            int replacementValue = memoryDict[tokenId];
            logger.AddVerbose($"{SourceFile}: Replacing '#{tokenName}{tokenId}#' with '{replacementValue}'");
            nssSource.Value = nssSource.Value.Substring(0, start) + replacementValue.ToString() + nssSource.Value.Substring(end);

            match = Regex.Match(nssSource.Value, searchPattern);
        }
    }
}
