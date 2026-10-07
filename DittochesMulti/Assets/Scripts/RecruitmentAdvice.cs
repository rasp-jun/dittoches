using System;

// Presentation only: these forecasts never change balances or grant income.
public static class RecruitmentAdvice
{
    public const string ShopLockHint="다음 라운드의 자동 새로고침을 막습니다.\n구매한 빈 칸도 그대로 유지합니다.\n구매와 수동 새로고침은 가능하며, 잠금은 직접 해제할 때까지 유지됩니다.";
    public static int Interest(int gold) { return Math.Min(5,Math.Max(0,gold)/10); }
    public static string Bank(int gold)
    {
        int interest=Interest(gold);
        return "이자 +"+interest+"G · "+(interest==5?"최대 이자":((interest+1)*10-gold)+"G 더 모으면 +"+(interest+1)+"G");
    }
    public static string Spend(int gold,int cost)
    {
        if(gold<cost)return "구매까지 "+(cost-gold)+"G 부족";
        int after=gold-cost,loss=Interest(gold)-Interest(after);
        return "구매 후 "+after+"G · 이자 +"+Interest(after)+"G"+(loss>0?" (−"+loss+"G)":"");
    }
    public static string Copies(int singles,int doubles)
    {
        if(singles>=2)return doubles>=2?"구매 → ★★★ 연속 합성":"구매 → ★★ 자동 합성";
        return singles+doubles>0?"★ "+singles+"/3 · ★★ "+doubles+"/3":"같은 유닛 3개로 ★★";
    }
    public static string Result(string name,int star,int cost,int returnedItems)
    {
        return name+" · "+(star>1?new string('★',star)+" 승급 완료":"모집 완료")+" · −"+cost+"G"+
            (returnedItems>0?" · 장비 "+returnedItems+"개 보관함 반환":"");
    }
    public static string Experience(int level,int xp,int needed)
    {
        if(level>=9)return "최고 레벨 · 배치 한도 9";
        int remaining=Math.Max(0,needed-xp);
        return "다음 레벨까지 "+remaining+" XP · "+((remaining+3)/4*4)+"G\n"+
            (remaining<=4?"이번 구매로 레벨 상승 · 배치 한도 증가":"경험치 구매 "+((remaining+3)/4)+"회 필요")+"\n라운드 경험치 +2는 제외한 현재 기준";
    }
}
