using UnityEngine;

// Visual identity only; simulation timing, hit geometry and damage remain unchanged.
public static class CombatEffectStyle
{
    public enum Element { Force, Flame, Storm, Frost, Nature, Light, Shadow, Water }
    public static Element Skill(string visual)
    {
        switch(visual)
        {
            case "fire":case "bluefire":case "spiral":case "meteors":case "phoenix":case "sun":return Element.Flame;
            case "lightning":case "electricorb":case "thunderbeam":return Element.Storm;
            case "ice":return Element.Frost;
            case "vine":case "whip":case "needles":case "flower":return Element.Nature;
            case "holy":case "holybeam":case "holyarrow":case "javelin":case "seven":case "starlight":case "gate":return Element.Light;
            case "darkclaw":case "darkorb":return Element.Shadow;
            case "bubble":case "air":case "water":return Element.Water;
            default:return Element.Force;
        }
    }
    public static Element Basic(BasicAttackPresentation.Kind kind,string id)
    {
        switch(kind)
        {
            case BasicAttackPresentation.Kind.Fire:case BasicAttackPresentation.Kind.Feather:return Element.Flame;
            case BasicAttackPresentation.Kind.Electric:return Element.Storm;
            case BasicAttackPresentation.Kind.Ice:return Element.Frost;
            case BasicAttackPresentation.Kind.Seed:case BasicAttackPresentation.Kind.Pollen:case BasicAttackPresentation.Kind.Flower:return Element.Nature;
            case BasicAttackPresentation.Kind.Holy:return Element.Light;
            case BasicAttackPresentation.Kind.Music:return Element.Shadow;
            case BasicAttackPresentation.Kind.Bubble:case BasicAttackPresentation.Kind.Air:case BasicAttackPresentation.Kind.Water:return Element.Water;
            case BasicAttackPresentation.Kind.Whip:return id=="devimon"?Element.Shadow:Element.Nature;
            default:return Element.Force;
        }
    }
}
