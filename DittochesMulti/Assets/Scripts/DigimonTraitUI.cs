using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static partial class DigimonTraitUI
{
    static readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
    static readonly string[] colorIds={"courage","friendship","knowledge","sincerity","love","hope","fighter","guardian","artillery","caster","harmonizer"};
    static readonly Color[] colors={new Color(1,.55f,.18f),new Color(.34f,.67f,1),new Color(.75f,.55f,1),new Color(.35f,.86f,.45f),new Color(1,.36f,.16f),new Color(.65f,.42f,.9f),new Color(.96f,.4f,.3f),new Color(.45f,.8f,.9f),new Color(.95f,.65f,.3f),new Color(.7f,.48f,1),new Color(.34f,.92f,.78f)};
    static GUIStyle title,body,small,bold,number,cardName;
    static readonly ArenaInterface ui=new ArenaInterface();
    static DigimonBuildCatalog.Member[] cachedBoard,cachedBench;
    static DigimonTraitOverview[] overviews;
    static bool cachedBenchKnown;
    static string scrollFocus,scrollContext;
    static Vector2 memberScroll;
    public static Rect GuideBounds(Rect canvas){return new Rect(canvas.center.x-640,canvas.center.y-405,1280,810);}
    public static Rect GuideCloseRect(Rect canvas){var r=GuideBounds(canvas);return new Rect(r.xMax-114,r.y+19,94,36);}
    public static Color ColorFor(string id){if(id=="light")return new Color(1,.94f,.69f);if(id=="machine")return new Color(.55f,.78f,.92f);if(id=="knowledge")return new Color(.6f,.84f,.3f);int i=System.Array.IndexOf(colorIds,id);return colors[Mathf.Clamp(i,0,colors.Length-1)];}
    public static Texture2D Icon(string id)
    {
        Texture2D result;if(icons.TryGetValue(id,out result))return result;
        var c=ColorFor(id);var p=new DigimonEquipmentArt.Painter(c);Color light=Color.Lerp(c,Color.white,.45f);
        switch(id)
        {
            case "courage":p.Poly(c,23,20,27,56,19,82,43,68,64,78,60,61,82,49,75,31,48,28,39,16);p.Poly(light,53,50,74,47,64,37,46,36);p.Gem(Color.white,47,57,4,4);break;
            case "friendship":p.Poly(c,23,27,31,48,49,59,68,48,78,26,61,19,49,24,37,19);p.Gem(light,23,63,8,12);p.Gem(light,42,78,8,12);p.Gem(light,62,78,8,12);p.Gem(light,80,63,8,12);break;
            case "machine":p.Ring(c,50,50,26,10);for(int tooth=0;tooth<8;tooth++){float a=tooth*Mathf.PI/4;p.Line(c,10,50+Mathf.Cos(a)*24,50+Mathf.Sin(a)*24,50+Mathf.Cos(a)*39,50+Mathf.Sin(a)*39);}p.Gem(light,50,50,10,13);break;
            case "knowledge":p.Poly(c,50,18,30,33,28,58,39,76,61,76,72,58,70,33);p.Line(light,4,50,20,50,74);p.Line(c,5,38,70,30,87);p.Line(c,5,62,70,70,87);for(int leg=0;leg<3;leg++){float y=31+leg*15;p.Line(light,4,30,y,15,y-7);p.Line(light,4,70,y,85,y-7);}break;
            case "sincerity":p.Poly(c,26,27,23,52,42,73,77,83,75,49,56,26);p.Line(light,4,26,18,63,65);p.Line(light,3,40,35,38,56);p.Line(light,3,50,46,69,45);break;
            case "love":p.Poly(c,28,27,22,49,39,68,42,49,57,84,76,53,74,30,50,18);p.Gem(light,50,40,10,17);break;
            case "hope":p.Poly(c,19,80,40,65,60,65,81,80,74,42,61,25,50,16,39,25,26,42);p.Poly(light,28,52,45,45,41,35);p.Poly(light,72,52,55,45,59,35);p.Gem(light,50,62,5,8);break;
            case "light":p.Ring(c,50,51,23,5);p.Gem(light,50,51,10,16);for(int ray=0;ray<8;ray++){float a=ray*Mathf.PI/4;p.Line(light,4,50+Mathf.Cos(a)*31,51+Mathf.Sin(a)*31,50+Mathf.Cos(a)*41,51+Mathf.Sin(a)*41);}break;
            case "fighter":p.Line(c,9,25,25,75,78);p.Line(light,9,75,25,25,78);p.Line(light,5,17,37,36,18);p.Line(c,5,64,18,84,38);break;
            case "guardian":p.Poly(c,18,75,50,85,82,75,76,43,50,16,24,43);p.Poly(light,50,75,70,68,64,43,50,28);break;
            case "artillery":for(int i=0;i<3;i++){float x=29+i*21;p.Poly(i==1?light:c,x-7,24,x-7,62,x,77,x+7,62,x+7,24);p.Line(light,3,x-8,34,x+8,34);}break;
            case "caster":p.Line(c,7,34,18,57,68);p.Ring(light,59,69,16,5);p.Gem(c,59,69,6,10);p.Gem(light,25,66,4,8);break;
            default:p.Line(c,4,50,75,23,29);p.Line(c,4,23,29,77,29);p.Line(c,4,77,29,50,75);p.Ring(light,50,75,10,5);p.Ring(light,23,29,10,5);p.Ring(light,77,29,10,5);p.Gem(c,50,46,7,10);break;
        }
        result=p.Finish(100+icons.Count);result.name="Synergy "+id;icons[id]=result;return result;
    }
    public static string Next(DigimonBuildCatalog.Trait trait,int count)
    {return trait.Level(count)==trait.tiers.Length?"최대 단계":(trait.Target(count)-count)+"종 더 배치하면 "+(trait.Level(count)+1)+"단계";}
    static void HandleMemberWheel(Rect viewport,float contentHeight)
    {
        float overflow=Mathf.Max(0,contentHeight-viewport.height);
        memberScroll.y=Mathf.Clamp(memberScroll.y,0,overflow);
        var wheel=Event.current;
        if(!GUI.enabled||wheel.type!=EventType.ScrollWheel||overflow<=0||!viewport.Contains(wheel.mousePosition)||Mathf.Approximately(wheel.delta.y,0))return;
        // Resolve the input in the guide's coordinate space before entering the scroll clip.
        memberScroll.y=Mathf.Clamp(memberScroll.y+wheel.delta.y*20f,0,overflow);
        wheel.Use();
    }
    public static bool DrawGuide(Rect canvas,ref string focus,string[] board,string[] bench,DigimonBuildCatalog.Member[] members=null,DigimonBuildCatalog.Member[] benchMembers=null,string context="내 전장",bool benchKnown=true,bool plannedFormation=false)
    {
        if(title==null){body=new GUIStyle(GUI.skin.label){fontSize=15,wordWrap=true};body.normal.textColor=BattleHudTheme.Text;title=new GUIStyle(body){fontSize=26,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=12};small.normal.textColor=BattleHudTheme.Muted;bold=new GUIStyle(body){fontStyle=FontStyle.Bold};number=new GUIStyle(title){fontSize=23};cardName=new GUIStyle(bold){fontSize=15,wordWrap=false,clipping=TextClipping.Clip};}
        var e=Event.current;if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){e.Use();return false;}
        members=members??(board??new string[0]).Select(id=>new DigimonBuildCatalog.Member(id)).ToArray();
        benchMembers=benchMembers??(bench??new string[0]).Select(id=>new DigimonBuildCatalog.Member(id)).ToArray();
        if(overviews==null||cachedBenchKnown!=benchKnown||!DigimonTraitOverview.SameMembers(cachedBoard,members)||!DigimonTraitOverview.SameMembers(cachedBench,benchMembers))
        {
            cachedBoard=members.Select(u=>u==null?null:new DigimonBuildCatalog.Member(u.id,u.items)).ToArray();
            cachedBench=benchMembers.Select(u=>u==null?null:new DigimonBuildCatalog.Member(u.id,u.items)).ToArray();cachedBenchKnown=benchKnown;
            overviews=DigimonBuildCatalog.Data.traits.Select(t=>DigimonTraitOverview.Create(t,cachedBoard,cachedBench,benchKnown)).ToArray();
        }
        ArenaInterface.Fill(canvas,new Color(.004f,.012f,.02f,.9f));Rect r=GuideBounds(canvas);
        BattleHudTheme.Surface(r,false,BattleHudTheme.Teal);
        BattleHudTheme.Round(new Rect(r.x+9,r.y+9,271,r.height-18),BattleHudTheme.Background);
        BattleHudTheme.Label(new Rect(r.x+24,r.y+17,236,17),"SYNERGY ARCHIVE",10,BattleHudTheme.Teal,true);
        GUI.Label(new Rect(r.x+21,r.y+39,240,36),"조합을 완성하는 힘",number);
        GUI.Label(new Rect(r.x+23,r.y+77,246,21),"계열 "+DigimonBuildCatalog.Data.traits.Count(t=>t.category=="계열")+"종 · 역할 "+DigimonBuildCatalog.Data.traits.Count(t=>t.category=="전투")+"종",small);
        var data=DigimonBuildCatalog.Data.traits;
        for(int i=0;i<data.Length;i++)
        {
            var t=data[i];var overview=overviews[i];int n=overview.count;float rowStep=Mathf.Min(60,(r.height-118)/data.Length);Rect row=new Rect(r.x+18,r.y+102+i*rowStep,254,rowStep-7);
            bool chosen=t.id==focus||t.name==focus;BattleHudTheme.Round(row,chosen?new Color(.06f,.18f,.21f):BattleHudTheme.Panel);
            GUI.DrawTexture(new Rect(row.x+5,row.y+3,40,40),Icon(t.id));
            GUI.Label(new Rect(row.x+58,row.y+4,128,27),t.name,bold);
            string progress=overview.Maximum?"최대 단계":overview.tier>0?overview.tier+"단계 · 다음까지 "+overview.missing+"종":"활성까지 "+overview.missing+"종";
            GUI.Label(new Rect(row.x+58,row.y+26,188,18),progress,small);
            GUI.Label(new Rect(row.x+196,row.y+5,55,27),n+"/"+t.Target(n),small);
            if(chosen||overview.tier>0)BattleHudTheme.Round(new Rect(row.x,row.y+10,3,row.height-20),chosen?BattleHudTheme.Teal:ColorFor(t.id));
            if(GUI.Button(row,GUIContent.none,GUIStyle.none))focus=t.id;
        }
        var trait=DigimonBuildCatalog.Find(focus)??data[0];focus=trait.id;var current=overviews[System.Array.IndexOf(data,trait)];int count=current.count,tier=current.tier;
        if(scrollFocus!=focus||scrollContext!=context){scrollFocus=focus;scrollContext=context;memberScroll=Vector2.zero;}
        float x=r.x+298,width=954;
        BattleHudTheme.Round(new Rect(x,r.y+18,66,66),Color.Lerp(BattleHudTheme.Raised,ColorFor(trait.id),.13f));
        GUI.DrawTexture(new Rect(x+3,r.y+21,60,60),Icon(trait.id));
        BattleHudTheme.Label(new Rect(x+82,r.y+18,680,18),trait.category+"  /  "+context,10,BattleHudTheme.Teal,true);
        GUI.Label(new Rect(x+79,r.y+40,690,39),trait.name+"  ·  "+trait.identity,title);
        bool open=!ui.Button(GuideCloseRect(canvas),new GUIContent("닫기 ESC"));
        BattleHudTheme.Round(new Rect(x,r.y+99,width,94),BattleHudTheme.Raised);
        GUI.Label(new Rect(x+15,r.y+109,422,32),(plannedFormation?"목표 ":"참여 ")+count+"종  ·  "+(tier>0?tier+"단계 "+(plannedFormation?"예상":"활성"):"미활성"),number);
        for(int i=0;i<trait.tiers.Length;i++)
        {
            float tierStep=506f/trait.tiers.Length;Rect step=new Rect(x+448+i*tierStep,r.y+112,tierStep-12,27);bool reached=count>=trait.tiers[i].count;
            BattleHudTheme.Round(new Rect(step.x,step.y+24,step.width,3),reached?ColorFor(trait.id):BattleHudTheme.Line);
            BattleHudTheme.Label(step,trait.tiers[i].count+"종 · "+(i+1)+"단계"+(tier==i+1?(plannedFormation?" 예상":" 적용"):""),trait.tiers.Length>3?10:12,reached?ColorFor(trait.id):BattleHudTheme.Muted,true);
        }
        GUI.Label(new Rect(x+15,r.y+146,width-30,22),plannedFormation?(current.Maximum?"목표 전체 배치 시 최대 단계 달성":(tier+1)+"단계까지 목표에 서로 다른 "+current.missing+"종 더 필요"):current.Progress,bold);
        GUI.Label(new Rect(x+15,r.y+171,width-30,20),plannedFormation?"목표 전체를 배치했을 때의 기본 소속 기준 · 실제 보유/배치/인장 효과는 포함하지 않음":current.duplicates>0?"동일 종류 "+current.duplicates+"명은 인원에서 제외 · 인장도 같은 종류끼리는 1종으로 계산":"다른 참여종을 빼지 않고 배치할 때 증가 · 대기석과 성급은 시너지 인원에 포함되지 않음",small);
        GUI.Label(new Rect(x,r.y+204,width,43),trait.id=="love"?trait.description.Replace(" 자신도","\n자신도"):trait.description,body);
        for(int i=0;i<trait.tiers.Length;i++)
        {
            float effectStep=177f/trait.tiers.Length;Rect row=new Rect(x,r.y+255+i*effectStep,width,effectStep-5);bool active=tier==i+1;
            BattleHudTheme.Round(row,active?Color.Lerp(BattleHudTheme.Raised,ColorFor(trait.id),.13f):BattleHudTheme.Panel);
            BattleHudTheme.Round(new Rect(row.x+1,row.y+11,3,row.height-22),active?ColorFor(trait.id):BattleHudTheme.Line);
            BattleHudTheme.Label(new Rect(row.x+17,row.y+6,120,23),trait.tiers[i].count+"종 필요",15,active?ColorFor(trait.id):BattleHudTheme.Text,true);
            if(trait.tiers.Length<=3)BattleHudTheme.Label(new Rect(row.x+18,row.y+31,120,14),active?(plannedFormation?"전체 배치 시 적용":"현재 적용 중"):count>=trait.tiers[i].count?"달성한 단계":"다음 목표",10,active?ColorFor(trait.id):BattleHudTheme.Muted);
            GUI.Label(new Rect(row.x+153,row.y+5,row.width-169,row.height-8),trait.tiers[i].text,body);
        }
        GUI.Label(new Rect(x,r.y+438,width,26),plannedFormation?current.Scope.Replace("인장으로 참여한 디지몬 포함","계획에는 기본 소속만 계산"):current.Scope,small);
        BattleHudTheme.Label(new Rect(x,r.y+464,390,26),plannedFormation?"참여 디지몬  ·  기본 소속 기준":"참여 디지몬  ·  인장 참여 포함",13,BattleHudTheme.Text,true);
        BattleHudTheme.Badge(new Rect(x+537,r.y+465,117,23),plannedFormation?"목표에 포함":"전장 기여",BattleHudTheme.Teal,11);
        if(!plannedFormation)BattleHudTheme.Badge(new Rect(x+664,r.y+465,117,23),"대기석 후보",new Color(.4f,.68f,1),11);
        BattleHudTheme.Badge(new Rect(x+791,r.y+465,163,23),plannedFormation?"목표 밖 · 추가 후보":"미보유 · 모집 필요",BattleHudTheme.Muted,11);
        Rect viewport=new Rect(x,r.y+494,width,181);float contentHeight=Mathf.Max(viewport.height,Mathf.CeilToInt(current.entries.Length/3f)*90);
#if DITTOCHES_PORTABLE_PREVIEW
        Event reviewWheelEvent=ReviewBeginWheel(viewport,contentHeight);
#endif
        HandleMemberWheel(viewport,contentHeight);
        memberScroll=GUI.BeginScrollView(viewport,memberScroll,new Rect(0,0,width-20,contentHeight),false,false);
        for(int i=0;i<current.entries.Length;i++)
        {
            var entry=current.entries[i];var def=TeamPlan.Roster.FirstOrDefault(u=>u.id==entry.id);
            Rect card=new Rect(i%3*312,i/3*90,304,83);
            Color statusTint=entry.deployed>0?BattleHudTheme.Teal:entry.held>0?new Color(.4f,.68f,1):BattleHudTheme.Muted;
            BattleHudTheme.Round(card,BattleHudTheme.Raised);BattleHudTheme.Round(new Rect(card.x+2,card.y+12,3,card.height-24),statusTint);
            FaithfulPortraits.Draw(new Rect(card.x+6,card.y+6,67,71),entry.id,true);
            GUI.Label(new Rect(card.x+81,card.y+6,218,25),def==null?entry.id:def.name,cardName);
            string status=plannedFormation?(entry.deployed>0?"목표에 포함 · 배치 시 1종 참여":"목표 밖 후보 · 추가 시 +1종"):entry.deployed>0?"전장 "+entry.deployed+"명 · 1종 참여":entry.held>0?"대기석 "+entry.held+"명 · 배치 시 +1종":benchKnown?"모집 필요":"대기석 확인 불가";
            BattleHudTheme.Label(new Rect(card.x+84,card.y+34,212,20),status,11,statusTint);
            GUI.Label(new Rect(card.x+81,card.y+56,218,21),(def==null?"":def.cost+"G · ")+(entry.innate?"기본 소속":"인장으로 참여"),small);
        }
        // Wheel input is resolved once above in guide coordinates. Unity's native
        // fallback must not handle it again using its separately cached pointer.
        GUI.EndScrollView(false);
#if DITTOCHES_PORTABLE_PREVIEW
        ReviewEndWheel(reviewWheelEvent,viewport,contentHeight);
#endif
        BattleHudTheme.Round(new Rect(x,r.y+681,width,115),BattleHudTheme.Raised);
        BattleHudTheme.Label(new Rect(x+14,r.y+689,width-28,18),"전략 가이드",10,BattleHudTheme.Teal,true);
        GUI.Label(new Rect(x+12,r.y+710,width-24,40),trait.usage,body);
        GUI.Label(new Rect(x+12,r.y+751,width-24,44),plannedFormation?"도달한 최고 단계만 적용 · 낮은 단계와 중첩되지 않음\n계획 미리보기 · 실제 편성과 목표는 유지됩니다 · ESC / 바깥 클릭: 팀 계획으로 돌아가기":"현재 도달한 최고 단계만 적용 · 낮은 단계와 중첩되지 않음\n전투 인원은 시작 시 확정 · 쓰러져도 유지 · 인장 장착/회수에 따른 시너지 변경은 전투 중 즉시 반영",small);
        if(e.isMouse||e.isKey)e.Use();return open;
    }
}
