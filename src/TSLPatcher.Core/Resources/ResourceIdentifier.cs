using System;

namespace TSLPatcher.Core.Resources;

/// <summary>
/// Class for storing resource name and type, facilitating case-insensitive object comparisons
/// and hashing equal to their string representations.
/// </summary>
public class ResourceIdentifier : IEquatable<ResourceIdentifier>
{
    public string ResName { get; }
    public ResourceType ResType { get; }

    private readonly string _cachedFilenameStr;
    private readonly string _lowerResName;

    public ResourceIdentifier(string resName, ResourceType resType)
    {
        ResName = resName;
        ResType = resType;

        var ext = resType.Extension;
        var suffix = string.IsNullOrEmpty(ext) ? "" : $".{ext}";
        _cachedFilenameStr = $"{resName}{suffix}".ToLower();
        _lowerResName = resName.ToLower();
    }

    public static ResourceIdentifier FromPath(string path)
    {
        var fileName = System.IO.Path.GetFileName(path);
        var ext = System.IO.Path.GetExtension(fileName).TrimStart('.');
        var resName = System.IO.Path.GetFileNameWithoutExtension(fileName);
        var resType = ResourceType.FromExtension(ext);

        return new ResourceIdentifier(resName, resType);
    }

    public (string, ResourceType) Unpack() => (ResName, ResType);

    public string LowerResName => _lowerResName;

    public override string ToString() => _cachedFilenameStr;

    public override int GetHashCode() => _cachedFilenameStr.GetHashCode();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        return obj switch
        {
            ResourceIdentifier other => Equals(other),
            string str => _cachedFilenameStr == str.ToLower(),
            _ => false
        };
    }

    public bool Equals(ResourceIdentifier? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return _cachedFilenameStr == other._cachedFilenameStr;
    }

    public static bool operator ==(ResourceIdentifier? left, ResourceIdentifier? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ResourceIdentifier? left, ResourceIdentifier? right)
    {
        return !(left == right);
    }
}

