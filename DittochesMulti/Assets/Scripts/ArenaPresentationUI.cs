using UnityEngine;

public static class ArenaPresentationUI
{
    public static void RoundPlate(Rect r,string round,string phase,string clock,bool combat,float remaining=-1,bool trial=false)
    {
        Color tint=combat?(trial&&remaining>=0&&remaining<=.2f?BattleHudTheme.Danger:BattleHudTheme.Gold):BattleHudTheme.Teal;
        BattleHudTheme.Surface(r,true);
        float numberWidth=trial?118:98;
        BattleHudTheme.Round(new Rect(r.x+1,r.y+1,numberWidth,r.height-2),Color.Lerp(BattleHudTheme.Panel,tint,.13f));
        ArenaInterface.Fill(new Rect(r.x+7,r.y+13,3,r.height-26),tint);
        BattleHudTheme.Label(new Rect(r.x+17,r.y+7,numberWidth-25,15),trial?"TRIAL":"ROUND",10,tint,true);
        BattleHudTheme.FitLabel(new Rect(r.x+16,r.y+22,numberWidth-23,35),round,29,null,true);
        BattleHudTheme.Label(new Rect(r.x+numberWidth+18,r.y+8,r.width-numberWidth-128,30),phase,16,null,true);
        BattleHudTheme.Label(new Rect(r.xMax-108,r.y+9,89,29),clock,22,tint,true,TextAnchor.MiddleRight);
        if(remaining>=0)BattleHudTheme.Bar(new Rect(r.x+numberWidth+18,r.yMax-13,r.width-numberWidth-37,4),remaining,tint);
        else BattleHudTheme.Label(new Rect(r.x+numberWidth+18,r.y+38,r.width-numberWidth-37,16),clock=="FINAL"?(trial?"TRIAL ENDED":"LEAGUE COMPLETE"):combat?"BATTLE IN PROGRESS":"FORMATION PHASE",10,BattleHudTheme.Muted);
    }
    public static void SectionHeading(Rect r,string title,string caption,Color? tint=null)
    {
        Color color=tint??BattleHudTheme.Teal;
        ArenaInterface.Fill(new Rect(r.x,r.y+6,3,r.height-12),color);
        BattleHudTheme.Label(new Rect(r.x+13,r.y,r.width-13,r.height),title,18,null,true);
        if(!string.IsNullOrEmpty(caption))BattleHudTheme.Label(new Rect(r.x+13,r.yMax+1,r.width-13,18),caption,11,BattleHudTheme.Muted);
    }
    public static bool CommandButton(Rect r,string title,string detail,string key,bool enabled,bool primary=false,string value="",string tip="")
    {
        bool active=GUI.enabled&&enabled,hover=active&&r.Contains(Event.current.mousePosition);
        Color tint=primary?BattleHudTheme.Teal:BattleHudTheme.Gold;
        BattleHudTheme.Surface(r,hover||primary,primary?tint:(Color?)null);
        if(primary)BattleHudTheme.Round(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),Color.Lerp(BattleHudTheme.Panel,tint,hover?.30f:.16f));
        ArenaInterface.Fill(new Rect(r.x+7,r.y+12,3,r.height-24),active?tint:BattleHudTheme.Line);
        bool tall=r.height>=70,compact=r.height<50;float top=r.y+(tall?13:compact?3:6);
        float reserve=value.Length>0?62:12;
        BattleHudTheme.FitLabel(new Rect(r.x+16,top,r.width-16-reserve,tall?30:compact?21:23),title,tall?23:16,active?(primary?tint:BattleHudTheme.Text):BattleHudTheme.Muted);
        if(value.Length>0)BattleHudTheme.Label(new Rect(r.xMax-60,top,45,23),value,15,active?BattleHudTheme.Gold:BattleHudTheme.Muted,true,TextAnchor.MiddleRight);
        BattleHudTheme.Label(new Rect(r.x+16,r.yMax-(tall?32:compact?18:23),r.width-76,compact?14:18),detail,tall?13:compact?10:11,BattleHudTheme.Muted);
        if(key.Length>0)
        {
            Rect keyRect=new Rect(r.xMax-(key.Length>2?63:39),r.yMax-(tall?33:compact?20:24),key.Length>2?48:24,18);
            ArenaInterface.Fill(keyRect,Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Text,.07f));
            BattleHudTheme.Label(keyRect,key,10,active?BattleHudTheme.Text:BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
        }
        if(!active)BattleHudTheme.Round(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),new Color(.015f,.025f,.04f,.23f));
        bool previous=GUI.enabled;GUI.enabled=active;
        bool clicked=GUI.Button(r,new GUIContent("",tip),GUIStyle.none);GUI.enabled=previous;return clicked;
    }
    public static void SeatMeter(Rect r,int occupied,int capacity,Color tint)
    {
        float gap=4,width=(r.width-gap*(capacity-1))/Mathf.Max(1,capacity);
        for(int i=0;i<capacity;i++)ArenaInterface.Fill(new Rect(r.x+i*(width+gap),r.y,width,r.height),i<occupied?tint:BattleHudTheme.Line);
    }
    public static void FinishTimeline(Rect r,int choice,float age)
    {
        float hit=TacticalArena.FinisherImpactTime(choice);
        float t=Mathf.Clamp(age,0,TacticalArena.FinisherDuration);
        string phase=t>=TacticalArena.FinisherDuration?"재생 완료":t<TacticalArena.FinisherLaunchTime?"집속":t<hit?"상대에게 비행":t<hit+.32f?"타격":"잔광";
        ArenaInterface.Fill(r,new Color(.015f,.025f,.044f,.91f));
        BattleHudTheme.Label(new Rect(r.x+12,r.y+6,r.width-90,22),phase,13,TamerLoadout.FinishColor(choice),true);
        BattleHudTheme.Label(new Rect(r.xMax-80,r.y+6,67,22),t.ToString("0.0")+" / "+TacticalArena.FinisherDuration.ToString("0.0"),11,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        BattleHudTheme.Bar(new Rect(r.x+12,r.yMax-12,r.width-24,4),t/TacticalArena.FinisherDuration,TamerLoadout.FinishColor(choice));
        float mark=r.x+12+(r.width-24)*hit/TacticalArena.FinisherDuration;
        ArenaInterface.Fill(new Rect(mark,r.yMax-15,1,10),BattleHudTheme.Text);
    }
    public static void CosmeticGlyph(Rect r,int tab,int id,Color tint)
    {
        ArenaInterface.Fill(r,Color.Lerp(BattleHudTheme.Panel,tint,.15f));BattleHudTheme.Sheen(r,.08f);
        if(tab==1)
        {
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                float x=r.x+8+col*14+(row%2)*4,y=r.y+19+row*8;
                ArenaInterface.Fill(new Rect(x,y,11,5),Color.Lerp(tint,Color.white,(row+col)%2*.16f));
            }
            ArenaInterface.Fill(new Rect(r.x+8,r.y+9,3,11),tint);ArenaInterface.Fill(new Rect(r.xMax-11,r.y+9,3,11),tint);
            ArenaInterface.Fill(new Rect(r.x+8,r.y+8,r.width-16,3),tint);
        }
        else if(id==2)
        {
            ArenaInterface.Fill(new Rect(r.x+17,r.y+13,3,31),tint);ArenaInterface.Fill(new Rect(r.xMax-20,r.y+13,3,31),tint);
            ArenaInterface.Fill(new Rect(r.x+17,r.y+12,r.width-34,3),tint);
            ArenaInterface.Fill(new Rect(r.center.x-2,r.y+20,4,25),Color.Lerp(tint,Color.white,.5f));
        }
        else
        {
            LobbyVisuals.Crest(new Rect(r.x+10,r.y+8,r.width-20,r.height-16),id==0?"knowledge":"courage",tint);
        }
    }
}
