using UnityEngine;

/// <summary>Presentation-only evolution sizes. Independent of price, skill radius and combat stats.</summary>
public static class DigimonVisualScale
{
    public enum Evolution { Unknown=0, Baby=1, Rookie=2, Champion=3, Ultimate=4, Mega=5 }
    public static Evolution Stage(string id)
    {
        switch(id)
        {
            case "koromon":case "tsunomon":case "mochimon":case "tanemon":case "pyocomon":case "tokomon":return Evolution.Baby;
            case "agumon":case "gabumon":case "tentomon":case "palmon":case "piyomon":case "patamon":return Evolution.Rookie;
            case "togemon":case "garurumon":case "greymon":case "kabuterimon":case "angemon":case "birdramon":
            case "kuwagamon":case "shellmon":case "devimon":return Evolution.Champion;
            case "metalgreymon":case "weregarurumon":case "lilimon":case "holyangemon":case "atlur":case "garudamon":case "etemon":return Evolution.Ultimate;
            case "herakle":case "hououmon":case "wargreymon":case "metalgarurumon":case "rosemon":case "seraphimon":return Evolution.Mega;
            default:return Evolution.Unknown;
        }
    }
    public static string StageName(string id)
    {
        switch(Stage(id))
        {case Evolution.Baby:return "유년기";case Evolution.Rookie:return "성장기";case Evolution.Champion:return "성숙기";case Evolution.Ultimate:return "완전체";case Evolution.Mega:return "궁극체";default:return "";}
    }
    public static float Height(string id,int star=1)
    {
        float height;
        switch(Stage(id))
        {case Evolution.Baby:height=.52f;break;case Evolution.Rookie:height=.88f;break;case Evolution.Champion:height=1.28f;break;case Evolution.Ultimate:height=1.64f;break;case Evolution.Mega:height=2.02f;break;default:height=1.3f;break;}
        // Staffs, raised wings and tall crests need a little more space to retain the torso's presence.
        switch(id)
        {
            case "birdramon":case "angemon":height*=1.07f;break;
            case "holyangemon":case "garudamon":case "seraphimon":case "hououmon":height*=1.04f;break;
            case "koromon":case "mochimon":height*=.96f;break;
        }
        // Star upgrades remain subtle; even a 3-star lower stage stays below the next stage.
        return height*(1+.03f*(Mathf.Clamp(star,1,3)-1));
    }
    public static float Shadow(string id)
    {return Mathf.Lerp(.19f,.50f,((int)Stage(id)-1)/4f);}
}
