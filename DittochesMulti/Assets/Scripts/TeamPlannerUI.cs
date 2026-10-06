using System;
using System.Linq;
using UnityEngine;

public sealed class TeamPlannerUI
{
    readonly ArenaInterface ui=new ArenaInterface();
    GUIStyle heading,text,small,input,slotLabel,cardName,caption;
    string query="",undo="",notice="변경 사항은 자동 저장됩니다";
    int cost;
    public string Query {get{return query;}set{query=value;}}
    public int Cost {get{return cost;}set{cost=Mathf.Clamp(value,0,5);}}
    void Remember(){undo=JsonUtility.ToJson(TeamPlan.Data);}
    void Toggle(string id){Remember();notice=TeamPlan.Toggle(id)?"팀 계획 저장 완료":"최대 9종입니다 · 먼저 목표를 하나 해제하세요";}
    public bool Draw(Rect canvas,string[] board,string[] bench,int level,bool inMatch)
    {
        if(heading==null)
        {
            text=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};text.normal.textColor=new Color(.87f,.93f,.94f);
            heading=new GUIStyle(text){fontSize=24,fontStyle=FontStyle.Bold};small=new GUIStyle(text){fontSize=12};small.normal.textColor=new Color(.65f,.78f,.8f);
            slotLabel=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};cardName=new GUIStyle(small){fontStyle=FontStyle.Bold};caption=new GUIStyle(small){fontSize=11};
            input=new GUIStyle(GUI.skin.textField){fontSize=17,padding=new RectOffset(10,10,7,7)};
        }
        bool close=false;Event e=Event.current;
        if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){GUI.FocusControl(null);e.Use();return false;}
        ArenaInterface.Fill(canvas,new Color(.005f,.012f,.025f,.88f));
        Rect r=new Rect(canvas.center.x-730,canvas.center.y-430,1460,860);
        ArenaInterface.Fill(r,new Color(.018f,.038f,.052f));ArenaInterface.Fill(new Rect(r.x,r.y,r.width,3),ArenaInterface.Gold);
        GUI.Label(new Rect(r.x+20,r.y+18,150,38),"팀 계획",heading);
        for(int i=0;i<3;i++)if(ui.Button(new Rect(r.x+188+i*132,r.y+18,122,36),new GUIContent("플랜 "+(i+1)),true,TeamPlan.Data.active==i))
        {Remember();TeamPlan.Data.active=i;TeamPlan.Save();notice="상점 목표를 플랜 "+(i+1)+"로 변경했습니다";GUI.FocusControl(null);}
        GUI.Label(new Rect(r.x+610,r.y+23,660,30),inMatch?"목표 디지몬과 시너지를 준비하세요 · 전투와 대기 시간은 멈추지 않습니다":"나만의 팀 3개를 저장하고 게임 중에도 전환할 수 있습니다",small);
        if(ui.Button(new Rect(r.xMax-115,r.y+18,95,36),new GUIContent("닫기  ESC")))close=true;
        var ids=TeamPlan.Units.ToArray();
        for(int i=0;i<9;i++)
        {
            Rect slot=new Rect(r.x+20+i*158,r.y+76,150,84);ArenaInterface.Fill(slot,new Color(.033f,.069f,.085f));
            if(i>=ids.Length){GUI.Label(slot,"+ 목표 "+(i+1),slotLabel);continue;}
            string id=ids[i];var def=TeamPlan.Roster.First(u=>u.id==id);
            FaithfulPortraits.Draw(new Rect(slot.x+3,slot.y+5,54,65),id,true);
            GUI.Label(new Rect(slot.x+59,slot.y+7,88,38),def.name,small);
            GUI.Label(new Rect(slot.x+59,slot.y+49,88,29),inMatch?TeamPlan.Progress(id,board,bench):def.cost+" G · 해제 ×",small);
            ArenaInterface.Fill(new Rect(slot.x,slot.yMax-3,slot.width,3),inMatch&&board.Contains(id)?new Color(.3f,.87f,.6f):ArenaInterface.Rarity(def.cost));
            if(GUI.Button(slot,new GUIContent("","클릭하면 목표에서 해제"),GUIStyle.none))Toggle(id);
        }
        GUI.Label(new Rect(r.x+20,r.y+169,1100,25),"목표 "+ids.Length+" / 9종   ·   "+(inMatch?TeamPlan.Summary(board.Concat(bench))+"   ·   현재 배치 한도 "+level+"명":"최대 9종을 선택할 수 있습니다")+"   ·   계획만 저장되며 유닛을 구매하거나 배치하지 않습니다",small);
        GUI.SetNextControlName("team-plan-search");query=GUI.TextField(new Rect(r.x+20,r.y+202,285,37),query,30,input);
        if(string.IsNullOrEmpty(query)&&GUI.GetNameOfFocusedControl()!="team-plan-search")GUI.Label(new Rect(r.x+32,r.y+210,260,26),"이름 · 시너지 검색",small);
        for(int i=0;i<=5;i++)if(ui.Button(new Rect(r.x+325+i*85,r.y+202,76,37),new GUIContent(i==0?"전체":i+" G"),true,cost==i)){cost=i;GUI.FocusControl(null);}
        if(ui.Button(new Rect(r.x+864,r.y+202,180,37),new GUIContent("검색 초기화"))){query="";cost=0;GUI.FocusControl(null);}
        var matches=TeamPlan.Search(query,cost);
        for(int i=0;i<matches.Length;i++)
        {
            var def=matches[i];Rect card=new Rect(r.x+20+i%5*213,r.y+253+i/5*87,202,78);bool chosen=TeamPlan.Contains(def.id);
            ArenaInterface.Fill(card,chosen?new Color(.1f,.19f,.19f):new Color(.028f,.062f,.08f));
            ArenaInterface.Fill(new Rect(card.x,card.y,3,card.height),chosen?ArenaInterface.Gold:ArenaInterface.Rarity(def.cost));
            FaithfulPortraits.Draw(new Rect(card.x+5,card.y+8,58,63),def.id,true);
            GUI.Label(new Rect(card.x+68,card.y+6,131,32),def.name,cardName);
            GUI.Label(new Rect(card.x+68,card.y+38,131,24),string.Join(" · ",DigimonBuildCatalog.ForUnit(def.id).Select(t=>t.name).ToArray()),caption);
            GUI.Label(new Rect(card.x+68,card.y+59,131,18),def.cost+"G · "+(chosen?"목표 선택됨":inMatch?TeamPlan.Progress(def.id,board,bench):def.role),caption);
            if(GUI.Button(card,GUIContent.none,GUIStyle.none))Toggle(def.id);
        }
        if(matches.Length==0)GUI.Label(new Rect(r.x+30,r.y+300,980,70),"검색 결과가 없습니다. 이름이나 시너지를 바꾸거나 필터를 초기화하세요.",text);
        Rect side=new Rect(r.x+1100,r.y+253,340,513);ArenaInterface.Fill(side,new Color(.028f,.059f,.073f));
        GUI.Label(new Rect(side.x+17,side.y+12,300,31),"계획 시너지",heading);
        GUI.Label(new Rect(side.x+17,side.y+47,300,38),"현재 전장 → 목표 전체 배치 시\n같은 디지몬은 한 번만 계산합니다",small);
        var traits=DigimonBuildCatalog.Data.traits.Where(t=>TeamPlan.Units.Any(id=>t.members.Contains(id)))
            .OrderByDescending(t=>t.Level(DigimonBuildCatalog.Count(t,TeamPlan.Units))).ThenByDescending(t=>DigimonBuildCatalog.Count(t,TeamPlan.Units)).ToArray();
        for(int i=0;i<traits.Length;i++)
        {
            var t=traits[i];int planned=DigimonBuildCatalog.Count(t,TeamPlan.Units),present=DigimonBuildCatalog.Count(t,board),tier=t.Level(planned);float y=side.y+96+i*37;
            GUI.DrawTexture(new Rect(side.x+13,y+2,24,24),DigimonTraitUI.Icon(t.id));
            GUI.Label(new Rect(side.x+44,y+2,140,29),t.name,small);
            GUI.Label(new Rect(side.x+184,y+2,146,29),(inMatch?present+" → ":"")+planned+" / "+t.Target(planned)+(tier>0?"  "+tier+"단계":""),small);
            string tip=t.name+" · 목표 "+planned+"종\n"+(tier>0?t.tiers[tier-1].text:"아직 활성화되지 않습니다")+"\n다음 기준 "+t.Target(planned)+"종 · "+t.description;
            GUI.Label(new Rect(side.x+14,y,308,32),new GUIContent("",tip),GUIStyle.none);
        }
        if(traits.Length==0)GUI.Label(new Rect(side.x+17,side.y+107,300,90),"왼쪽에서 목표 디지몬을 선택하면\n완성할 수 있는 시너지가 표시됩니다.",small);
        if(ui.Button(new Rect(r.x+20,r.yMax-63,145,39),new GUIContent("현재 플랜 비우기"),ids.Length>0)){Remember();TeamPlan.Units.Clear();TeamPlan.Save();notice="플랜을 비웠습니다 · 이전 변경 취소로 복구할 수 있습니다";}
        if(ui.Button(new Rect(r.x+179,r.yMax-63,160,39),new GUIContent("이전 변경 취소"),undo.Length>0)){TeamPlan.Restore(undo);undo="";notice="이전 계획을 복구했습니다";}
        if(ui.Button(new Rect(r.x+353,r.yMax-63,175,39),new GUIContent("현재 전장 담기","배치한 디지몬을 현재 플랜에 저장합니다. 중복 디지몬은 한 번만 포함합니다."),inMatch&&board.Length>0))
        {Remember();TeamPlan.Capture(board);notice="현재 전장을 팀 계획에 저장했습니다";}
        GUI.Label(new Rect(r.x+552,r.yMax-60,888,48),notice+"\n목표는 상점에서 표시됩니다 · 배치 한도보다 큰 계획은 레벨업 후 완성하세요",small);
        ui.Tooltip(canvas);
        if(e.isMouse||e.isKey)e.Use();return !close;
    }
    public static void ShopMarker(Rect rect,string id,GUIStyle style)
    {
        if(!TeamPlan.Contains(id))return;
        ArenaInterface.Fill(rect,new Color(.075f,.25f,.25f,.97f));
        GUI.Label(rect,"목표",style);
    }
}
