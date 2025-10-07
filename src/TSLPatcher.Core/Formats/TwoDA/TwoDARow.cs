using System;
using System.Collections.Generic;
using System.Linq;

namespace TSLPatcher.Core.Formats.TwoDA;

/// <summary>
/// Represents a single row in a TwoDA file.
/// </summary>
public class TwoDARow(string rowLabel, Dictionary<string, string> rowData)
{
    private readonly string _rowLabel = rowLabel;
    private readonly Dictionary<string, string> _data = rowData;

    public string Label() => _rowLabel;

    public Dictionary<string, string> GetData() => new(_data);

    public void UpdateValues(Dictionary<string, string> values)
    {
        foreach (var (column, cell) in values)
        {
            SetString(column, cell);
        }
    }

    public string GetString(string header, string? context = null)
    {
        if (!_data.ContainsKey(header))
        {
            string msg = $"The header '{header}' does not exist.";
            if (context != null)
                msg += $" Context: {context}";
            throw new KeyNotFoundException(msg);
        }
        return _data[header];
    }

    public int? GetInteger(string header, int? defaultValue = null)
    {
        var cellValue = GetString(header);
        if (string.IsNullOrWhiteSpace(cellValue) || cellValue == "****")
            return defaultValue;

        if (int.TryParse(cellValue, out int result))
            return result;

        return defaultValue;
    }

    public float? GetFloat(string header, float? defaultValue = null)
    {
        var cellValue = GetString(header);
        if (string.IsNullOrWhiteSpace(cellValue) || cellValue == "****")
            return defaultValue;

        if (float.TryParse(cellValue, out float result))
            return result;

        return defaultValue;
    }

    public bool? GetBoolean(string header, bool? defaultValue = null)
    {
        var intValue = GetInteger(header);
        if (intValue == null)
            return defaultValue;
        return intValue != 0;
    }

    public void SetString(string header, string value)
    {
        if (!_data.ContainsKey(header))
            throw new KeyNotFoundException($"The header '{header}' does not exist.");
        _data[header] = value;
    }

    public void SetInteger(string header, int value)
    {
        SetString(header, value.ToString());
    }

    public void SetFloat(string header, float value)
    {
        SetString(header, value.ToString());
    }

    public void SetBoolean(string header, bool value)
    {
        SetString(header, value ? "1" : "0");
    }

    public override bool Equals(object? obj)
    {
        if (obj is TwoDARow other)
        {
            return _rowLabel == other._rowLabel &&
                   _data.Count == other._data.Count &&
                   _data.All(kvp => other._data.TryGetValue(kvp.Key, out var value) && value == kvp.Value);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_rowLabel, string.Join(",", _data.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}={kvp.Value}")));
    }

    public override string ToString()
    {
        return $"TwoDARow(row_label={_rowLabel}, row_data={{{string.Join(", ", _data.Select(kvp => $"{kvp.Key}={kvp.Value}"))}}}";
    }
}

