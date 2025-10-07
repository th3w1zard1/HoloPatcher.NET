using System;

namespace TSLPatcher.Core.Common;

/// <summary>
/// Represents a 4 dimensional vector.
/// </summary>
public struct Vector4 : IEquatable<Vector4>
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }

    public Vector4(float x, float y, float z, float w)
    {
        X = x;
        Y = y;
        Z = z;
        W = w;
    }

    public override string ToString() => $"{X} {Y} {Z} {W}";

    public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

    public override bool Equals(object? obj)
    {
        return obj is Vector4 other && Equals(other);
    }

    public bool Equals(Vector4 other)
    {
        const float epsilon = 1e-9f;
        return Math.Abs(X - other.X) < epsilon &&
               Math.Abs(Y - other.Y) < epsilon &&
               Math.Abs(Z - other.Z) < epsilon &&
               Math.Abs(W - other.W) < epsilon;
    }

    public static Vector4 operator +(Vector4 a, Vector4 b)
        => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);

    public static Vector4 operator -(Vector4 a, Vector4 b)
        => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

    public static Vector4 operator *(Vector4 v, float scalar)
        => new(v.X * scalar, v.Y * scalar, v.Z * scalar, v.W * scalar);

    public static Vector4 operator /(Vector4 v, float scalar)
        => new(v.X / scalar, v.Y / scalar, v.Z / scalar, v.W / scalar);

    public static bool operator ==(Vector4 left, Vector4 right)
        => left.Equals(right);

    public static bool operator !=(Vector4 left, Vector4 right)
        => !left.Equals(right);
}

