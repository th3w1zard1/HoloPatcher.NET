using System;

namespace TSLPatcher.Core.Formats.GFF;

/// <summary>
/// The different types of fields based off what kind of data it stores.
/// </summary>
public enum GFFFieldType
{
    UInt8 = 0,
    Int8 = 1,
    UInt16 = 2,
    Int16 = 3,
    UInt32 = 4,
    Int32 = 5,
    UInt64 = 6,
    Int64 = 7,
    Single = 8,
    Double = 9,
    String = 10,
    ResRef = 11,
    LocalizedString = 12,
    Binary = 13,
    Struct = 14,
    List = 15,
    Vector4 = 16,
    Vector3 = 17
}

public static class GFFFieldTypeExtensions
{
    public static Type GetReturnType(this GFFFieldType fieldType)
    {
        return fieldType switch
        {
            GFFFieldType.UInt8 or GFFFieldType.UInt16 or GFFFieldType.UInt32 or GFFFieldType.UInt64 or
            GFFFieldType.Int8 or GFFFieldType.Int16 or GFFFieldType.Int32 or GFFFieldType.Int64 => typeof(int),
            GFFFieldType.String => typeof(string),
            GFFFieldType.ResRef => typeof(Common.ResRef),
            GFFFieldType.Vector3 => typeof(Common.Vector3),
            GFFFieldType.Vector4 => typeof(Common.Vector4),
            GFFFieldType.LocalizedString => typeof(Common.LocalizedString),
            GFFFieldType.Struct => typeof(GFFStruct),
            GFFFieldType.List => typeof(GFFList),
            GFFFieldType.Binary => typeof(byte[]),
            GFFFieldType.Single => typeof(float),
            GFFFieldType.Double => typeof(double),
            _ => typeof(object)
        };
    }

    public static bool IsIntegerType(this GFFFieldType fieldType)
    {
        return fieldType is GFFFieldType.Int8 or GFFFieldType.UInt8 or
               GFFFieldType.Int16 or GFFFieldType.UInt16 or
               GFFFieldType.Int32 or GFFFieldType.UInt32 or
               GFFFieldType.Int64 or GFFFieldType.UInt64;
    }

    public static bool IsFloatType(this GFFFieldType fieldType)
    {
        return fieldType is GFFFieldType.Single or GFFFieldType.Double;
    }

    public static bool IsStringType(this GFFFieldType fieldType)
    {
        return fieldType is GFFFieldType.String or GFFFieldType.ResRef;
    }
}

