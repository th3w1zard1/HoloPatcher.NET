using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TSLPatcher.Core.Common;

namespace TSLPatcher.Core.Formats.GFF;

/// <summary>
/// Reads GFF (General File Format) binary data.
/// 1:1 port of Python GFFBinaryReader from pykotor/resource/formats/gff/io_gff.py
/// </summary>
public class GFFBinaryReader
{
    private readonly byte[] _data;
    private readonly BinaryReader _reader;

    private GFF? _gff;
    private List<string> _labels = new();
    private int _fieldDataOffset;
    private int _fieldIndicesOffset;
    private int _listIndicesOffset;
    private int _structOffset;
    private int _fieldOffset;

    // Complex fields that are stored in the field data section
    private static readonly HashSet<GFFFieldType> _complexFields = new()
    {
        GFFFieldType.UInt64,
        GFFFieldType.Int64,
        GFFFieldType.Double,
        GFFFieldType.String,
        GFFFieldType.ResRef,
        GFFFieldType.LocalizedString,
        GFFFieldType.Binary,
        GFFFieldType.Vector3,
        GFFFieldType.Vector4
    };

    public GFFBinaryReader(byte[] data)
    {
        _data = data;
        _reader = new BinaryReader(new MemoryStream(data));
    }

    public GFFBinaryReader(string filepath)
    {
        _data = File.ReadAllBytes(filepath);
        _reader = new BinaryReader(new MemoryStream(_data));
    }

    public GFFBinaryReader(Stream source)
    {
        using var ms = new MemoryStream();
        source.CopyTo(ms);
        _data = ms.ToArray();
        _reader = new BinaryReader(new MemoryStream(_data));
    }

    public GFF Load()
    {
        try
        {
            _gff = new GFF();

            _reader.BaseStream.Seek(0, SeekOrigin.Begin);

            // Read header
            string fileType = Encoding.ASCII.GetString(_reader.ReadBytes(4));
            string fileVersion = Encoding.ASCII.GetString(_reader.ReadBytes(4));

            // Validate content type
            if (!IsValidGFFContent(fileType))
            {
                throw new InvalidDataException("Not a valid binary GFF file.");
            }

            if (fileVersion != "V3.2")
            {
                throw new InvalidDataException("The GFF version of the file is unsupported.");
            }

            _gff.Content = GFFContentExtensions.FromFourCC(fileType);

            _structOffset = (int)_reader.ReadUInt32();
            _reader.ReadUInt32(); // struct count (unused during reading)
            _fieldOffset = (int)_reader.ReadUInt32();
            _reader.ReadUInt32(); // field count (unused)
            int labelOffset = (int)_reader.ReadUInt32();
            int labelCount = (int)_reader.ReadUInt32();
            _fieldDataOffset = (int)_reader.ReadUInt32();
            _reader.ReadUInt32(); // field data count (unused)
            _fieldIndicesOffset = (int)_reader.ReadUInt32();
            _reader.ReadUInt32(); // field indices count (unused)
            _listIndicesOffset = (int)_reader.ReadUInt32();
            _reader.ReadUInt32(); // list indices count (unused)

            // Read labels
            _labels = new List<string>();
            _reader.BaseStream.Seek(labelOffset, SeekOrigin.Begin);
            for (int i = 0; i < labelCount; i++)
            {
                string label = Encoding.ASCII.GetString(_reader.ReadBytes(16)).TrimEnd('\0');
                _labels.Add(label);
            }

            // Load root struct
            LoadStruct(_gff.Root, 0);

            return _gff;
        }
        catch (EndOfStreamException)
        {
            throw new InvalidDataException("Invalid GFF file format - unexpected end of file.");
        }
    }

    private bool IsValidGFFContent(string fourCC)
    {
        // Check if fourCC matches any GFFContent enum value
        string trimmedFourCC = fourCC.Trim();
        return Enum.TryParse<GFFContent>(trimmedFourCC, ignoreCase: true, out _);
    }

    private void LoadStruct(GFFStruct gffStruct, int structIndex)
    {
        _reader.BaseStream.Seek(_structOffset + structIndex * 12, SeekOrigin.Begin);

        int structId = _reader.ReadInt32();
        uint data = _reader.ReadUInt32();
        uint fieldCount = _reader.ReadUInt32();

        gffStruct.StructId = structId;

        if (fieldCount == 1)
        {
            LoadField(gffStruct, (int)data);
        }
        else if (fieldCount > 1)
        {
            _reader.BaseStream.Seek(_fieldIndicesOffset + data, SeekOrigin.Begin);
            var indices = new List<int>();
            for (int i = 0; i < fieldCount; i++)
            {
                indices.Add((int)_reader.ReadUInt32());
            }

            foreach (int index in indices)
            {
                LoadField(gffStruct, index);
            }
        }
    }

    private void LoadField(GFFStruct gffStruct, int fieldIndex)
    {
        _reader.BaseStream.Seek(_fieldOffset + fieldIndex * 12, SeekOrigin.Begin);

        uint fieldTypeId = _reader.ReadUInt32();
        uint labelId = _reader.ReadUInt32();

        var fieldType = (GFFFieldType)fieldTypeId;
        string label = _labels[(int)labelId];

        if (_complexFields.Contains(fieldType))
        {
            uint offset = _reader.ReadUInt32(); // Relative to field data
            _reader.BaseStream.Seek(_fieldDataOffset + offset, SeekOrigin.Begin);

            switch (fieldType)
            {
                case GFFFieldType.UInt64:
                    gffStruct.SetUInt64(label, _reader.ReadUInt64());
                    break;
                case GFFFieldType.Int64:
                    gffStruct.SetInt64(label, _reader.ReadInt64());
                    break;
                case GFFFieldType.Double:
                    gffStruct.SetDouble(label, _reader.ReadDouble());
                    break;
                case GFFFieldType.String:
                    uint stringLength = _reader.ReadUInt32();
                    string str = Encoding.ASCII.GetString(_reader.ReadBytes((int)stringLength)).TrimEnd('\0');
                    gffStruct.SetString(label, str);
                    break;
                case GFFFieldType.ResRef:
                    byte resrefLength = _reader.ReadByte();
                    string resrefStr = Encoding.ASCII.GetString(_reader.ReadBytes(resrefLength)).Trim();
                    gffStruct.SetResRef(label, new ResRef(resrefStr));
                    break;
                case GFFFieldType.LocalizedString:
                    gffStruct.SetLocString(label, _reader.ReadLocalizedString());
                    break;
                case GFFFieldType.Binary:
                    uint binaryLength = _reader.ReadUInt32();
                    gffStruct.SetBinary(label, _reader.ReadBytes((int)binaryLength));
                    break;
                case GFFFieldType.Vector3:
                    gffStruct.SetVector3(label, _reader.ReadVector3());
                    break;
                case GFFFieldType.Vector4:
                    gffStruct.SetVector4(label, _reader.ReadVector4());
                    break;
            }
        }
        else if (fieldType == GFFFieldType.Struct)
        {
            uint structIndex = _reader.ReadUInt32();
            var newStruct = new GFFStruct();
            LoadStruct(newStruct, (int)structIndex);
            gffStruct.SetStruct(label, newStruct);
        }
        else if (fieldType == GFFFieldType.List)
        {
            LoadList(gffStruct, label);
        }
        else
        {
            // Simple types (stored inline in the field data)
            switch (fieldType)
            {
                case GFFFieldType.UInt8:
                    gffStruct.SetUInt8(label, _reader.ReadByte());
                    break;
                case GFFFieldType.Int8:
                    gffStruct.SetInt8(label, _reader.ReadSByte());
                    break;
                case GFFFieldType.UInt16:
                    gffStruct.SetUInt16(label, _reader.ReadUInt16());
                    break;
                case GFFFieldType.Int16:
                    gffStruct.SetInt16(label, _reader.ReadInt16());
                    break;
                case GFFFieldType.UInt32:
                    gffStruct.SetUInt32(label, _reader.ReadUInt32());
                    break;
                case GFFFieldType.Int32:
                    gffStruct.SetInt32(label, _reader.ReadInt32());
                    break;
                case GFFFieldType.Single:
                    gffStruct.SetSingle(label, _reader.ReadSingle());
                    break;
            }
        }
    }

    private void LoadList(GFFStruct gffStruct, string label)
    {
        uint offset = _reader.ReadUInt32(); // Relative to list indices
        _reader.BaseStream.Seek(_listIndicesOffset + offset, SeekOrigin.Begin);

        var value = new GFFList();
        uint count = _reader.ReadUInt32();
        var listIndices = new List<int>();

        for (int i = 0; i < count; i++)
        {
            listIndices.Add((int)_reader.ReadUInt32());
        }

        foreach (int structIndex in listIndices)
        {
            value.Add(0);
            var child = value.At(value.Count - 1);
            if (child != null)
            {
                LoadStruct(child, structIndex);
            }
        }

        gffStruct.SetList(label, value);
    }
}

