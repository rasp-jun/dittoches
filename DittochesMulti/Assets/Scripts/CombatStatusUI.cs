using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only state from the current native fighter or visible server frame.</summary>
public static class CombatStatusUI
{
    public sealed class Row
    {public string icon,text,detail;public float progress;public bool spent;}
    public sealed class State
    {public float burnRemaining,shredRemaining,battleAge;public int synergyUsed;public bool revived;}
    public static Row[] Describe(DigimonBuildCatalog.Bonus bonus,float age,int attacks,bool crisisUsed,bool recorded,bool ended,State state=null)
    {
        var rows=new List<Row>();
        state=recorded&&state!=null?state:new State();
        for(int i=0;i<3;i++)
        {
            int bit=1<<i;float power=i==0?bonus.teamRally:i==1?bonus.teamInferno:bonus.teamSalvation;
            if(power<=0&&(state.synergyUsed&bit)==0)continue;
            bool used=(state.synergyUsed&bit)!=0;
            string name=i==0?"9야수 결집":i==1?"9화염 대폭발":"9성역 기적";
            rows.Add(new Row{icon=i==0?"friendship":i==1?"love":"light",text=name+" · "+(used?"발동 완료":ended?"미발동":"대기"),spent=used,progress=used?1:0,
                detail="팀 체력 합계 40% 이하 · 전투당 1회\n전멸하면 발동하지 않습니다. 보호막은 체력에서 제외.\n"+(i==0?"생존 야수 기절 해제 · 체력/보호막 30% · 마나 30":i==1?"모든 적 최대 체력 18% 마법 피해 + 3초 작열":"가장 비싼 사망 아군 1명 체력 40% 부활\n생존 아군 전체 보호막 15% · 동률이면 높은 성급")});
        }
        if(state.burnRemaining>0)rows.Add(new Row{icon="love",text="작열 · "+state.burnRemaining.ToString("0.0")+"초",progress=state.burnRemaining/3,detail="매초 최대 체력 비례 마법 피해\n마법 저항·보호막 적용 · 강한 작열 하나만 유지"});
        if(state.shredRemaining>0)rows.Add(new Row{icon="courage",text="방어 파쇄 · "+state.shredRemaining.ToString("0.0")+"초",progress=state.shredRemaining/4,detail="현재 방어력 수치에 파쇄 반영\n물리 피해에만 영향 · 강한 파쇄 하나만 유지"});
        if(bonus.openingReduction>0)
        {
            float remaining=recorded?Mathf.Max(0,8-state.battleAge):8;
            rows.Add(new Row{icon="guardian",text=remaining<=0?"개전 방벽 · 종료":"개전 방벽 · "+remaining.ToString("0.0")+"초",progress=remaining/8,spent=remaining<=0,
                detail="전투 시작 8초 동안 받는 피해 -"+(bonus.openingReduction*100).ToString("0")+"%\n장비 피해 감소와 합산 · 총 감소 상한 60%"});
        }
        if(bonus.rampSpeed>0)
        {
            float amount=recorded?DigimonCombatMath.RampSpeed(bonus.rampSpeed,age):0;
            int count=Mathf.RoundToInt(amount/bonus.rampSpeed);
            string next=!recorded?"전투 시작 후 3초마다 증가":ended?"최종 기록 · 추가 중첩 정지":count>=5?"최대 중첩 도달":"다음 중첩까지 "+Mathf.Max(0,(count+1)*3-age).ToString("0.0")+"초";
            rows.Add(new Row{icon="friendship",text="야수 "+count+"/5 · +"+(amount*100).ToString("0.#")+"%p",progress=count/5f,
                detail=next+"\n중첩 공속만 표시 · 시작 공속/장비는 능력치에 합산\n생존 중 기절과 기술 동작에도 시간이 흐릅니다."});
        }
        if(bonus.thirdHit>0||bonus.thirdStun>0)
        {
            int count=recorded?Mathf.Max(0,attacks)%3:0;bool stopped=recorded&&ended;
            rows.Add(new Row{icon="artillery",text=stopped?"강화 공격 · 최종 "+count+"/3":count==2?"다음 타격 강화":"강화 공격 · "+(3-count)+"타 남음",progress=count/3f,
                detail=(stopped?"전투 종료 시의 기본 공격 주기":"기본 공격 "+(3-count)+"회 후 강화")+"\n3번째 기본 공격"+(bonus.thirdHit>0?" 피해 +"+(bonus.thirdHit*100).ToString("0.#")+"%":"")+(bonus.thirdStun>0?" · 기절 "+bonus.thirdStun.ToString("0.#")+"초":"")+"\n기술은 기본 공격 횟수에 포함하지 않습니다."});
        }
        if(bonus.lowShield>0||bonus.lowHeal>0)
        {
            bool used=recorded&&crisisUsed;
            rows.Add(new Row{icon="hope",text=used?"위기 효과 · 사용 완료":recorded&&ended?"위기 효과 · 미발동":"위기 효과 · 대기",progress=used?1:0,spent=used,
                detail="체력 35% 이하로 생존 시 전투당 1회\n"+(bonus.lowHeal>0?"최대 체력 "+(bonus.lowHeal*100).ToString("0.#")+"% 회복 · ":"")+"보호막 "+(bonus.lowShield*100).ToString("0.#")+"%\n장비와 시너지의 위기 효과를 합산합니다."});
        }
        return rows.ToArray();
    }
    static GUIStyle label,empty;
    static Vector2 scroll;
    static string scrollSignature="";
    public static void Draw(Rect rect,Row[] rows)
    {
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=false,alignment=TextAnchor.MiddleLeft};label.normal.textColor=BattleHudTheme.Text;
            empty=new GUIStyle(label){fontSize=11};empty.normal.textColor=BattleHudTheme.Muted;
        }
        if(rows.Length==0){BattleHudTheme.Round(new Rect(rect.x,rect.y,rect.width,24),BattleHudTheme.Panel);GUI.Label(new Rect(rect.x+8,rect.y,rect.width-16,24),"누적·1회 효과 없음",empty);return;}
        bool overflow=rows.Length*28>rect.height;
        string signature=string.Join("|",System.Array.ConvertAll(rows,row=>row.icon));
        if(signature!=scrollSignature){scrollSignature=signature;scroll=Vector2.zero;}
        if(overflow){scroll=GUI.BeginScrollView(rect,scroll,new Rect(0,0,rect.width-17,rows.Length*28),false,true);rect=new Rect(0,0,rect.width-17,rows.Length*28);}
        for(int i=0;i<rows.Length;i++)
        {
            var row=rows[i];Rect r=new Rect(rect.x,rect.y+i*28,rect.width,25);
            Color tint=row.spent?BattleHudTheme.Muted:DigimonTraitUI.ColorFor(row.icon);
            BattleHudTheme.Round(r,Color.Lerp(BattleHudTheme.Raised,tint,row.spent?.02f:.07f));
            GUI.DrawTexture(new Rect(r.x+4,r.y+2,20,20),DigimonTraitUI.Icon(row.icon));
            BattleHudTheme.FitLabel(new Rect(r.x+30,r.y,r.width-36,20),row.text,12,row.spent?BattleHudTheme.Muted:BattleHudTheme.Text,false);
            BattleHudTheme.Round(new Rect(r.x+30,r.yMax-4,r.width-37,2),BattleHudTheme.Line);
            BattleHudTheme.Round(new Rect(r.x+30,r.yMax-4,(r.width-37)*Mathf.Clamp01(row.progress),2),tint);
            GUI.Label(r,new GUIContent("",row.detail),GUIStyle.none);
        }
        if(overflow)GUI.EndScrollView();
    }
}
