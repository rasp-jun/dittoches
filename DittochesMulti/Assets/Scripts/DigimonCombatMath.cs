using System;

/// <summary>Stat math shared by combat and inspection; AP is a flat stat starting at 100.</summary>
public static class DigimonCombatMath
{
    public static int StarIndex(int star){return Math.Max(0,Math.Min(2,star-1));}
    public static float Attack(float baseAttack,int star,float percent)
    {return baseAttack*(float)Math.Pow(1.5,StarIndex(star))*Math.Max(0,1+percent);}
    public static float AbilityPower(float bonus){return Math.Max(0,100+bonus);}
    public static float Skill(float attack,float abilityPower,float adRatio,float apRatio)
    {return Math.Max(0,attack*adRatio+abilityPower*apRatio);}
    public static float Mitigate(float raw,float armor,float magicResist,string damageType)
    {float resist=damageType=="physical"?armor:magicResist;return Math.Max(0,raw)*100/(100+Math.Max(0,resist));}
    static float RowOffset(float row)
    {row=Math.Max(0,Math.Min(7,row));int first=(int)Math.Floor(row),next=Math.Min(7,first+1);return ((first%2)+(next%2-first%2)*(row-first))*.5f;}
    public static float HexDistance(float sx,float sy,float tx,float ty)
    {float r=ty-sy,q=(tx+RowOffset(ty)-ty*.5f)-(sx+RowOffset(sy)-sy*.5f);return (Math.Abs(q)+Math.Abs(r)+Math.Abs(q+r))*.5f;}
    public static bool InAttackRange(float sx,float sy,float tx,float ty,int range)
    {return HexDistance(sx,sy,tx,ty)<=range+.025f;}
}
