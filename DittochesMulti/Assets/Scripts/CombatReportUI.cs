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
        body=Style(13,BattleHudTheme.Text);muted=Style(11,BattleHudTheme.Muted);
        heading=Style(18,BattleHudTheme.Text);heading.fontStyle=FontStyle.Bold;footer=Style(13,BattleHudTheme.Text);
        tab=new GUIStyle(body){alignment=TextAnchor.MiddleCenter};
        nameAlive=new GUIStyle(body){wordWrap=false};nameDead=new GUIStyle(nameAlive);
        nameDead.normal.textColor=BattleHudTheme.Muted;
        number=new GUIStyle(body){alignment=TextAnchor.UpperRight,wordWrap=false,fontSize=14,fontStyle=FontStyle.Bold};
    }
    static readonly Color attack=new Color(1,.61f,.28f),spell=new Color(.61f,.52f,1),protection=new Color(.46f,.81f,1),healing=new Color(.35f,.87f,.59f);
    static void Fill(Rect r,Color c){Color old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    static string Number(float value){return Mathf.Max(0,value).ToString("N0");}
    static GUIStyle Style(int size,Color color){var s=new GUIStyle(GUI.skin.label){fontSize=size,wordWrap=true};s.normal.textColor=color;return s;}
    public static Row Draw(Rect r,Table rows,ref int metric,string title)
    {
        BattleHudTheme.Surface(r);
        EnsureStyles();
        Fill(new Rect(r.x+12,r.y+15,3,17),BattleHudTheme.Teal);
        GUI.Label(new Rect(r.x+24,r.y+9,r.width-36,29),title,heading);
        metric=Mathf.Clamp(metric,0,3);
        float w=(r.width-24)/4;
        for(int i=0;i<4;i++)
        {
            Rect t=new Rect(r.x+12+i*w,r.y+43,w-3,26);
            Fill(t,metric==i?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.14f):BattleHudTheme.Raised);
            if(metric==i)Fill(new Rect(t.x,t.yMax-2,t.width,2),BattleHudTheme.Teal);
            if(GUI.Button(t,tabs[i],tab))metric=i;
        }
        if(metric<2)
        {
            Fill(new Rect(r.x+14,r.y+82,5,5),attack);Fill(new Rect(r.x+106,r.y+82,5,5),metric==0?spell:protection);
            GUI.Label(new Rect(r.x+22,r.y+75,84,21),metric==0?"기본 공격":"체력 감소",muted);
            GUI.Label(new Rect(r.x+114,r.y+75,r.width-126,21),metric==0?"스킬":"보호막 흡수",muted);
        }
        else GUI.Label(new Rect(r.x+12,r.y+75,r.width-24,21),metric==2?"실제 회복한 체력":"실제 생성한 보호막",muted);
        rows.Prepare(metric);
        Row clicked=null;float max=rows.Maximum,y=r.y+103;
        for(int rowIndex=0;rowIndex<rows.VisibleCount;rowIndex++)
        {
            var row=rows.Ranked(rowIndex);
            Rect hit=new Rect(r.x+9,y,r.width-18,43);bool hover=GUI.enabled&&hit.Contains(Event.current.mousePosition);
            Fill(hit,hover?BattleHudTheme.Raised:Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Raised,rowIndex%2==0?.32f:.12f));
            Fill(new Rect(r.x+12,y+4,28,30),BattleHudTheme.Raised);
            if(row.portrait!=null)GUI.DrawTexture(new Rect(r.x+12,y+3,28,32),row.portrait,ScaleMode.ScaleToFit);
            var name=row.dead?nameDead:nameAlive;
            GUI.Label(new Rect(r.x+44,y,r.width-107,20),new GUIContent(row.name,row.name+(row.dead?" · 전투 불능":"")+" · 시전 "+row.casts+"회"),name);
            GUI.Label(new Rect(r.xMax-64,y,50,20),Number(row.Value(metric)),number);
            string detail=metric==0?"평타 "+Number(row.basic)+" · 스킬 "+Number(row.skill):metric==1?"체력 "+Number(row.taken)+" · 흡수 "+Number(row.absorbed):"시전 "+row.casts+"회"+(row.dead?" · 전투 불능":"");
            GUI.Label(new Rect(r.x+44,y+18,r.width-56,19),detail,muted);
            float width=r.width-56,first=metric==0?row.basic:metric==1?row.taken:row.Value(metric),second=metric==0?row.skill:metric==1?row.absorbed:0;
            Fill(new Rect(r.x+44,y+38,width,4),BattleHudTheme.Line);
            float a=width*Mathf.Clamp01(first/max),b=width*Mathf.Clamp01(second/max);
            Fill(new Rect(r.x+44,y+38,a,4),metric==2?healing:metric==3?protection:attack);
            if(b>0)Fill(new Rect(r.x+44+a,y+38,b,4),metric==0?spell:protection);
            if(GUI.Button(hit,GUIContent.none,GUIStyle.none))clicked=row;y+=44;
        }
        if(rows.Count==0)GUI.Label(new Rect(r.x+12,y+10,r.width-24,48),"완료된 전투 기록이 없습니다.",body);
        y=Mathf.Max(y+8,r.y+508);
        Fill(new Rect(r.x+12,y-5,r.width-24,1),BattleHudTheme.Line);
        BattleHudTheme.Label(new Rect(r.x+13,y+1,83,28),"아군 합계",12,BattleHudTheme.Muted);
        BattleHudTheme.Label(new Rect(r.x+95,y,r.width-110,29),Number(rows.Total),22,metric==2?healing:metric==3?protection:BattleHudTheme.Gold,true,TextAnchor.MiddleRight);
        GUI.Label(new Rect(r.x+12,y+32,r.width-24,38),metric==0?"실제 체력 피해만 집계\n보호막 · 초과 피해 제외":metric==1?"체력 감소 + 보호막 흡수\n방어 적용 후 실제 수치":metric==2?"회복한 체력만 집계\n흡혈 · 재생 포함":"보호막 생성량 기준\n실제 흡수는 ‘받음’에서 확인",muted);
        return clicked;
    }
    public static void Vitals(Rect r,float hp,float maxHp,float mana,float maxMana,float shield,string status)
    {
        EnsureStyles();
        GUI.Label(new Rect(r.x,r.y,r.width,19),"체력 "+Number(hp)+" / "+Number(maxHp),body);
        Fill(new Rect(r.x,r.y+21,r.width,5),BattleHudTheme.Line);
        Fill(new Rect(r.x,r.y+21,r.width*Mathf.Clamp01(hp/Mathf.Max(1,maxHp)),5),healing);
        GUI.Label(new Rect(r.x,r.y+31,r.width,19),"마나 "+Number(mana)+" / "+Number(maxMana),body);
        Fill(new Rect(r.x,r.y+52,r.width,4),BattleHudTheme.Line);
        Fill(new Rect(r.x,r.y+52,r.width*Mathf.Clamp01(mana/Mathf.Max(1,maxMana)),4),protection);
        GUI.Label(new Rect(r.x,r.y+62,r.width,28),"보호막 "+Number(shield)+" · "+status,body);
    }
}
