using System;

namespace TSLPatcher.Core.Common;

/// <summary>
/// Represents a 3 dimensional vector.
/// </summary>
public struct Vector3 : IEquatable<Vector3>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override string ToString() => $"{X} {Y} {Z}";

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override bool Equals(object? obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public bool Equals(Vector3 other)
    {
        const float epsilon = 1e-9f;
        return Math.Abs(X - other.X) < epsilon &&
               Math.Abs(Y - other.Y) < epsilon &&
               Math.Abs(Z - other.Z) < epsilon;
    }

    public static Vector3 operator +(Vector3 a, Vector3 b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    public static Vector3 operator -(Vector3 a, Vector3 b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    public static Vector3 operator *(Vector3 v, float scalar)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar);

    public static Vector3 operator /(Vector3 v, float scalar)
        => new(v.X / scalar, v.Y / scalar, v.Z / scalar);

    public static bool operator ==(Vector3 left, Vector3 right)
        => left.Equals(right);

    public static bool operator !=(Vector3 left, Vector3 right)
        => !left.Equals(right);
}

