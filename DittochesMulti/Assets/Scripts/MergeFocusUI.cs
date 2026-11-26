using System;
using UnityEngine;

public static class MergeFocusUI
{
    public static readonly Color MaterialTint=new Color(.33f,.68f,1f);
    public static bool Visible(PurchaseForecast.Source source,bool battle){return !battle||!source.board;}
    public static Vector3 Position(PurchaseForecast.Source source)
    {return source.board?TacticalArena.CellWorld(source.slot%7,source.slot/7+4):TacticalArena.BenchWorld(source.slot);}
    public static void Ground(TacticalArena arena,PurchaseForecast forecast,bool battle,Func<PurchaseForecast.Source,Vector3> position)
    {
        if(forecast==null)return;
        foreach(var source in forecast.sources)if(Visible(source,battle))
            arena.DrawMergeFocus(position(source),forecast.result.star,source.recipient,Time.unscaledTime);
    }
    public static void Labels(TacticalArena arena,Rect viewport,PurchaseForecast forecast,string name,bool battle,Func<PurchaseForecast.Source,Vector3> position)
    {
        if(forecast==null)return;
        Color tint=StarUpgradeUI.Tint(forecast.result.star);
        float labelWidth=viewport.width<1100?84:98;
        foreach(var source in forecast.sources)
        {
            if(!Visible(source,battle))continue;
            float height=source.star==0?.55f:TacticalArena.DigimonHeadHeight(forecast.result.id,source.star)*(source.board?1:.85f)+.15f;
            Vector2 point=arena.Project(position(source)+Vector3.up*height);
            Rect label=new Rect(Mathf.Clamp(point.x-labelWidth/2,viewport.x+2,viewport.xMax-labelWidth-2),Mathf.Clamp(point.y-24,viewport.y+3,viewport.yMax-26),labelWidth,24);
            BattleHudTheme.Round(label,new Color(.009f,.025f,.04f,.96f));
            ArenaInterface.Fill(new Rect(label.x+4,label.yMax-2,label.width-8,2),source.recipient?tint:MaterialTint);
            BattleHudTheme.Label(label,source.recipient?new string('★',forecast.result.star)+(source.star==0?" 생성":" 유지"):new string('★',source.star)+" 합성 재료",labelWidth<90?11:12,source.recipient?tint:MaterialTint,true,TextAnchor.MiddleCenter);
        }
        float width=Mathf.Min(820,viewport.width-34);
        Rect summary=new Rect(viewport.center.x-width/2,viewport.y+86,width,31);
        BattleHudTheme.Round(summary,new Color(.009f,.025f,.04f,.95f));
        string text=name+" · 구매 시 "+new string('★',forecast.result.star)+"  /  "+(battle?"전장 승급은 다음 전투 · 대기석만 표시":"파랑: 합성 재료 · "+(forecast.result.star>=3?"금색":"청록")+": 강화 후 위치");
        BattleHudTheme.FitLabel(new Rect(summary.x+10,summary.y,summary.width-20,summary.height),text,13,tint);
    }
}
