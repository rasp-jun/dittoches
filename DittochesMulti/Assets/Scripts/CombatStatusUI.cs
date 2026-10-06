using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only state from the current native fighter or visible server frame.</summary>
public static class CombatStatusUI
{
    public sealed class Row
    {public string icon,text,detail;public float progress;public bool spent;}
    public static Row[] Describe(DigimonBuildCatalog.Bonus bonus,float age,int attacks,bool crisisUsed,bool recorded,bool ended)
    {
        var rows=new List<Row>();
        if(bonus.rampSpeed>0)
        {
            float amount=recorded?DigimonCombatMath.RampSpeed(bonus.rampSpeed,age):0;
            int count=Mathf.RoundToInt(amount/bonus.rampSpeed);
            string next=!recorded?"전투 시작 후 3초마다 증가":ended?"최종 기록 · 추가 중첩 정지":count>=5?"최대 중첩 도달":"다음 중첩까지 "+Mathf.Max(0,(count+1)*3-age).ToString("0.0")+"초";
            rows.Add(new Row{icon="friendship",text="우정 "+count+"/5 · +"+(amount*100).ToString("0.#")+"%p",progress=count/5f,
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
    public static void Draw(Rect rect,Row[] rows)
    {
        if(label==null)
        {
            label=new GUIStyle(GUI.skin.label){fontSize=12,wordWrap=false,alignment=TextAnchor.MiddleLeft};label.normal.textColor=new Color(.82f,.91f,.93f);
            empty=new GUIStyle(label){fontSize=11};empty.normal.textColor=new Color(.51f,.66f,.7f);
        }
        if(rows.Length==0){GUI.Label(new Rect(rect.x,rect.y,rect.width,24),"누적·1회 효과 없음",empty);return;}
        for(int i=0;i<rows.Length;i++)
        {
            var row=rows[i];Rect r=new Rect(rect.x,rect.y+i*28,rect.width,25);
            ArenaInterface.Fill(r,new Color(.043f,.075f,.087f));
            GUI.DrawTexture(new Rect(r.x+2,r.y+2,20,20),DigimonTraitUI.Icon(row.icon));
            GUI.Label(new Rect(r.x+27,r.y,r.width-29,23),new GUIContent(row.text,row.detail),label);
            ArenaInterface.Fill(new Rect(r.x+26,r.yMax-2,(r.width-29)*Mathf.Clamp01(row.progress),2),row.spent?new Color(.45f,.52f,.54f):DigimonTraitUI.ColorFor(row.icon));
        }
    }
}
