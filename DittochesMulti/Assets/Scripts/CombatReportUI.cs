using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Same accounting labels and stacked bars for native and server replay reports.</summary>
public static class CombatReportUI
{
    public sealed class Row
    {
        public object key;public string name;public Texture portrait;public bool dead;
        public float damage,basic,skill,taken,absorbed,healing,shielding;public int casts;
        public float Value(int metric){return metric==1?taken+absorbed:metric==2?healing:metric==3?shielding:damage;}
    }
    // Each report owns its rows; active rows and ranking reuse their storage between GUI events.
    public sealed class Table
    {
        readonly List<Row> rows=new List<Row>();
        readonly int[] order=new int[9];
        public int Count { get;private set; }
        public int VisibleCount { get;private set; }
        public float Maximum { get;private set; }
        public float Total { get;private set; }
        public void Clear(){Count=0;VisibleCount=0;Maximum=1;Total=0;}
        public Row Add(){if(Count==rows.Count)rows.Add(new Row());return rows[Count++];}
        public Row Ranked(int index){return rows[order[index]];}
        public void Prepare(int metric)
        {
            VisibleCount=0;Maximum=1;Total=0;double total=0;
            for(int i=0;i<Count;i++)
            {
                float value=rows[i].Value(metric);Maximum=Mathf.Max(Maximum,value);total+=value;
                int at=0;while(at<VisibleCount&&rows[order[at]].Value(metric)>=value)at++;
                if(at>=order.Length)continue;
                int last=Mathf.Min(VisibleCount,order.Length-1);
                for(int j=last;j>at;j--)order[j]=order[j-1];
                order[at]=i;VisibleCount=Mathf.Min(VisibleCount+1,order.Length);
            }
            Total=(float)total;
        }
    }
    static readonly string[] tabs={"피해","받음","회복","보호"};
    static GUISkin cachedSkin;static Font cachedFont;
    static GUIStyle body,muted,heading,tab,nameAlive,nameDead,number,footer;
    static void EnsureStyles()
    {
        if(body!=null&&cachedSkin==GUI.skin&&cachedFont==GUI.skin.font)return;
        cachedSkin=GUI.skin;cachedFont=GUI.skin.font;
        body=Style(12,new Color(.8f,.88f,.9f));muted=Style(11,new Color(.59f,.7f,.75f));
        heading=Style(15,Color.white);footer=Style(13,Color.white);
        tab=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
        nameAlive=new GUIStyle(body){wordWrap=false};nameDead=new GUIStyle(nameAlive);
        nameDead.normal.textColor=new Color(.48f,.56f,.59f);
        number=new GUIStyle(body){alignment=TextAnchor.UpperRight,wordWrap=false};
    }
    static readonly Color attack=new Color(1,.61f,.28f),spell=new Color(.61f,.52f,1),protection=new Color(.46f,.81f,1),healing=new Color(.35f,.87f,.59f);
    static void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    static string Number(float value){return Mathf.Max(0,value).ToString("N0");}
    static GUIStyle Style(int size,Color color){var s=new GUIStyle(GUI.skin.label){fontSize=size,wordWrap=true};s.normal.textColor=color;return s;}
    public static Row Draw(Rect r,Table rows,ref int metric,string title)
    {
        Fill(r,new Color(.025f,.045f,.056f,.99f));Fill(new Rect(r.x,r.y,r.width,1),new Color(.32f,.38f,.33f));
        EnsureStyles();
        GUI.Label(new Rect(r.x+12,r.y+10,r.width-24,27),title,heading);
        metric=Mathf.Clamp(metric,0,3);
        float w=(r.width-24)/4;
        for(int i=0;i<4;i++)
        {
            Rect t=new Rect(r.x+12+i*w,r.y+43,w-3,26);
            Fill(t,metric==i?new Color(.24f,.21f,.12f):new Color(.075f,.12f,.14f));
            if(GUI.Button(t,tabs[i],tab))metric=i;
        }
        GUI.Label(new Rect(r.x+12,r.y+75,r.width-24,21),metric==0?"주황: 기본 공격  ·  보라: 스킬":metric==1?"주황: 체력 감소  ·  파랑: 흡수":metric==2?"초과 회복을 제외한 실제 회복":"상한을 반영한 보호막 생성량",muted);
        rows.Prepare(metric);
        Row clicked=null;float max=rows.Maximum,y=r.y+103;
        for(int rowIndex=0;rowIndex<rows.VisibleCount;rowIndex++)
        {
            var row=rows.Ranked(rowIndex);
            Rect hit=new Rect(r.x+9,y,r.width-18,43);bool hover=GUI.enabled&&hit.Contains(Event.current.mousePosition);
            if(hover)Fill(hit,new Color(.1f,.16f,.18f));
            if(row.portrait!=null)GUI.DrawTexture(new Rect(r.x+12,y+3,27,29),row.portrait,ScaleMode.ScaleToFit);
            var name=row.dead?nameDead:nameAlive;
            GUI.Label(new Rect(r.x+44,y,r.width-107,20),new GUIContent(row.name,row.name+(row.dead?" · 전투 불능":"")+" · 시전 "+row.casts+"회"),name);
            GUI.Label(new Rect(r.xMax-64,y,50,20),Number(row.Value(metric)),number);
            string detail=metric==0?"평타 "+Number(row.basic)+" · 스킬 "+Number(row.skill):metric==1?"체력 "+Number(row.taken)+" · 흡수 "+Number(row.absorbed):"시전 "+row.casts+"회"+(row.dead?" · 전투 불능":"");
            GUI.Label(new Rect(r.x+44,y+18,r.width-56,19),detail,muted);
            float width=r.width-56,first=metric==0?row.basic:metric==1?row.taken:row.Value(metric),second=metric==0?row.skill:metric==1?row.absorbed:0;
            Fill(new Rect(r.x+44,y+38,width,4),new Color(.11f,.18f,.21f));
            float a=width*Mathf.Clamp01(first/max),b=width*Mathf.Clamp01(second/max);
            Fill(new Rect(r.x+44,y+38,a,4),metric==2?healing:metric==3?protection:attack);
            if(b>0)Fill(new Rect(r.x+44+a,y+38,b,4),metric==0?spell:protection);
            if(GUI.Button(hit,GUIContent.none,GUIStyle.none))clicked=row;y+=44;
        }
        if(rows.Count==0)GUI.Label(new Rect(r.x+12,y+10,r.width-24,48),"완료된 전투 기록이 없습니다.",body);
        y=Mathf.Max(y+8,r.y+508);
        GUI.Label(new Rect(r.x+12,y,r.width-24,24),"아군 합계  "+Number(rows.Total),footer);
        GUI.Label(new Rect(r.x+12,y+30,r.width-24,44),metric==0?"실제 체력 피해 · 보호막/초과 피해 제외":metric==1?"방어 적용 후 체력 감소 + 보호막 흡수":metric==2?"회복한 체력만 집계 · 흡혈/재생 포함":"생성량 기준 · 실제 흡수는 ‘받음’에서 확인",muted);
        return clicked;
    }
    public static void Vitals(Rect r,float hp,float maxHp,float mana,float maxMana,float shield,string status)
    {
        EnsureStyles();
        GUI.Label(new Rect(r.x,r.y,r.width,19),"체력 "+Number(hp)+" / "+Number(maxHp),body);
        Fill(new Rect(r.x,r.y+21,r.width,5),new Color(.1f,.16f,.19f));
        Fill(new Rect(r.x,r.y+21,r.width*Mathf.Clamp01(hp/Mathf.Max(1,maxHp)),5),healing);
        GUI.Label(new Rect(r.x,r.y+31,r.width,19),"마나 "+Number(mana)+" / "+Number(maxMana),body);
        Fill(new Rect(r.x,r.y+52,r.width,4),new Color(.1f,.16f,.19f));
        Fill(new Rect(r.x,r.y+52,r.width*Mathf.Clamp01(mana/Mathf.Max(1,maxMana)),4),protection);
        GUI.Label(new Rect(r.x,r.y+62,r.width,28),"보호막 "+Number(shield)+" · "+status,body);
    }
}
