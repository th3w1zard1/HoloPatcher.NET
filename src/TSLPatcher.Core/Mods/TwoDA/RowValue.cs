using System;
using TSLPatcher.Core.Formats.TwoDA;
using TSLPatcher.Core.Memory;
using TSLPatcher.Core.Mods.NSS;

namespace TSLPatcher.Core.Mods.TwoDA;

/// <summary>
/// Abstract base for row values that can be constants or memory references.
/// 1:1 port from Python RowValue in pykotor/tslpatcher/mods/twoda.py
/// </summary>
public abstract class RowValue
{
    public abstract string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row);
}

/// <summary>
/// Row value that stores a constant string.
/// 1:1 port from Python RowValueConstant
/// </summary>
public class RowValueConstant : RowValue
{
    public string String { get; }

    public RowValueConstant(string value)
    {
        String = value;
    }

    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        return String;
    }

    public override string ToString() => $"RowValueConstant(string='{String}')";
}

/// <summary>
/// Row value from 2DA memory.
/// 1:1 port from Python RowValue2DAMemory
/// </summary>
public class RowValue2DAMemory : RowValue
{
    public int TokenId { get; }

    public RowValue2DAMemory(int tokenId)
    {
        TokenId = tokenId;
    }

    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        if (!memory.Memory2DA.ContainsKey(TokenId))
        {
            throw new KeyError($"2DAMEMORY{TokenId} was not defined before use.");
        }
        string memoryVal = memory.Memory2DA[TokenId];
        return memoryVal;
    }

    public override string ToString() => $"RowValue2DAMemory(token_id={TokenId})";
}

/// <summary>
/// Row value from TLK memory.
/// 1:1 port from Python RowValueTLKMemory
/// </summary>
public class RowValueTLKMemory : RowValue
{
    public int TokenId { get; }

    public RowValueTLKMemory(int tokenId)
    {
        TokenId = tokenId;
    }

    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        if (!memory.MemoryStr.ContainsKey(TokenId))
        {
            throw new KeyError($"StrRef{TokenId} was not defined before use.");
        }
        return memory.MemoryStr[TokenId].ToString();
    }

    public override string ToString() => $"RowValueTLKMemory(token_id={TokenId})";
}

/// <summary>
/// Row value that returns the highest value in a column or row label.
/// 1:1 port from Python RowValueHigh
/// </summary>
public class RowValueHigh : RowValue
{
    public string? Column { get; }

    public RowValueHigh(string? column)
    {
        Column = column;
    }

    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        if (Column == null)
        {
            return LabelMax(twoda).ToString();
        }
        return ColumnMax(twoda, Column).ToString();
    }

    private int LabelMax(Core.Formats.TwoDA.TwoDA twoda)
    {
        int maxFound = -1;
        foreach (string label in twoda.GetLabels())
        {
            if (int.TryParse(label, out int value))
            {
                maxFound = Math.Max(value, maxFound);
            }
        }
        return maxFound + 1;
    }

    private int ColumnMax(Core.Formats.TwoDA.TwoDA twoda, string header)
    {
        int maxFound = -1;
        foreach (string cell in twoda.GetColumn(header))
        {
            if (int.TryParse(cell, out int value))
            {
                maxFound = Math.Max(value, maxFound);
            }
        }
        return maxFound + 1;
    }

    public override string ToString() => $"RowValueHigh(column='{Column}')";
}

/// <summary>
/// Row value that returns the row index.
/// 1:1 port from Python RowValueRowIndex
/// </summary>
public class RowValueRowIndex : RowValue
{
    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        if (row == null) return "";
        return twoda.RowIndex(row)?.ToString() ?? "";
    }
}

/// <summary>
/// Row value that returns the row label.
/// 1:1 port from Python RowValueRowLabel
/// </summary>
public class RowValueRowLabel : RowValue
{
    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        return row?.Label() ?? "";
    }
}

/// <summary>
/// Row value that returns a cell value from the current row.
/// 1:1 port from Python RowValueRowCell
/// </summary>
public class RowValueRowCell : RowValue
{
    public string Column { get; }

    public RowValueRowCell(string column)
    {
        Column = column;
    }

    public override string Value(PatcherMemory memory, Core.Formats.TwoDA.TwoDA twoda, TwoDARow? row)
    {
        return row == null ? "" : row.GetString(Column);
    }

    public override string ToString() => $"RowValueRowCell(column='{Column}')";
}
