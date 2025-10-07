using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Formats.ERF;
using TSLPatcher.Core.Formats.RIM;
using TSLPatcher.Core.Resources;

namespace TSLPatcher.Core.Formats.Capsule;

/// <summary>
/// Represents a capsule file (ERF, RIM, MOD, or SAV) that contains multiple resources.
/// This is the main class for accessing game archive files.
/// </summary>
public class Capsule : IEnumerable<CapsuleResource>
{
    private readonly List<CapsuleResource> _resources = new();
    private readonly CaseAwarePath _path;
    private CapsuleType _capsuleType;

    public CaseAwarePath Path => _path;
    public CapsuleType Type => _capsuleType;
    public int Count => _resources.Count;

    public Capsule(string path, bool createIfNotExist = false)
    {
        _path = new CaseAwarePath(path);
        _capsuleType = DetermineCapsuleType(_path.Extension);

        if (createIfNotExist && !_path.IsFile())
        {
            CreateEmpty();
        }
        else if (_path.IsFile())
        {
            Reload();
        }
    }

    private static CapsuleType DetermineCapsuleType(string extension)
    {
        var ext = extension.TrimStart('.').ToLowerInvariant();
        return ext switch
        {
            "rim" => CapsuleType.RIM,
            "erf" => CapsuleType.ERF,
            "mod" => CapsuleType.MOD,
            "sav" => CapsuleType.SAV,
            _ => throw new ArgumentException($"Unknown capsule type: {extension}")
        };
    }

    private void CreateEmpty()
    {
        if (_capsuleType == CapsuleType.RIM)
        {
            // Write empty RIM
            using var writer = new BinaryWriter(File.Create(_path.GetResolvedPath()));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIM "));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("V1.0"));
            writer.Write(0); // reserved
            writer.Write(0); // entry count
            writer.Write(120); // offset to keys (header size)
        }
        else
        {
            // Write empty ERF
            using var writer = new BinaryWriter(File.Create(_path.GetResolvedPath()));
            var fourCC = _capsuleType == CapsuleType.ERF ? "ERF " : "MOD ";
            writer.Write(System.Text.Encoding.ASCII.GetBytes(fourCC));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("V1.0"));
            writer.Write(0); // language count
            writer.Write(0); // localized string size
            writer.Write(0); // entry count
            writer.Write(0); // offset to localized strings
            writer.Write(160); // offset to keys
            writer.Write(160); // offset to resources
            writer.Write(0); // build year
            writer.Write(0); // build day
            writer.Write(0xFFFFFFFF); // description strref
            // Pad to 160 bytes
            for (int i = 0; i < 116; i++)
                writer.Write((byte)0);
        }
    }

    public void Reload()
    {
        _resources.Clear();

        if (!_path.IsFile())
            return;

        using var reader = new BinaryReader(File.OpenRead(_path.GetResolvedPath()));

        var fileType = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
        var fileVersion = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));

        if (fileVersion != "V1.0")
            throw new InvalidDataException($"Unsupported capsule version: {fileVersion}");

        if (fileType == "RIM ")
        {
            LoadRIM(reader);
        }
        else if (fileType == "ERF " || fileType == "MOD ")
        {
            LoadERF(reader);
        }
        else
        {
            throw new InvalidDataException($"Unknown capsule file type: {fileType}");
        }
    }

    private void LoadRIM(BinaryReader reader)
    {
        reader.BaseStream.Seek(8, SeekOrigin.Begin); // Skip header
        reader.ReadInt32(); // reserved
        uint entryCount = reader.ReadUInt32();
        uint offsetToKeys = reader.ReadUInt32();

        var entries = new List<(string resref, uint restype, uint resid, uint offset, uint size)>();

        reader.BaseStream.Seek(offsetToKeys, SeekOrigin.Begin);
        for (uint i = 0; i < entryCount; i++)
        {
            var resref = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(16)).TrimEnd('\0');
            var restype = reader.ReadUInt32();
            var resid = reader.ReadUInt32();
            var offset = reader.ReadUInt32();
            var size = reader.ReadUInt32();
            entries.Add((resref, restype, resid, offset, size));
        }

        foreach (var entry in entries)
        {
            reader.BaseStream.Seek(entry.offset, SeekOrigin.Begin);
            var data = reader.ReadBytes((int)entry.size);
            var resType = ResourceType.FromId((int)entry.restype);
            _resources.Add(new CapsuleResource(entry.resref, resType, data, (int)entry.size, (int)entry.offset, _path.ToString()));
        }
    }

    private void LoadERF(BinaryReader reader)
    {
        reader.BaseStream.Seek(8, SeekOrigin.Begin); // Skip header already read
        reader.ReadUInt32(); // language count
        reader.ReadUInt32(); // localized string size
        uint entryCount = reader.ReadUInt32();
        uint offsetToLocalizedStrings = reader.ReadUInt32();
        uint offsetToKeys = reader.ReadUInt32();
        uint offsetToResources = reader.ReadUInt32();

        var resrefs = new List<string>();
        var resids = new List<uint>();
        var restypes = new List<ushort>();

        reader.BaseStream.Seek(offsetToKeys, SeekOrigin.Begin);
        for (uint i = 0; i < entryCount; i++)
        {
            var resref = System.Text.Encoding.ASCII.GetString(reader.ReadBytes(16)).TrimEnd('\0');
            var resid = reader.ReadUInt32();
            var restype = reader.ReadUInt16();
            reader.ReadUInt16(); // unused
            resrefs.Add(resref);
            resids.Add(resid);
            restypes.Add(restype);
        }

        var resoffsets = new List<uint>();
        var ressizes = new List<uint>();

        reader.BaseStream.Seek(offsetToResources, SeekOrigin.Begin);
        for (uint i = 0; i < entryCount; i++)
        {
            resoffsets.Add(reader.ReadUInt32());
            ressizes.Add(reader.ReadUInt32());
        }

        for (int i = 0; i < entryCount; i++)
        {
            reader.BaseStream.Seek(resoffsets[i], SeekOrigin.Begin);
            var data = reader.ReadBytes((int)ressizes[i]);
            var resType = ResourceType.FromId(restypes[i]);
            _resources.Add(new CapsuleResource(resrefs[i], resType, data, (int)ressizes[i], (int)resoffsets[i], _path.ToString()));
        }
    }

    public byte[]? GetResource(string resname, ResourceType restype)
    {
        var resource = _resources.FirstOrDefault(r =>
            string.Equals(r.ResName, resname, StringComparison.OrdinalIgnoreCase) &&
            r.ResType == restype);
        return resource?.Data;
    }

    public void SetResource(string resname, ResourceType restype, byte[] data)
    {
        var existing = _resources.FirstOrDefault(r =>
            string.Equals(r.ResName, resname, StringComparison.OrdinalIgnoreCase) &&
            r.ResType == restype);

        if (existing != null)
        {
            _resources.Remove(existing);
        }

        _resources.Add(new CapsuleResource(resname, restype, data, data.Length, 0, _path.ToString()));
    }

    public bool Contains(string resname, ResourceType restype)
    {
        return _resources.Any(r =>
            string.Equals(r.ResName, resname, StringComparison.OrdinalIgnoreCase) &&
            r.ResType == restype);
    }

    public List<CapsuleResource> GetResources() => new(_resources);

    public IEnumerator<CapsuleResource> GetEnumerator() => _resources.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>
/// Type of capsule file.
/// </summary>
public enum CapsuleType
{
    ERF,
    RIM,
    MOD,
    SAV
}

/// <summary>
/// Represents a resource within a capsule.
/// </summary>
public class CapsuleResource
{
    public string ResName { get; }
    public ResourceType ResType { get; }
    public byte[] Data { get; }
    public int Size { get; }
    public int Offset { get; }
    public string FilePath { get; }

    public CapsuleResource(string resname, ResourceType restype, byte[] data, int size, int offset, string filepath)
    {
        ResName = resname;
        ResType = restype;
        Data = data;
        Size = size;
        Offset = offset;
        FilePath = filepath;
    }

    public ResourceIdentifier Identifier => new(ResName, ResType);

    public override string ToString() => $"{ResName}.{ResType.Extension}";
}

