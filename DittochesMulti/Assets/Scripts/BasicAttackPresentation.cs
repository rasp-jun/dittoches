using UnityEngine;

// Presentation only: attack damage, range and cadence remain simulation-owned.
public static class BasicAttackPresentation
{
    public enum Kind { Bash, Bite, Claw, Punch, Horn, Sword, Whip, Bubble, Seed, Pollen, Fire, Electric, Air, Feather, Missile, Flower, Ice, Holy, Music, Water }
    public static Kind For(string id)
    {
        switch(id)
        {
            case "koromon":case "mochimon":case "plotmon":return Kind.Bash;
            case "tokomon":case "garurumon":return Kind.Bite;
            case "agumon":case "greymon":case "weregarurumon":case "wargreymon":case "kuwagamon":case "tailmon":return Kind.Claw;
            case "togemon":case "leomon":case "meramon":return Kind.Punch;
            case "monochromon":return Kind.Horn;
            case "ogremon":case "andromon":case "piedmon":return Kind.Sword;
            case "skullgreymon":case "machinedramon":return Kind.Missile;
            case "atlur":case "herakle":return Kind.Horn;
            case "angemon":case "holyangemon":return Kind.Sword;
            case "palmon":case "rosemon":case "devimon":case "nyaromon":return Kind.Whip;
            case "tsunomon":return Kind.Bubble;
            case "tanemon":return Kind.Seed;
            case "pyocomon":return Kind.Pollen;
            case "gabumon":case "piyomon":return Kind.Fire;
            case "tentomon":case "kabuterimon":return Kind.Electric;
            case "patamon":return Kind.Air;
            case "birdramon":case "garudamon":case "hououmon":return Kind.Feather;
            case "metalgreymon":return Kind.Missile;
            case "lilimon":return Kind.Flower;
            case "metalgarurumon":return Kind.Ice;
            case "seraphimon":case "angewomon":case "ophanimon":return Kind.Holy;
            case "etemon":return Kind.Music;
            case "shellmon":return Kind.Water;
            default:return Kind.Bash;
        }
    }
    public static bool Contact(Kind kind){return (int)kind<=(int)Kind.Whip;}
    public static float Windup(string id)
    {var k=For(id);return k==Kind.Horn||k==Kind.Sword?.26f:k==Kind.Claw||k==Kind.Whip?.22f:.18f;}
    public static float Recovery(string id)
    {var k=For(id);return k==Kind.Punch?.26f:k==Kind.Claw?.30f:.35f;}
    public static float Travel(string id){return Contact(For(id))?0:For(id)==Kind.Electric?.10f:For(id)==Kind.Missile?.22f:.17f;}
    public static float Lifetime(string id){return Travel(id)+.19f;}
    public static string Joint(string id)
    {
        if(id=="ogremon")return "HandL"; // Source-model club is held by the negative-X hand.
        switch(For(id))
        {
            case Kind.Claw:case Kind.Punch:case Kind.Sword:case Kind.Whip:case Kind.Flower:return "HandR";
            case Kind.Feather:return "WingR";
            case Kind.Missile:return "Spine";
            case Kind.Electric:case Kind.Horn:return "Head";
            default:return "Jaw";
        }
    }
    public static float WindupPhase(float progress)
    {float t=Mathf.Clamp01(progress);return t*t;}
    public static float RecoveryPhase(float age,float duration)
    {float t=duration>0?Mathf.Clamp01(age/duration):1;return t*t*(3-2*t);}
}
