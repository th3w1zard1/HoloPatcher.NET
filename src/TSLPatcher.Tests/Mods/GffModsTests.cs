using TSLPatcher.Core.Formats.GFF;
using TSLPatcher.Core.Memory;
using TSLPatcher.Core.Common;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Mods.GFF;
using Xunit;
using FluentAssertions;

namespace TSLPatcher.Tests.Mods;

/// <summary>
/// Tests for GFF modification functionality
/// 1:1 port from tests/tslpatcher/test_mods.py (TestManipulateGFF)
/// </summary>
public class GffModsTests
{
    #region Modify Field Tests - Integer Types

    [Fact]
    public void ModifyField_UInt8()
    {
        // Python test: test_modify_field_uint8
        var gff = new GFF();
        gff.Root.SetUInt8("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetUInt8("Field1").Should().Be(2);
    }

    [Fact]
    public void ModifyField_Int8()
    {
        // Python test: test_modify_field_int8
        var gff = new GFF();
        gff.Root.SetInt8("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetInt8("Field1").Should().Be(2);
    }

    [Fact]
    public void ModifyField_UInt16()
    {
        // Python test: test_modify_field_uint16
        var gff = new GFF();
        gff.Root.SetUInt16("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetUInt16("Field1").Should().Be(2);
    }

    [Fact]
    public void ModifyField_Int16()
    {
        // Python test: test_modify_field_int16
        var gff = new GFF();
        gff.Root.SetInt16("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetInt16("Field1").Should().Be(2);
    }

    [Fact]
    public void ModifyField_UInt32()
    {
        // Python test: test_modify_field_uint32
        var gff = new GFF();
        gff.Root.SetUInt32("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetUInt32("Field1").Should().Be((uint)2);
    }

    [Fact]
    public void ModifyField_Int32()
    {
        // Python test: test_modify_field_int32
        var gff = new GFF();
        gff.Root.SetInt32("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetInt32("Field1").Should().Be(2);
    }

    [Fact]
    public void ModifyField_UInt64()
    {
        // Python test: test_modify_field_uint64
        var gff = new GFF();
        gff.Root.SetUInt64("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant((ulong)2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetUInt64("Field1").Should().Be((ulong)2);
    }

    [Fact]
    public void ModifyField_Int64()
    {
        // Python test: test_modify_field_int64
        var gff = new GFF();
        gff.Root.SetInt64("Field1", 1);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant((long)2)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetInt64("Field1").Should().Be((long)2);
    }

    #endregion

    #region Modify Field Tests - Float Types

    [Fact]
    public void ModifyField_Single()
    {
        // Python test: test_modify_field_single
        var gff = new GFF();
        gff.Root.SetSingle("Field1", 1.234f);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2.345f)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetSingle("Field1").Should().BeApproximately(2.345f, 0.0001f);
    }

    [Fact]
    public void ModifyField_Double()
    {
        // Python test: test_modify_field_double
        var gff = new GFF();
        gff.Root.SetDouble("Field1", 1.234567);

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(2.345678)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetDouble("Field1").Should().Be(2.345678);
    }

    #endregion

    #region Modify Field Tests - String and Complex Types

    [Fact]
    public void ModifyField_String()
    {
        // Python test: test_modify_field_string
        var gff = new GFF();
        gff.Root.SetString("Field1", "abc");

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant("def")));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetString("Field1").Should().Be("def");
    }

    [Fact]
    public void ModifyField_LocString()
    {
        // Python test: test_modify_field_locstring
        var gff = new GFF();
        gff.Root.SetLocString("Field1", LocalizedString.FromInvalid());

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        var delta = new LocalizedStringDelta();
        delta.SetData(Language.English, Gender.Male, "test");
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(delta)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        var locString = patchedGff.Root.GetLocString("Field1");
        locString.Get(Language.English, Gender.Male).Should().Be("test");
    }

    [Fact]
    public void ModifyField_Vector3()
    {
        // Python test: test_modify_field_vector3
        var gff = new GFF();
        gff.Root.SetVector3("Field1", new Vector3(0, 1, 2));

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(new Vector3(1, 2, 3))));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetVector3("Field1").Should().Be(new Vector3(1, 2, 3));
    }

    [Fact]
    public void ModifyField_Vector4()
    {
        // Python test: test_modify_field_vector4
        var gff = new GFF();
        gff.Root.SetVector4("Field1", new Vector4(0, 1, 2, 3));

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("Field1", new FieldValueConstant(new Vector4(1, 2, 3, 4))));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetVector4("Field1").Should().Be(new Vector4(1, 2, 3, 4));
    }

    #endregion

    #region Modify Field Tests - Nested and Memory

    [Fact]
    public void ModifyField_Nested()
    {
        // Python test: test_modify_nested
        var gff = new GFF();
        gff.Root.SetList("List", new GFFList());
        var gffList = gff.Root.GetList("List");
        var gffStruct = gffList.Add(0);
        gffStruct.SetString("String", "");

        var memory = new PatcherMemory();
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("List/0/String", new FieldValueConstant("abc")));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        var patchedList = patchedGff.Root.GetList("List");
        var patchedStruct = patchedList.At(0);
        patchedStruct!.GetString("String").Should().Be("abc");
    }

    [Fact]
    public void ModifyField_2DAMemory()
    {
        // Python test: test_modify_2damemory
        var gff = new GFF();
        gff.Root.SetString("String", "");
        gff.Root.SetUInt8("Integer", 0);

        var memory = new PatcherMemory();
        memory.Memory2DA[5] = "123";
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("String", new FieldValue2DAMemory(5)));
        config.Modifiers.Add(new ModifyFieldGFF("Integer", new FieldValue2DAMemory(5)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetString("String").Should().Be("123");
        patchedGff.Root.GetUInt8("Integer").Should().Be(123);
    }

    [Fact]
    public void ModifyField_TLKMemory()
    {
        // Python test: test_modify_tlkmemory
        var gff = new GFF();
        gff.Root.SetString("String", "");
        gff.Root.SetUInt8("Integer", 0);

        var memory = new PatcherMemory();
        memory.MemoryStr[5] = 123;
        var logger = new PatchLogger();
        var config = new ModificationsGFF("", false);
        config.Modifiers.Add(new ModifyFieldGFF("String", new FieldValueTLKMemory(5)));
        config.Modifiers.Add(new ModifyFieldGFF("Integer", new FieldValueTLKMemory(5)));

        var writer = new GFFBinaryWriter(gff);
        byte[] gffBytes = writer.Write();
        var patchedBytes = (byte[])config.PatchResource(gffBytes, memory, logger, 1);
        var patchedGff = new GFFBinaryReader(patchedBytes).Load();

        patchedGff.Root.GetString("String").Should().Be("123");
        patchedGff.Root.GetUInt8("Integer").Should().Be(123);
    }

    #endregion

    // Note: AddFieldGFF, AddStructToListGFF tests are pending implementation
    // of those classes (requires !FieldPath support). See TODO item #14.
}
