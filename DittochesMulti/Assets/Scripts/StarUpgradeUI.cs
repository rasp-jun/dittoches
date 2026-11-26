using UnityEngine;

/// <summary>Shared star-upgrade forecasts. These describe the existing combat rules.</summary>
public static class StarUpgradeUI
{
    public const float ReceiptDuration=4.2f;
    public static float Duration(int star){return star>=3?2.4f:1.6f;}
    public static Color Tint(int star){return star>=3?BattleHudTheme.Gold:BattleHudTheme.Teal;}
    public static int Copies(int singles,int doubles){return Mathf.Max(0,singles)+3*Mathf.Max(0,doubles);}
    public static int Target(int doubles){return doubles>0?9:3;}
    public static string Progress(int singles,int doubles,int triples=0)
    {
        if(triples>0)return "★★★ 완성 · 최고 성급";
        int target=Target(doubles),count=Mathf.Min(target,Copies(singles,doubles));
        return (target==9?"★★★":"★★")+" 목표 · "+count+" / "+target+"개 · "+Mathf.Max(0,target-count)+"개 남음";
    }
    public static string Collection(int singles,int doubles,int triples=0)
    {
        string text=Progress(singles,doubles,triples)+" (1성 환산)";
        if(triples>0)return "★★★ 완성 · 최고 성급 · 같은 종의 시너지는 1종으로 계산";
        if(singles>=2)text+="\n이번 구매 → "+(doubles>=2?"★★★ 연속 승급":"★★ 승급")+" · 장비 최대 3개 유지, 초과분 반환";
        else text+="\n같은 종·같은 성급 3개 → 다음 성급 · 전장 유닛 우선 합성";
        return text;
    }
    public static float AttackAt(DigimonSkillCatalog.Stats stats,int current,int target)
    {return stats.attack*Mathf.Pow(1.5f,DigimonCombatMath.StarIndex(target)-DigimonCombatMath.StarIndex(current));}
    public static float HealthAt(DigimonSkillCatalog.Stats stats,int current,int target)
    {return stats.health*Mathf.Pow(1.8f,DigimonCombatMath.StarIndex(target)-DigimonCombatMath.StarIndex(current));}
    public static string Growth(int star)
    {return star>=2?"성급당 체력 +80% · 공격력 +50% · 스킬 계수 강화":"";}
    public static void DrawPromotionNotice(Rect rect,string name,int star,bool battle,float left)
    {
        Color previous=GUI.color;GUI.color=new Color(previous.r,previous.g,previous.b,previous.a*Mathf.Clamp01(left/.45f));
        Color tint=Tint(star);
        BattleHudTheme.Surface(rect,false,tint);
        BattleHudTheme.Label(new Rect(rect.x+16,rect.y+8,rect.width-32,28),new string('★',star)+"  "+name+" · "+(star>=3?"최고 성급 완성":"성급 강화 완료"),20,tint,true,TextAnchor.MiddleCenter);
        BattleHudTheme.Label(new Rect(rect.x+16,rect.y+41,rect.width-32,20),battle?"다음 전투부터 강화된 성급 적용 · 장비는 즉시 적용":Growth(star),12,BattleHudTheme.Text,false,TextAnchor.MiddleCenter);
        GUI.color=previous;
    }
}
