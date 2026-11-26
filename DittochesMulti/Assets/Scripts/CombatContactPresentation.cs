using UnityEngine;

// Uses immutable species/star information, never current HP or a subsequently selected target.
public static class CombatContactPresentation
{
    public static float Height(string id,int star=1)
    {return DigimonVisualScale.Stage(id)==DigimonVisualScale.Evolution.Unknown?.65f:DigimonVisualScale.Height(id,star)*.55f;}
    public static float Surface(string id,int star=1)
    {return DigimonVisualScale.Stage(id)==DigimonVisualScale.Evolution.Unknown?.28f:Mathf.Clamp(DigimonVisualScale.Height(id,star)*.24f,.14f,.5f);}
}
