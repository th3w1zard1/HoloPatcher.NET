namespace TSLPatcher.Core.Common;

/// <summary>
/// Language IDs recognized by both KOTOR games.
/// Found in the TalkTable header, and CExoLocStrings (LocalizedStrings) within GFFs.
/// </summary>
public enum Language
{
    Unknown = 0x7FFFFFFE,

    // Official releases
    English = 0,
    French = 1,
    German = 2,
    Italian = 3,
    Spanish = 4,
    Polish = 5,  // Only released for K1

    // Extended language support
    Afrikaans = 6,
    Basque = 7,
    Breton = 9,
    Catalan = 10,
    Danish = 14,
    Dutch = 15,
    Finnish = 19,
    Galician = 22,
    Icelandic = 27,
    Indonesian = 29,
    Irish = 31,
    Latin = 34,
    Norwegian = 37,
    Portuguese = 39,
    Swedish = 46,
    Welsh = 54,

    // Cyrillic (cp-1251)
    Bulgarian = 58,
    Belarisian = 59,
    Macedonian = 60,
    Russian = 61,
    Serbian_Cyrillic = 62,
    Ukrainian = 66,

    // Central European (cp-1250)
    Albanian = 68,
    Bosnian_Latin = 69,
    Czech = 70,
    Slovak = 71,
    Slovene = 72,
    Croatian = 73,
    Hungarian = 75,
    Romanian = 76,

    // Greek (cp-1253)
    Greek = 77,

    // Turkish (cp-1254)
    Turkish = 78,

    // Japanese (cp-932)
    Japanese = 128,

    // Korean (cp-949)
    Korean = 129,

    // Chinese Traditional (cp-950)
    Chinese_Traditional = 130,

    // Chinese Simplified (cp-936)
    Chinese_Simplified = 131
}

/// <summary>
/// Gender for localized strings
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1
}

public static class LanguageExtensions
{
    public static string GetEncoding(this Language language)
    {
        return language switch
        {
            Language.Polish => "windows-1250",
            Language.Russian or Language.Bulgarian or Language.Serbian_Cyrillic or
            Language.Ukrainian or Language.Belarisian or Language.Macedonian => "windows-1251",
            Language.Czech or Language.Slovak or Language.Slovene or Language.Croatian or
            Language.Hungarian or Language.Romanian or Language.Albanian or Language.Bosnian_Latin => "windows-1250",
            Language.Greek => "windows-1253",
            Language.Turkish => "windows-1254",
            Language.Japanese => "shift_jis",
            Language.Korean => "ks_c_5601-1987",
            Language.Chinese_Traditional => "big5",
            Language.Chinese_Simplified => "gb2312",
            _ => "windows-1252"
        };
    }
}

