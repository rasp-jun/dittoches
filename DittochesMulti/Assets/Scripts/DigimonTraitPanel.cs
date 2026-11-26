using System.Linq;
using UnityEngine;

/// <summary>Read-only HUD: hover to compare contributors, click for the full guide.</summary>
public sealed class DigimonTraitPanel
{
    readonly DigimonTraitHudSnapshot bench=new DigimonTraitHudSnapshot(DigimonTraitHudSnapshot.RowOrder.Scout);
    DigimonTraitOverview overview;
    DigimonTraitHudSnapshot source;
    int revision=-1,benchRevision=-1,page;
    bool known;
    string focus="",described="";
    public string Focus {get{return focus;}}
    public int Page {get{return page;}}
    public string Draw<T>(Rect r,DigimonTraitHudSnapshot snapshot,T held,bool benchKnown=true) where T:struct,DigimonTraitHudSnapshot.IFormation
    {
        bench.Refresh(held);
        var rows=snapshot.Rows;
        int capacity=Mathf.Max(1,Mathf.FloorToInt((r.height-173)/32)),pages=Mathf.Max(1,(rows.Length+capacity-1)/capacity);
        page=Mathf.Clamp(page,0,pages-1);
        if(snapshot.Find(focus)==null||snapshot.Find(focus).count==0)focus=rows.Length>0?rows[0].trait.id:"";
        int active=0,near=0;foreach(var row in rows){if(row.tier>0)active++;if(row.tier<row.trait.tiers.Length&&row.target-row.count==1)near++;}
        float summaryWidth=(r.width-7)*.5f;
        BattleHudTheme.Badge(new Rect(r.x,r.y,summaryWidth,20),"활성  "+active,BattleHudTheme.Teal,10);
        BattleHudTheme.Badge(new Rect(r.x+summaryWidth+7,r.y,summaryWidth,20),"1종 남음  "+near,BattleHudTheme.Gold,10);
        string clicked=null;
        for(int i=0;i<capacity;i++)
        {
            int index=page*capacity+i;if(index>=rows.Length)break;
            var row=rows[index];Rect rect=new Rect(r.x,r.y+25+i*32,r.width,29);
            if(GUI.enabled&&rect.Contains(Event.current.mousePosition))focus=row.trait.id;
            Color tint=DigimonTraitUI.ColorFor(row.trait.id);bool selected=focus==row.trait.id;
            BattleHudTheme.Round(rect,Color.Lerp(BattleHudTheme.Panel,tint,selected?.19f:row.tier>0?.09f:.025f));
            GUI.DrawTexture(new Rect(rect.x+5,rect.y+3,22,22),DigimonTraitUI.Icon(row.trait.id));
            BattleHudTheme.Label(new Rect(rect.x+34,rect.y+1,rect.width-87,21),row.trait.name,12,row.tier>0?BattleHudTheme.Text:BattleHudTheme.Muted,true);
            BattleHudTheme.Label(new Rect(rect.xMax-48,rect.y+1,42,21),row.progress,12,row.tier>0?tint:BattleHudTheme.Muted,true,TextAnchor.MiddleRight);
            float tierWidth=(rect.width-42)/row.trait.tiers.Length;
            for(int step=0;step<row.trait.tiers.Length;step++)BattleHudTheme.Round(new Rect(rect.x+34+step*tierWidth,rect.yMax-4,tierWidth-3,2),step<row.tier?tint:BattleHudTheme.Line);
            if(selected)BattleHudTheme.Round(new Rect(rect.x,rect.y+7,2,rect.height-14),tint);
            if(GUI.Button(rect,new GUIContent("",row.trait.name+" · "+DigimonTraitUI.Next(row.trait,row.count)+"\n클릭: 기여 디지몬과 단계별 효과"),GUIStyle.none))clicked=row.trait.id;
        }
        if(rows.Length==0)
        {
            BattleHudTheme.Label(new Rect(r.x+8,r.y+40,r.width-16,25),"전장에 팀을 배치하세요",13,null,true);
            BattleHudTheme.Label(new Rect(r.x+8,r.y+68,r.width-16,25),"서로 다른 종이 시너지를 만듭니다",11,BattleHudTheme.Muted);
        }
        float navY=r.yMax-147;
        if(pages>1)
        {
            int previousPage=page;
            BattleHudTheme.Round(new Rect(r.x,navY,32,22),BattleHudTheme.Raised);
            BattleHudTheme.Round(new Rect(r.xMax-32,navY,32,22),BattleHudTheme.Raised);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&page>0;
            if(GUI.Button(new Rect(r.x,navY,32,22),"‹",BattleHudTheme.Style(17,true,TextAnchor.MiddleCenter)))page--;
            GUI.enabled=enabled&&page+1<pages;
            if(GUI.Button(new Rect(r.xMax-32,navY,32,22),"›",BattleHudTheme.Style(17,true,TextAnchor.MiddleCenter)))page++;
            GUI.enabled=enabled;
            BattleHudTheme.Label(new Rect(r.x+35,navY,r.width-70,22),(page+1)+" / "+pages,11,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
            if(GUI.enabled&&Event.current.type==EventType.ScrollWheel&&r.Contains(Event.current.mousePosition)&&!Mathf.Approximately(Event.current.delta.y,0))
            {page=Mathf.Clamp(page+(Event.current.delta.y>0?1:-1),0,pages-1);Event.current.Use();}
            if(page!=previousPage)focus=rows[page*capacity].trait.id;
        }
        else BattleHudTheme.Label(new Rect(r.x,navY,r.width,22),"마우스: 빠른 확인 · 클릭: 도감",10,BattleHudTheme.Muted);
        if(source!=snapshot||revision!=snapshot.RebuildCount||benchRevision!=bench.RebuildCount||known!=benchKnown||described!=focus)
        {
            source=snapshot;revision=snapshot.RebuildCount;benchRevision=bench.RebuildCount;known=benchKnown;described=focus;
            overview=snapshot.Describe(focus,bench,benchKnown);
        }
        Rect detail=new Rect(r.x,r.yMax-120,r.width,120);
        BattleHudTheme.Surface(detail,true,overview==null?BattleHudTheme.Line:DigimonTraitUI.ColorFor(focus));
        if(overview==null)
        {
            BattleHudTheme.Label(new Rect(detail.x+9,detail.y+9,detail.width-18,28),"시너지 계획",14,null,true);
            BattleHudTheme.Label(new Rect(detail.x+9,detail.y+43,detail.width-18,26),"도감에서 참여 디지몬을 확인하세요",11,BattleHudTheme.Muted);
            return clicked;
        }
        var focusTint=DigimonTraitUI.ColorFor(focus);
        BattleHudTheme.Label(new Rect(detail.x+10,detail.y+8,detail.width-70,21),overview.trait.name,14,focusTint,true);
        BattleHudTheme.Badge(new Rect(detail.xMax-58,detail.y+10,48,19),overview.tier>0?overview.tier+"단계":"미활성",overview.tier>0?focusTint:BattleHudTheme.Muted,10);
        BattleHudTheme.Label(new Rect(detail.x+10,detail.y+32,detail.width-20,18),overview.Maximum?"최고 단계 달성":(overview.tier+1)+"단계까지 "+overview.missing+"종을 더 배치",11,null,true);
        ArenaInterface.Fill(new Rect(detail.x+10,detail.y+53,detail.width-20,1),BattleHudTheme.Line);
        DrawMembers(new Rect(detail.x+9,detail.y+56,detail.width-18,26),overview,true);
        DrawMembers(new Rect(detail.x+9,detail.y+89,detail.width-18,26),overview,false);
        return clicked;
    }
    static void DrawMembers(Rect r,DigimonTraitOverview info,bool deployed)
    {
        var entries=info.entries;int count=0;foreach(var entry in entries)if(deployed?entry.deployed>0:entry.held>0&&entry.deployed==0)count++;
        Color color=deployed?BattleHudTheme.Teal:new Color(.4f,.68f,1);
        BattleHudTheme.Label(new Rect(r.x,r.y,52,r.height),deployed?"기여 "+count:"후보 "+count,11,BattleHudTheme.Muted);
        if(!deployed&&!info.benchKnown){BattleHudTheme.Label(new Rect(r.x+55,r.y,r.width-55,r.height),"정보 미수신",10,BattleHudTheme.Muted);return;}
        if(count==0){BattleHudTheme.Label(new Rect(r.x+55,r.y,r.width-55,r.height),deployed?"배치 없음":"대기석에 없음",11,BattleHudTheme.Muted);return;}
        int capacity=Mathf.Max(1,Mathf.FloorToInt((r.width-54)/29)),index=0;
        foreach(var entry in entries)
        {
            if(!(deployed?entry.deployed>0:entry.held>0&&entry.deployed==0))continue;
            if(index>=capacity)break;
            Rect portrait=new Rect(r.x+54+index*29,r.y,26,26);
            if(index==capacity-1&&count>capacity)
                BattleHudTheme.Badge(portrait,"+"+(count-index),color,10);
            else
            {
                BattleHudTheme.Round(portrait,Color.Lerp(BattleHudTheme.Panel,color,.15f));
                FaithfulPortraits.Draw(portrait,entry.id,true);
                BattleHudTheme.Round(new Rect(portrait.x+4,portrait.yMax-2,portrait.width-8,2),color);
                var def=TeamPlan.Roster.FirstOrDefault(u=>u.id==entry.id);
                GUI.Label(portrait,new GUIContent("",(def==null?entry.id:def.name)+" · "+(deployed?"전장 기여":"배치 시 +1종")+(entry.innate?"":" · 인장 참여")),GUIStyle.none);
            }
            index++;
        }
    }
}
