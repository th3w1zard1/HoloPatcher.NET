namespace TSLPatcher.Core.Formats.GFF;

/// <summary>
/// The different resources that the GFF can represent.
/// </summary>
public enum GFFContent
{
    GFF,
    BIC,
    BTC,
    BTD,
    BTE,
    BTI,
    BTP,
    BTM,
    BTT,
    UTC,
    UTD,
    UTE,
    UTI,
    UTP,
    UTS,
    UTM,
    UTT,
    UTW,
    ARE,
    DLG,
    FAC,
    GIT,
    GUI,
    IFO,
    ITP,
    JRL,
    PTH,
    NFO,
    PT,
    GVT,
    INV
}

public static class GFFContentExtensions
{
    public static string ToFourCC(this GFFContent content)
    {
        return content.ToString().PadRight(4);
    }

    public static GFFContent FromFourCC(string fourCC)
    {
        return fourCC.Trim() switch
        {
            "GFF" => GFFContent.GFF,
            "BIC" => GFFContent.BIC,
            "BTC" => GFFContent.BTC,
            "BTD" => GFFContent.BTD,
            "BTE" => GFFContent.BTE,
            "BTI" => GFFContent.BTI,
            "BTP" => GFFContent.BTP,
            "BTM" => GFFContent.BTM,
            "BTT" => GFFContent.BTT,
            "UTC" => GFFContent.UTC,
            "UTD" => GFFContent.UTD,
            "UTE" => GFFContent.UTE,
            "UTI" => GFFContent.UTI,
            "UTP" => GFFContent.UTP,
            "UTS" => GFFContent.UTS,
            "UTM" => GFFContent.UTM,
            "UTT" => GFFContent.UTT,
            "UTW" => GFFContent.UTW,
            "ARE" => GFFContent.ARE,
            "DLG" => GFFContent.DLG,
            "FAC" => GFFContent.FAC,
            "GIT" => GFFContent.GIT,
            "GUI" => GFFContent.GUI,
            "IFO" => GFFContent.IFO,
            "ITP" => GFFContent.ITP,
            "JRL" => GFFContent.JRL,
            "PTH" => GFFContent.PTH,
            "NFO" => GFFContent.NFO,
            "PT" => GFFContent.PT,
            "GVT" => GFFContent.GVT,
            "INV" => GFFContent.INV,
            _ => GFFContent.GFF
        };
    }
}

