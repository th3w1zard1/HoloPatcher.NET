using System;
using System.Linq;
using TSLPatcher.Core.Formats.TwoDA;
using TSLPatcher.Core.Memory;

namespace TSLPatcher.Core.Mods.TwoDA;

/// <summary>
/// Target type for 2DA row operations.
/// 1:1 port from Python TargetType in pykotor/tslpatcher/mods/twoda.py
/// </summary>
public enum TargetType
{
    ROW_INDEX = 0,
    ROW_LABEL = 1,
    LABEL_COLUMN = 2
}

/// <summary>
/// Represents a target row in a 2DA file.
/// 1:1 port from Python Target in pykotor/tslpatcher/mods/twoda.py
/// </summary>
public class Target
{
    public TargetType TargetType { get; }
    public object Value { get; } // Can be string, int, RowValue2DAMemory, or RowValueTLKMemory

    public Target(TargetType targetType, object value)
    {
        TargetType = targetType;
        Value = value;

        if (targetType == TargetType.ROW_INDEX && value is string)
        {
            throw new ArgumentException("Target value must be int if type is row index.");
        }
    }

    public override string ToString()
    {
        return $"Target(target_type=TargetType.{TargetType}, value={Value})";
    }

    /// <summary>
    /// Searches a TwoDA for a row matching the target.
    /// 1:1 port from Python Target.search()
    /// </summary>
    public TwoDARow? Search(Core.Formats.TwoDA.TwoDA twoda, PatcherMemory memory)
    {
        object value = Value;

        // Resolve memory references
        if (Value is RowValueTLKMemory tlkMem)
        {
            value = tlkMem.Value(memory, twoda, null);
        }
        else if (Value is RowValue2DAMemory twodaMem)
        {
            value = twodaMem.Value(memory, twoda, null);
        }

        TwoDARow? sourceRow = null;

        switch (TargetType)
        {
            case TargetType.ROW_INDEX:
                sourceRow = twoda.GetRow(Convert.ToInt32(value));
                break;

            case TargetType.ROW_LABEL:
                sourceRow = twoda.FindRow(value.ToString() ?? "");
                break;

            case TargetType.LABEL_COLUMN:
                var headers = twoda.GetHeaders();
                if (!headers.Contains("label"))
                {
                    throw new WarningError($"'label' could not be found in the twoda's headers: ({TargetType}, {value})");
                }

                var columnValues = twoda.GetColumn("label");
                string valueStr = value.ToString() ?? "";
                if (!columnValues.Contains(valueStr))
                {
                    throw new WarningError($"The value '{value}' could not be found in the twoda's columns");
                }

                foreach (var row in twoda)
                {
                    if (row.GetString("label") == value.ToString())
                    {
                        sourceRow = row;
                        break;
                    }
                }
                break;
        }

        return sourceRow;
    }
}

/// <summary>
/// Warning exception for 2DA modifications.
/// 1:1 port from Python WarningError
/// </summary>
public class WarningError : Exception
{
    public WarningError(string message) : base(message) { }
}

/// <summary>
/// Critical exception for 2DA modifications.
/// 1:1 port from Python CriticalError
/// </summary>
public class CriticalError : Exception
{
    public CriticalError(string message) : base(message) { }
}
