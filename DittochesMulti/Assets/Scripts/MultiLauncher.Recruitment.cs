using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    void SetOnlineShopLock(bool locked)
    {Send("/action",new Command{action="shop_lock",shopLocked=locked});}
    void DrawOnlineShopLock(Player me,Room room,bool fresh)
    {
        bool available=fresh&&!busy&&(room.phase=="prepare"||room.phase=="battle");
        string tip=RecruitmentAdvice.ShopLockHint;
        if(onlineInterface.Button(new Rect(1115,782,150,28),new GUIContent(me.shopLocked?"잠금 유지 중":"상점 잠금",tip),available,me.shopLocked,13))SetOnlineShopLock(!me.shopLocked);
        if(me.shopLocked)GUI.Label(new Rect(788,789,318,24),"다음 라운드에도 이 모집 목록 유지",new GUIStyle(small){fontSize=12});
    }

    FormationForecast.Plan OnlineFormationForecast(Player me,bool editable)
    {
        if(artPack!=0||!editable||!GUI.enabled||busy||onlineItem>=0||selectedSlot<0||arena==null)return null;
        int seat=arena.HitBench(Event.current.mousePosition),cell=arena.HitCell(Event.current.mousePosition);
        if(seat<0&&cell<28)return null;
        var board=new string[28];var bench=new string[9];
        foreach(var unit in me.board)board[unit.slot]=unit.id;
        foreach(var unit in me.bench)bench[unit.slot]=unit.id;
        return FormationForecast.Preview(board,bench,selectedArea=="board",selectedSlot,seat<0,seat>=0?seat:cell-28,me.level);
    }
    void OpenOnlineShopSkill(UnitDef definition)
    {onlineSkillId=definition.id;onlineSkillArea="shop";onlineSkillSlot=-1;onlineItemGuide=-1;arenaPointer.Reset();}
    void DrawOnlineRecruitCard(Rect r,string id,int slot,Player me,bool editable)
    {
        var def=Def(id);float hover=onlineInterface.HoverAmount(r,GUI.enabled);
        Color rarity=def==null?new Color(.15f,.25f,.3f):ArenaInterface.Rarity(def.cost);
        Card(r,Color.Lerp(ArenaInterface.Ink,rarity,.08f+hover*.12f),Color.Lerp(new Color(.15f,.25f,.3f),rarity,hover));
        if(def==null){GUI.Label(r,"모집 완료",centered);return;}
        var skill=DigimonSkillCatalog.Find(id);bool afford=me.gold>=def.cost;
        int singles=me.board.Concat(me.bench).Count(u=>u.id==id&&u.star==1);
        int doubles=me.board.Concat(me.bench).Count(u=>u.id==id&&u.star==2);
        bool merge=singles>=2,space=me.bench.Length<9||merge;
        var caption=new GUIStyle(small){fontSize=12};caption.normal.textColor=new Color(.76f,.85f,.86f);
        var nameStyle=new GUIStyle(caption){fontSize=def.name.Length>8?11:14,fontStyle=FontStyle.Bold};nameStyle.normal.textColor=new Color(.92f,.96f,.95f);
        CharacterCardArt.Stage(new Rect(r.x+1,r.y+1,r.width-2,83),def.cost);
        CharacterCardArt.Border(r,def.cost,merge,hover);
        GUI.Label(new Rect(r.x+8,r.y+5,r.width-49,22),def.name,nameStyle);
        GUI.Label(new Rect(r.xMax-37,r.y+5,30,22),def.cost+"G",caption);
        if(FaithfulPortraits.Get(id,true)!=null)FaithfulPortraits.Draw(new Rect(r.x+2,r.y+27,74,57),id,true);
        else Portrait(new Rect(r.x+6,r.y+29,52,55),id);
        GUI.Label(new Rect(r.x+78,r.y+29,98,22),skill.ScalingRole.Replace(" 딜러","")+" · "+skill.attackRange+"칸",caption);
        GUI.Label(new Rect(r.x+78,r.y+49,98,35),string.Join(" · ",DigimonBuildCatalog.ForUnit(id).Select(t=>t.name).ToArray()),new GUIStyle(caption){fontSize=11});
        string hint=!editable?(me.ready?"준비 해제 후 모집":"준비 단계에 모집"):!afford?"골드 부족":!space?"대기석 가득":RecruitmentAdvice.Copies(singles,doubles);
        GUI.Label(new Rect(r.x+8,r.y+87,133,20),hint,new GUIStyle(caption){fontSize=11});
        // Inspection is available even when a purchase is disabled.
        Rect icon=new Rect(r.xMax-35,r.y+73,28,28);
        if(DigimonSkillUI.DrawIcon(icon,skill))OpenOnlineShopSkill(def);
        if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&r.Contains(Event.current.mousePosition))
        {OpenOnlineShopSkill(def);Event.current.Use();}
        TeamPlannerUI.ShopMarker(new Rect(r.x+5,r.y+65,46,18),id,new GUIStyle(caption){fontSize=11,alignment=TextAnchor.MiddleCenter});
        string tip=(TeamPlan.Contains(id)?"[팀 계획 목표]\n":"")+def.name+" · "+def.cost+"G\n"+skill.ScalingRole+" · 기본 공격 "+skill.attackRange+"칸\n"+
            RecruitmentAdvice.Copies(singles,doubles)+"\n"+RecruitmentAdvice.Spend(me.gold,def.cost)+"\n우클릭: 유닛 / 스킬 정보";
        GUI.Label(r,new GUIContent("",tip),GUIStyle.none);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&editable&&!busy&&afford&&space;
        if(GUI.Button(r,new GUIContent("",tip),GUIStyle.none))Send("/action",new Command{action="buy",slot=slot});
        GUI.enabled=enabled;
    }
}
