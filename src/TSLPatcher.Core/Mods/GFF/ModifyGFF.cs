using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TSLPatcher.Core.Formats.GFF;
using TSLPatcher.Core.Logger;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods.GFF;

/// <summary>
/// Abstract base for GFF modifications.
/// 1:1 port from Python ModifyGFF in pykotor/tslpatcher/mods/gff.py
/// </summary>
public abstract class ModifyGFF
{
    public abstract void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger);

    protected GFFStruct? NavigateToStruct(GFFStruct rootContainer, string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return rootContainer;
        }

        string[] parts = path.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
        object? container = rootContainer;

        foreach (string part in parts)
        {
            if (container is GFFStruct gffStruct)
            {
                // Try to get as struct or list
                if (gffStruct.TryGetStruct(part, out GFFStruct? childStruct))
                {
                    container = childStruct;
                }
                else if (gffStruct.TryGetList(part, out GFFList? childList))
                {
                    container = childList;
                }
                else
                {
                    return null;
                }
            }
            else if (container is GFFList gffList)
            {
                // Part should be a list index
                if (int.TryParse(part, out int index))
                {
                    container = gffList.At(index);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        return container as GFFStruct;
    }
}

/// <summary>
/// Modifies an existing field in a GFF structure.
/// 1:1 port from Python ModifyFieldGFF
/// </summary>
public class ModifyFieldGFF : ModifyGFF
{
    public string Path { get; }
    public FieldValue Value { get; }

    public ModifyFieldGFF(string path, FieldValue value)
    {
        Path = path;
        Value = value;
    }

    public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger)
    {
        if (rootContainer is not GFFStruct rootStruct)
        {
            logger.AddError($"Expected GFFStruct but got {rootContainer.GetType().Name}");
            return;
        }

        // Navigate to the parent struct
        string parentPath = System.IO.Path.GetDirectoryName(Path)?.Replace("\\", "/") ?? "";
        string label = System.IO.Path.GetFileName(Path);

        GFFStruct? navigatedStruct = NavigateToStruct(rootStruct, parentPath);
        if (navigatedStruct == null)
        {
            logger.AddError($"Could not navigate to path: {parentPath}");
            return;
        }

        // Get the existing field type
        if (!navigatedStruct.TryGetFieldType(label, out GFFFieldType fieldType))
        {
            logger.AddError($"Field '{label}' not found in struct");
            return;
        }

        // Get the value and set it
        object value = Value.Value(memory, fieldType);

        // Handle LocalizedStringDelta specially
        if (value is LocalizedStringDelta delta)
        {
            if (navigatedStruct.TryGetLocString(label, out Common.LocalizedString? original))
            {
                delta.Apply(original!, memory);
                navigatedStruct.SetLocString(label, original!);
            }
            return;
        }

        // Set the field based on type
        SetFieldValue(navigatedStruct, label, value, fieldType);
    }

    private static void SetFieldValue(GFFStruct gffStruct, string label, object value, GFFFieldType fieldType)
    {
        switch (fieldType)
        {
            case GFFFieldType.UInt8:
                gffStruct.SetUInt8(label, Convert.ToByte(value));
                break;
            case GFFFieldType.Int8:
                gffStruct.SetInt8(label, Convert.ToSByte(value));
                break;
            case GFFFieldType.UInt16:
                gffStruct.SetUInt16(label, Convert.ToUInt16(value));
                break;
            case GFFFieldType.Int16:
                gffStruct.SetInt16(label, Convert.ToInt16(value));
                break;
            case GFFFieldType.UInt32:
                gffStruct.SetUInt32(label, Convert.ToUInt32(value));
                break;
            case GFFFieldType.Int32:
                gffStruct.SetInt32(label, Convert.ToInt32(value));
                break;
            case GFFFieldType.UInt64:
                gffStruct.SetUInt64(label, Convert.ToUInt64(value));
                break;
            case GFFFieldType.Int64:
                gffStruct.SetInt64(label, Convert.ToInt64(value));
                break;
            case GFFFieldType.Single:
                gffStruct.SetSingle(label, Convert.ToSingle(value));
                break;
            case GFFFieldType.Double:
                gffStruct.SetDouble(label, Convert.ToDouble(value));
                break;
            case GFFFieldType.String:
                gffStruct.SetString(label, value.ToString() ?? "");
                break;
            case GFFFieldType.ResRef:
                gffStruct.SetResRef(label, value as Common.ResRef ?? Common.ResRef.FromBlank());
                break;
            case GFFFieldType.LocalizedString:
                gffStruct.SetLocString(label, value as Common.LocalizedString ?? new Common.LocalizedString(0));
                break;
            case GFFFieldType.Vector3:
                if (value is Common.Vector3 v3) gffStruct.SetVector3(label, v3);
                break;
            case GFFFieldType.Vector4:
                if (value is Common.Vector4 v4) gffStruct.SetVector4(label, v4);
                break;
        }
    }
}

/// <summary>
/// Adds a new field to a GFF structure.
/// 1:1 port from Python AddFieldGFF
/// </summary>
public class AddFieldGFF : ModifyGFF
{
    public string Identifier { get; }
    public string Label { get; }
    public GFFFieldType FieldType { get; }
    public FieldValue Value { get; }
    public string Path { get; }
    public List<ModifyGFF> Modifiers { get; } = new();

    public AddFieldGFF(string identifier, string label, GFFFieldType fieldType, FieldValue value, string path)
    {
        Identifier = identifier;
        Label = label;
        FieldType = fieldType;
        Value = value;
        Path = path;
    }

    public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger)
    {
        if (rootContainer is not GFFStruct rootStruct)
        {
            logger.AddError($"Expected GFFStruct but got {rootContainer.GetType().Name}");
            return;
        }

        // Resolve the special sentinel used when adding a Struct
        string containerPath = Path.EndsWith(">>##INDEXINLIST##<<") ? System.IO.Path.GetDirectoryName(Path)?.Replace("\\", "/") ?? "" : Path;

        GFFStruct? navigatedStruct = NavigateToStruct(rootStruct, containerPath);
        if (navigatedStruct == null)
        {
            logger.AddError($"Unable to add new GFF Field '{Label}' at GFF Path '{containerPath}': does not exist!");
            return;
        }

        object value = Value.Value(memory, FieldType);

        // Set the field based on type
        switch (FieldType)
        {
            case GFFFieldType.UInt8:
                navigatedStruct.SetUInt8(Label, Convert.ToByte(value));
                break;
            case GFFFieldType.Int8:
                navigatedStruct.SetInt8(Label, Convert.ToSByte(value));
                break;
            case GFFFieldType.UInt16:
                navigatedStruct.SetUInt16(Label, Convert.ToUInt16(value));
                break;
            case GFFFieldType.Int16:
                navigatedStruct.SetInt16(Label, Convert.ToInt16(value));
                break;
            case GFFFieldType.UInt32:
                navigatedStruct.SetUInt32(Label, Convert.ToUInt32(value));
                break;
            case GFFFieldType.Int32:
                navigatedStruct.SetInt32(Label, Convert.ToInt32(value));
                break;
            case GFFFieldType.UInt64:
                navigatedStruct.SetUInt64(Label, Convert.ToUInt64(value));
                break;
            case GFFFieldType.Int64:
                navigatedStruct.SetInt64(Label, Convert.ToInt64(value));
                break;
            case GFFFieldType.Single:
                navigatedStruct.SetSingle(Label, Convert.ToSingle(value));
                break;
            case GFFFieldType.Double:
                navigatedStruct.SetDouble(Label, Convert.ToDouble(value));
                break;
            case GFFFieldType.String:
                navigatedStruct.SetString(Label, value.ToString() ?? "");
                break;
            case GFFFieldType.ResRef:
                navigatedStruct.SetResRef(Label, value as Common.ResRef ?? Common.ResRef.FromBlank());
                break;
            case GFFFieldType.LocalizedString:
                if (value is LocalizedStringDelta delta && navigatedStruct.TryGetLocString(Label, out var existing))
                {
                    delta.Apply(existing!, memory);
                }
                else if (value is Common.LocalizedString locString)
                {
                    navigatedStruct.SetLocString(Label, locString);
                }
                break;
            case GFFFieldType.Vector3:
                if (value is Common.Vector3 v3) navigatedStruct.SetVector3(Label, v3);
                break;
            case GFFFieldType.Vector4:
                if (value is Common.Vector4 v4) navigatedStruct.SetVector4(Label, v4);
                break;
            case GFFFieldType.List:
                if (value is GFFList list) navigatedStruct.SetList(Label, list);
                break;
            case GFFFieldType.Struct:
                if (value is GFFStruct @struct) navigatedStruct.SetStruct(Label, @struct);
                break;
        }

        // Apply any modifiers
        foreach (var modifier in Modifiers)
        {
            modifier.Apply(rootStruct, memory, logger);
        }
    }
}

/// <summary>
/// Adds a new struct to a GFF list.
/// 1:1 port from Python AddStructToListGFF
/// </summary>
public class AddStructToListGFF : ModifyGFF
{
    public string Identifier { get; }
    public FieldValue Value { get; }
    public string Path { get; }
    public int? IndexToToken { get; }
    public List<ModifyGFF> Modifiers { get; } = new();

    public AddStructToListGFF(string identifier, FieldValue value, string path, int? indexToToken = null)
    {
        Identifier = identifier;
        Value = value;
        Path = path;
        IndexToToken = indexToToken;
    }

    public override void Apply(object rootContainer, PatcherMemory memory, PatchLogger logger)
    {
        if (rootContainer is not GFFStruct rootStruct)
        {
            logger.AddError($"Expected GFFStruct but got {rootContainer.GetType().Name}");
            return;
        }

        // Resolve the special sentinel
        string listPath = Path.EndsWith(">>##INDEXINLIST##<<") ? System.IO.Path.GetDirectoryName(Path)?.Replace("\\", "/") ?? "" : Path;

        // Navigate to the list container
        object? navigatedContainer = null;
        if (string.IsNullOrEmpty(listPath))
        {
            navigatedContainer = rootStruct;
        }
        else
        {
            string[] parts = listPath.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            object? container = rootStruct;

            foreach (string part in parts)
            {
                if (container is GFFStruct currentStruct)
                {
                    if (currentStruct.TryGetStruct(part, out GFFStruct? childStruct))
                    {
                        container = childStruct;
                    }
                    else if (currentStruct.TryGetList(part, out GFFList? childList))
                    {
                        container = childList;
                    }
                    else
                    {
                        container = null;
                        break;
                    }
                }
                else if (container is GFFList gffList && int.TryParse(part, out int index))
                {
                    container = gffList.At(index);
                }
                else
                {
                    container = null;
                    break;
                }
            }
            navigatedContainer = container;
        }

        if (navigatedContainer is not GFFList listContainer)
        {
            logger.AddError($"Unable to add struct to list '{listPath}': Not a GFFList");
            return;
        }

        // Get the struct ID from the value
        object valueObj = Value.Value(memory, GFFFieldType.Struct);
        int structId = 0;
        if (valueObj is GFFStruct gffStruct)
        {
            structId = gffStruct.StructId;
        }
        else if (valueObj is int id)
        {
            structId = id;
        }

        // Add new struct to the list
        var newStruct = listContainer.Add(structId);
        int newIndex = listContainer.Count - 1;

        // Store the index if requested
        if (IndexToToken.HasValue)
        {
            memory.Memory2DA[IndexToToken.Value] = newIndex.ToString();
        }

        // Apply any modifiers - need to replace the sentinel path
        foreach (var modifier in Modifiers)
        {
            if (modifier is AddFieldGFF addField)
            {
                string modPath = addField.Path;
                if (modPath.Contains(">>##INDEXINLIST##<<"))
                {
                    modPath = modPath.Replace(">>##INDEXINLIST##<<", newIndex.ToString());
                }
                var newAddField = new AddFieldGFF(addField.Identifier, addField.Label, addField.FieldType, addField.Value, listPath + "/" + newIndex + "/" + addField.Label.Split('/').Last());
                newAddField.Apply(rootStruct, memory, logger);
            }
            else
            {
                modifier.Apply(rootStruct, memory, logger);
            }
        }
    }
}

