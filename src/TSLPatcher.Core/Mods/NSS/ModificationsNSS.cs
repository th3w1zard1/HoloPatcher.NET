using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
public class ModificationsNSS : PatcherModification
{
    public new string Action { get; set; } = "Compile";
    public new bool SkipIfNotReplace { get; set; } = true;

    public ModificationsNSS(string filename, bool replaceFile = false)
        : base(filename, replaceFile)
    {
        SaveAs = Path.ChangeExtension(filename, ".ncs");
    }

    public override object PatchResource(
        byte[] source,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
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
        int game)
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

/// <summary>
/// Container for NCS (compiled script) binary modifications.
/// 1:1 port from Python ModificationsNCS in pykotor/tslpatcher/mods/ncs.py
/// </summary>
public class ModificationsNCS : PatcherModification
{
    public new string Action { get; set; } = "Hack ";
    public List<(string TokenType, int Offset, int TokenIdOrValue)> HackData { get; set; } = new();

    public ModificationsNCS(string filename, bool replaceFile = false)
        : base(filename, replaceFile)
    {
    }

    public override object PatchResource(
        byte[] source,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        byte[] ncsArray = new byte[source.Length];
        Array.Copy(source, ncsArray, source.Length);
        Apply(ncsArray, memory, logger, game);
        return ncsArray;
    }

    public override void Apply(
        object mutableData,
        PatcherMemory memory,
        PatchLogger logger,
        int game)
    {
        if (mutableData is byte[] ncsArray)
        {
            using var ms = new MemoryStream(ncsArray);
            using var writer = new BinaryWriter(ms);

            foreach (var (tokenType, offset, tokenIdOrValue) in HackData)
            {
                logger.AddVerbose($"HACKList {SourceFile}: seeking to offset {offset:X}");
                writer.Seek(offset, SeekOrigin.Begin);

                int value;
                string tokenTypeLower = tokenType.ToLower();

                if (tokenTypeLower == "strref")
                {
                    if (!memory.MemoryStr.ContainsKey(tokenIdOrValue))
                    {
                        throw new KeyError($"StrRef{tokenIdOrValue} was not defined before use");
                    }
                    value = memory.MemoryStr[tokenIdOrValue];
                    logger.AddVerbose($"HACKList {SourceFile}: writing unsigned WORD {value} at offset {offset:X}");
                    WriteUInt16BigEndian(writer, (ushort)value);
                }
                else if (tokenTypeLower == "2damemory")
                {
                    if (!memory.Memory2DA.ContainsKey(tokenIdOrValue))
                    {
                        throw new KeyError($"2DAMEMORY{tokenIdOrValue} was not defined before use");
                    }
                    value = int.Parse(memory.Memory2DA[tokenIdOrValue].ToString()!);
                    logger.AddVerbose($"HACKList {SourceFile}: writing unsigned WORD {value} at offset {offset:X}");
                    WriteUInt16BigEndian(writer, (ushort)value);
                }
                else
                {
                    value = tokenIdOrValue;
                    if (tokenTypeLower == "uint32")
                    {
                        logger.AddVerbose($"HACKList {SourceFile}: writing unsigned DWORD (32-bit) {value} at offset {offset:X}");
                        WriteUInt32BigEndian(writer, (uint)value);
                    }
                    else if (tokenTypeLower == "uint16")
                    {
                        logger.AddVerbose($"HACKList {SourceFile}: writing unsigned WORD (16-bit) {value} at offset {offset:X}");
                        WriteUInt16BigEndian(writer, (ushort)value);
                    }
                    else if (tokenTypeLower == "uint8")
                    {
                        logger.AddVerbose($"HACKList {SourceFile}: writing unsigned BYTE (8-bit) {value} at offset {offset:X}");
                        writer.Write((byte)value);
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown token type '{tokenType}' in HACKList patch");
                    }
                }
            }
        }
        else
        {
            logger.AddError($"Expected byte array for ModificationsNCS, but got {mutableData.GetType().Name}");
        }
    }

    private static void WriteUInt16BigEndian(BinaryWriter writer, ushort value)
    {
        writer.Write((byte)(value >> 8));
        writer.Write((byte)(value & 0xFF));
    }

    private static void WriteUInt32BigEndian(BinaryWriter writer, uint value)
    {
        writer.Write((byte)(value >> 24));
        writer.Write((byte)((value >> 16) & 0xFF));
        writer.Write((byte)((value >> 8) & 0xFF));
        writer.Write((byte)(value & 0xFF));
    }
}

/// <summary>
/// Exception thrown when a required key is not found in memory.
/// </summary>
public class KeyError(string message) : KeyNotFoundException(message)
{
}
