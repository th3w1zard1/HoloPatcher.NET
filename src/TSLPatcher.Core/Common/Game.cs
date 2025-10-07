namespace TSLPatcher.Core.Common;

/// <summary>
/// Represents which KOTOR game the installation is for.
/// </summary>
public enum Game
{
    /// <summary>
    /// Knights of the Old Republic (KOTOR 1)
    /// </summary>
    K1,

    /// <summary>
    /// Knights of the Old Republic II: The Sith Lords (KOTOR 2 / TSL)
    /// </summary>
    K2
}

public static class GameExtensions
{
    public static bool IsK1(this Game game) => game == Game.K1;
    public static bool IsK2(this Game game) => game == Game.K2;
}

