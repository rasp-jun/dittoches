using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class LoadoutStudio : IDisposable
{
    TacticalArena preview;
    readonly ArenaInterface ui=new ArenaInterface();
    TamerLoadout draft;
    public int Tab,Group;
    public string Search="";
    readonly List<int> matches=new List<int>();
    string matchedSearch;int matchedTab=-1,matchedGroup=-1;
    int previewKind=-1;
    Rect previewRect;
    float effectStarted=-10,appliedAt=-10;
    bool walking;
    GUIStyle searchStyle;
    Vector3 position=new Vector3(0,.23f,0),velocity;
    public TamerLoadout Draft {get{return draft;}}
    public int PendingChanges(TamerLoadout equipped){return draft==null?0:(draft.tamer==equipped.tamer?0:1)+(draft.field==equipped.field?0:1)+(draft.finisher==equipped.finisher?0:1);}
    public static Rect ApplyRect(Rect r){return new Rect(r.xMax-220,r.yMax-62,200,44);}
    public static Rect RevertRect(Rect r){return new Rect(r.xMax-404,r.yMax-62,172,44);}
    public static Rect BrowserRect(Rect r){return new Rect(r.x+r.width*.56f,r.y+116,r.width*.44f-24,r.height-211);}
    public static Rect TabRect(Rect r,int tab){return new Rect(r.x+22+tab*170,r.y+60,158,37);}
    public static Rect ChoiceRect(Rect r,int tab,int index)
    {
        Rect b=BrowserRect(r);int columns=tab==0?3:1,rows=tab==0?4:3;
        float width=(b.width-(columns-1)*10)/columns,height=(b.height-124-(rows-1)*10)/rows;
        return new Rect(b.x+(index%columns)*(width+10),b.y+124+(index/columns)*(height+10),width,height);
    }
    public int MatchCount {get{RefreshMatches();return matches.Count;}}
    public int MatchedChoice(int slot){RefreshMatches();return slot>=0&&slot<matches.Count?matches[slot]:-1;}
    void RefreshMatches()
    {
        if(matchedSearch==Search&&matchedTab==Tab&&matchedGroup==Group)return;
        matchedSearch=Search;matchedTab=Tab;matchedGroup=Group;matches.Clear();
        string[] names=Tab==0?TamerLoadout.Tamers:Tab==1?TamerLoadout.Fields:TamerLoadout.Finishers;
        string query=(Search??"").Trim();
        for(int i=0;i<names.Length;i++)
        {
            if(Tab==0&&Group==1&&i>=7||Tab==0&&Group==2&&i<7)continue;
            string key=names[i]+(Tab==0?" "+TamerLoadout.Models[i]+" "+TamerLoadout.Family(i):"");
            if(query.Length==0||key.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)matches.Add(i);
        }
    }
    public void Preview(TamerLoadout value,int tab){draft=value.Copy();draft.Normalize();Tab=Mathf.Clamp(tab,0,2);Search="";Group=0;effectStarted=Time.unscaledTime;appliedAt=-10;}
    public bool Draw(Rect r,ref TamerLoadout equipped,bool editable=true)
    {
        if(draft==null)draft=equipped.Copy();draft.Normalize();
        BattleHudTheme.Surface(r);
        BattleHudTheme.Label(new Rect(r.x+24,r.y+15,350,32),"파트너 스튜디오",25,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(r.x+230,r.y+20,350,25),"PERSONAL COLLECTION",11,BattleHudTheme.Teal,true);
        string[] tabs={"테이머","아레나","처형 효과"};
        for(int i=0;i<3;i++)if(ui.Button(TabRect(r,i),new GUIContent("0"+(i+1)+"   "+tabs[i]),true,Tab==i,15))
        {Tab=i;Search="";Group=0;effectStarted=Time.unscaledTime;GUI.FocusControl(null);}
        BattleHudTheme.Label(new Rect(r.xMax-370,r.y+65,344,25),"선택 → 미리보기 → 외형 적용",13,BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        BattleHudTheme.Badge(new Rect(r.xMax-164,r.y+20,138,23),"능력치 영향 없음",BattleHudTheme.Teal,11);
        string[] names=Tab==0?TamerLoadout.Tamers:Tab==1?TamerLoadout.Fields:TamerLoadout.Finishers;
        int selected=Tab==0?draft.tamer:Tab==1?draft.field:draft.finisher,applied=Tab==0?equipped.tamer:Tab==1?equipped.field:equipped.finisher;
        Rect browser=BrowserRect(r),view=new Rect(r.x+22,browser.y,browser.x-r.x-40,browser.height);
        DrawPreview(view,selected,names[selected]);
        BattleHudTheme.Label(new Rect(browser.x,browser.y,browser.width,24),Tab==0?"함께할 파트너  /  "+TamerLoadout.Tamers.Length:Tab==1?"전투의 무대  /  3":"승리의 피날레  /  3",18,null,true);
        if(searchStyle==null){searchStyle=new GUIStyle(GUI.skin.textField){fontSize=15,padding=new RectOffset(12,30,8,8)};searchStyle.normal.textColor=searchStyle.focused.textColor=searchStyle.hover.textColor=BattleHudTheme.Text;searchStyle.normal.background=searchStyle.focused.background=searchStyle.hover.background=searchStyle.active.background=null;}
        Rect search=new Rect(browser.x,browser.y+35,browser.width,36);
        BattleHudTheme.Round(search,GUI.GetNameOfFocusedControl()=="cosmetic-search"?BattleHudTheme.Teal:BattleHudTheme.Line);
        BattleHudTheme.Round(new Rect(search.x+1,search.y+1,search.width-2,search.height-2),BattleHudTheme.Background);
        GUI.SetNextControlName("cosmetic-search");Search=GUI.TextField(search,Search??"",40,searchStyle);
        if(string.IsNullOrEmpty(Search)&&GUI.GetNameOfFocusedControl()!="cosmetic-search")BattleHudTheme.Label(new Rect(search.x+12,search.y,search.width-36,search.height),"이름으로 검색",14,BattleHudTheme.Muted);
        if(Search.Length>0)
        {
            Rect clear=new Rect(search.xMax-31,search.y+3,27,30);BattleHudTheme.Label(clear,"×",20,BattleHudTheme.Text,false,TextAnchor.MiddleCenter);
            if(GUI.Button(clear,new GUIContent("","검색 지우기"),GUIStyle.none)){Search="";GUI.FocusControl(null);}
        }
        if(Tab==0)
        {
            string[] groups={"전체","작은 파트너","모험 파트너"};float width=(browser.width-12)/3;
            for(int i=0;i<3;i++)if(SeasonLobbyArt.Tab(new Rect(browser.x+i*(width+6),browser.y+81,width,30),groups[i],Group==i,true,13))Group=i;
        }
        else BattleHudTheme.Label(new Rect(browser.x,browser.y+82,browser.width,28),"장면을 선택해 실제 연출을 확인하세요",13,BattleHudTheme.Muted);
        RefreshMatches();
        if(matches.Count==0)
        {
            BattleHudTheme.Label(new Rect(browser.x,browser.y+166,browser.width,55),"검색 결과가 없습니다",20,BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
            if(SeasonLobbyArt.Tab(new Rect(browser.x+30,browser.y+236,browser.width-60,42),"검색과 분류 초기화",false)){Search="";Group=0;GUI.FocusControl(null);}
        }
        for(int slot=0;slot<matches.Count;slot++)
        {
            int choice=matches[slot];Rect card=ChoiceRect(r,Tab,slot);bool chosen=choice==selected,hover=GUI.enabled&&card.Contains(Event.current.mousePosition);
            BattleHudTheme.Round(card,chosen?BattleHudTheme.Teal:hover?new Color(.28f,.47f,.53f):BattleHudTheme.Line);
            BattleHudTheme.Round(new Rect(card.x+1,card.y+1,card.width-2,card.height-2),chosen?new Color(.073f,.17f,.20f):hover?new Color(.09f,.14f,.19f):BattleHudTheme.Raised);
            if(Tab==0)
            {
                var portrait=TamerLoadout.Portrait(choice);if(portrait!=null)GUI.DrawTexture(new Rect(card.x+10,card.y+3,card.width-20,card.height-31),portrait,ScaleMode.ScaleToFit);
                BattleHudTheme.Label(new Rect(card.x+8,card.yMax-29,card.width-16,23),names[choice],14,chosen?BattleHudTheme.Teal:BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
                if(choice==applied)BattleHudTheme.Badge(new Rect(card.xMax-36,card.y+6,29,19),"✓",SeasonLobbyArt.Mint,13);
            }
            else
            {
                Color color=Tab==1?TamerLoadout.FieldColor(choice):TamerLoadout.FinishColor(choice);
                BattleHudTheme.Round(new Rect(card.x+14,card.y+14,card.height-28,card.height-28),Color.Lerp(BattleHudTheme.Panel,color,.10f));
                ArenaPresentationUI.CosmeticGlyph(new Rect(card.x+23,card.y+(card.height-58)/2,58,58),Tab,choice,color);
                BattleHudTheme.FitLabel(new Rect(card.x+100,card.y+card.height*.24f,card.width-120,29),names[choice],21,chosen?SeasonLobbyArt.Cream:BattleHudTheme.Text);
                BattleHudTheme.Label(new Rect(card.x+102,card.y+card.height*.63f,card.width-120,23),choice==applied?"장착 중  ✓":chosen?"선택한 외형":"미리보기",13,choice==applied?SeasonLobbyArt.Mint:BattleHudTheme.Muted);
            }
            if(GUI.Button(card,new GUIContent("",names[choice]+" · 미리보기"),GUIStyle.none))
            {if(Tab==0)draft.tamer=choice;else if(Tab==1)draft.field=choice;else draft.finisher=choice;effectStarted=Time.unscaledTime;GUI.FocusControl(null);}
        }
        LobbyVisuals.Rule(new Rect(r.x+22,r.yMax-77,r.width-44,1),BattleHudTheme.Line);
        int pending=PendingChanges(equipped);bool saved=Time.unscaledTime-appliedAt<3&&pending==0;
        BattleHudTheme.Label(new Rect(r.x+20,r.yMax-67,r.width-462,21),!editable?"대전 탐색 중 · 미리보기만 가능합니다":saved?"장착 구성을 저장했습니다  ✓":pending>0?pending+"개 외형 변경 · 적용하면 저장됩니다":"현재 장착 구성",13,saved?SeasonLobbyArt.Mint:pending>0?LobbyVisuals.Gold:BattleHudTheme.Muted);
        BattleHudTheme.FitLabel(new Rect(r.x+20,r.yMax-42,r.width-462,25),draft.Summary,14,null,true);
        if(ui.Button(RevertRect(r),new GUIContent("선택 되돌리기"),pending>0)){draft=equipped.Copy();appliedAt=-10;}
        if(SeasonLobbyArt.Play(ApplyRect(r),pending==0?"장착 완료  ✓":"외형 적용",editable&&pending>0,false)){equipped=draft.Copy();equipped.Save();appliedAt=Time.unscaledTime;return true;}
        return false;
    }
    void DrawPreview(Rect view,int selected,string name)
    {
        Rect render=Tab==0?new Rect(view.x+view.width*.02f,view.y+14,view.width*.96f,view.height-38):view;
        int kind=Tab==0?0:1;
        if(preview!=null&&(previewKind!=kind||previewRect!=render)){preview.Dispose();preview=null;}
        if(preview==null){preview=new TacticalArena(render,true,kind!=0);previewKind=kind;previewRect=render;}
        if(Tab==0)SeasonLobbyArt.Background(view,null,.20f);
        if(Event.current.type==EventType.Repaint)
        {
            if(Tab==0){preview.BeginFrame(-1,-1,-1,false);preview.DrawSeasonLobbyPortrait();preview.SetSeasonLobbyTamer(draft.tamer,Time.unscaledTime,walking);}
            else
            {
                preview.SetField(draft.field);preview.SetLoadoutCamera(false);preview.BeginFrame(-1,-1,-1,false);
                Vector3 target=new Vector3(0,.23f,-3.3f);
                if(walking&&Tab==1)target+=new Vector3(Mathf.Sin(Time.unscaledTime*.8f)*1.15f,0,Mathf.Cos(Time.unscaledTime*.8f)*.4f);
                if(Tab==2){position=target;velocity=Vector3.zero;}else position=Vector3.SmoothDamp(position,target,ref velocity,.22f,3,Time.unscaledDeltaTime);
                preview.SetTamer("preview",draft.tamer,position,target,Mathf.Clamp01(velocity.magnitude),Time.unscaledTime,.15f);
                if(Tab==2)
                {
                    Vector3 rival=new Vector3(0,.23f,2.6f);preview.SetTamer("preview:rival",(draft.tamer+1)%TamerLoadout.Tamers.Length,rival,position,0,Time.unscaledTime);
                    preview.FaceActor("preview",rival-position);preview.FaceActor("preview:rival",position-rival);
                    preview.DrawFinisher(draft.finisher,position,rival,Time.unscaledTime-effectStarted);
                }
            }
            preview.Render();
        }
        if(Tab==0)
        {Vector2 ground=preview.Project(new Vector3(walking?Mathf.Sin(Time.unscaledTime)*.7f:0,.05f,0));BattleHudTheme.GroundShadow(new Rect(ground.x-view.width*.13f,ground.y-12,view.width*.26f,view.height*.066f),.42f);}
        GUI.DrawTexture(render,preview.Texture,ScaleMode.StretchToFill,Tab==0);LobbyVisuals.CinematicShade(view);
        BattleHudTheme.Badge(new Rect(view.x+17,view.y+15,124,25),"●  LIVE PREVIEW",BattleHudTheme.Teal,11);
        if(Tab==2)ArenaPresentationUI.FinishTimeline(new Rect(view.xMax-285,view.y+13,266,48),draft.finisher,Time.unscaledTime-effectStarted);
        SeasonLobbyArt.Shade(new Rect(view.x,view.yMax-172,view.width,172),new Color(.024f,.046f,.071f,.99f),true,true);
        BattleHudTheme.Label(new Rect(view.x+24,view.yMax-119,view.width-48,39),name,30,BattleHudTheme.Text,true);
        BattleHudTheme.Label(new Rect(view.x+25,view.yMax-79,view.width-50,25),Tab==0?TamerLoadout.Family(selected)+"  ·  전장을 함께할 파트너":Tab==1?"당신의 전투가 시작되는 무대":"상대 테이머를 향하는 승리 연출",13,BattleHudTheme.Muted);
        if(ui.Button(new Rect(view.x+24,view.yMax-44,Mathf.Min(204,view.width-48),32),new GUIContent(Tab==2?"처형 다시 재생  ↻":walking?"이동 시연 정지":"이동 시연  ▷"),true,Tab!=2&&walking,12))
        {if(Tab==2)effectStarted=Time.unscaledTime;else walking=!walking;}
    }
    public void Dispose(){if(preview!=null){preview.Dispose();preview=null;}}
}
