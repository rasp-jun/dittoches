using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DigimonTraitUI
{
    static readonly Dictionary<string,Texture2D> icons=new Dictionary<string,Texture2D>();
    static readonly Color[] colors={new Color(1,.55f,.18f),new Color(.34f,.67f,1),new Color(.75f,.55f,1),new Color(.35f,.86f,.45f),new Color(1,.43f,.64f),new Color(1,.86f,.42f),new Color(.96f,.4f,.3f),new Color(.45f,.8f,.9f),new Color(.95f,.65f,.3f),new Color(.7f,.48f,1),new Color(.34f,.92f,.78f)};
    static GUIStyle title,body,small,bold;
    static readonly ArenaInterface ui=new ArenaInterface();
    public static Color ColorFor(string id){int i=System.Array.FindIndex(DigimonBuildCatalog.Data.traits,t=>t.id==id);return colors[Mathf.Clamp(i,0,colors.Length-1)];}
    public static Texture2D Icon(string id)
    {
        Texture2D result;if(icons.TryGetValue(id,out result))return result;
        var c=ColorFor(id);var p=new DigimonEquipmentArt.Painter(c);Color light=Color.Lerp(c,Color.white,.45f);
        switch(id)
        {
            case "courage":p.Poly(c,28,27,22,49,39,68,42,49,57,84,76,53,74,30,50,18);p.Gem(light,50,40,10,17);break;
            case "friendship":p.Ring(c,37,53,21,6);p.Ring(light,64,44,21,6);break;
            case "knowledge":p.Poly(c,17,24,17,72,47,66,50,25);p.Poly(light,53,25,53,66,83,72,83,24);p.Line(c,4,50,18,50,75);break;
            case "sincerity":p.Poly(c,26,27,23,52,42,73,77,83,75,49,56,26);p.Line(light,4,26,18,63,65);p.Line(light,3,40,35,38,56);p.Line(light,3,50,46,69,45);break;
            case "love":p.Poly(c,50,20,18,52,18,68,31,81,44,78,50,68,57,78,70,81,83,67,83,52);p.Line(light,4,26,60,33,70);break;
            case "hope":p.Poly(c,50,85,59,58,83,50,59,43,50,17,42,43,18,50,42,58);p.Gem(light,50,50,8,13);break;
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
    public static bool DrawGuide(Rect canvas,ref string focus,string[] board,string[] bench,DigimonBuildCatalog.Member[] members=null)
    {
        if(title==null){body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};body.normal.textColor=new Color(.87f,.93f,.94f);title=new GUIStyle(body){fontSize=26,fontStyle=FontStyle.Bold};small=new GUIStyle(body){fontSize=13};small.normal.textColor=new Color(.65f,.79f,.82f);bold=new GUIStyle(body){fontStyle=FontStyle.Bold};}
        var e=Event.current;if(e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape){e.Use();return false;}
        ArenaInterface.Fill(canvas,new Color(.004f,.012f,.02f,.9f));Rect r=new Rect(canvas.center.x-640,canvas.center.y-405,1280,810);
        ArenaInterface.Fill(r,new Color(.018f,.038f,.053f));ArenaInterface.Fill(new Rect(r.x,r.y,r.width,3),ArenaInterface.Gold);
        GUI.Label(new Rect(r.x+20,r.y+17,240,40),"시너지 도감",title);
        GUI.Label(new Rect(r.x+20,r.y+62,252,32),"문장 6종 · 전투 역할 5종",small);
        var data=DigimonBuildCatalog.Data.traits;
        for(int i=0;i<data.Length;i++)
        {
            var t=data[i];int n=members==null?DigimonBuildCatalog.Count(t,board):DigimonBuildCatalog.Count(t,members);Rect row=new Rect(r.x+18,r.y+102+i*60,254,53);
            bool chosen=t.id==focus||t.name==focus;ArenaInterface.Fill(row,chosen?new Color(.10f,.19f,.20f):new Color(.032f,.064f,.08f));
            GUI.DrawTexture(new Rect(row.x+5,row.y+5,43,43),Icon(t.id));
            GUI.Label(new Rect(row.x+58,row.y+4,128,27),t.name,bold);
            GUI.Label(new Rect(row.x+58,row.y+29,175,20),t.identity,small);
            GUI.Label(new Rect(row.x+196,row.y+5,55,27),n+"/"+t.Target(n),small);
            if(t.Level(n)>0)ArenaInterface.Fill(new Rect(row.x,row.y,3,row.height),ColorFor(t.id));
            if(GUI.Button(row,GUIContent.none,GUIStyle.none))focus=t.id;
        }
        var trait=DigimonBuildCatalog.Find(focus)??data[0];focus=trait.id;int count=members==null?DigimonBuildCatalog.Count(trait,board):DigimonBuildCatalog.Count(trait,members),tier=trait.Level(count);
        float x=r.x+298,width=954;
        GUI.DrawTexture(new Rect(x,r.y+18,61,61),Icon(trait.id));
        GUI.Label(new Rect(x+77,r.y+18,690,39),trait.name+"  ·  "+trait.identity,title);
        GUI.Label(new Rect(x+78,r.y+60,730,26),trait.category+"  /  전장 "+count+"종  /  "+Next(trait,count),small);
        bool open=!ui.Button(new Rect(r.xMax-114,r.y+19,94,36),new GUIContent("닫기 ESC"));
        var emblemNames=members==null?new string[0]:members.Where(u=>u!=null&&!trait.members.Contains(u.id)&&DigimonBuildCatalog.HasTrait(trait,u.id,u.items)).Select(u=>u.id).Distinct().Select(id=>TeamPlan.Roster.First(u=>u.id==id).name).ToArray();
        GUI.Label(new Rect(x,r.y+103,width,75),trait.description+(emblemNames.Length>0?"\n인장으로 참여: "+string.Join(" · ",emblemNames):""),body);
        for(int i=0;i<trait.tiers.Length;i++)
        {
            Rect row=new Rect(x,r.y+196+i*72,width,63);bool active=tier==i+1;
            ArenaInterface.Fill(row,active?new Color(.13f,.19f,.17f):new Color(.035f,.065f,.08f));
            ArenaInterface.Fill(new Rect(row.x,row.y,4,row.height),active?ColorFor(trait.id):new Color(.17f,.28f,.32f));
            GUI.Label(new Rect(row.x+17,row.y+10,120,48),trait.tiers[i].count+"종 · "+(active?"적용 중":count>=trait.tiers[i].count?"달성":"목표"),bold);
            GUI.Label(new Rect(row.x+153,row.y+10,row.width-169,47),trait.tiers[i].text,body);
        }
        GUI.Label(new Rect(x,r.y+423,width,30),"소속 디지몬  ·  초록: 전장  /  파랑: 대기석  /  회색: 미보유",small);
        for(int i=0;i<trait.members.Length;i++)
        {
            string id=trait.members[i];var def=TeamPlan.Roster.First(u=>u.id==id);bool deployed=board.Contains(id),held=bench.Contains(id);
            Rect card=new Rect(x+i%3*320,r.y+462+i/3*105,310,94);
            ArenaInterface.Fill(card,new Color(.035f,.069f,.085f));ArenaInterface.Fill(new Rect(card.x,card.y,3,card.height),deployed?new Color(.3f,.9f,.58f):held?new Color(.3f,.7f,1):new Color(.26f,.34f,.38f));
            FaithfulPortraits.Draw(new Rect(card.x+7,card.y+8,73,75),id,true);
            GUI.Label(new Rect(card.x+89,card.y+9,210,32),def.name,bold);
            GUI.Label(new Rect(card.x+89,card.y+44,210,32),def.cost+"G · "+TeamPlan.Progress(id,board,bench),small);
        }
        GUI.Label(new Rect(x,r.y+688,width,56),"구성 방향  ·  "+trait.usage,body);
        GUI.Label(new Rect(x,r.y+745,width,59),"서로 다른 종류만 계산 · 대기석 제외 · 최고 단계만 적용\n전투 인원은 시작 시 확정 · 인장 장착/회수에 따른 시너지 변화는 전투 중에도 즉시 반영",small);
        if(e.isMouse||e.isKey)e.Use();return open;
    }
}
