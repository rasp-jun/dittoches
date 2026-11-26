using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    PurchaseForecast PreviewOnlinePurchase(Player me,string id,Room room=null)
    {
        var pieces=me.board.Select(u=>new PurchaseForecast.Piece(u.id,true,u.slot,u.star,u.items))
            .Concat(me.bench.Select(u=>new PurchaseForecast.Piece(u.id,false,u.slot,u.star,u.items)));
        var preview=PurchaseForecast.Create(id,pieces,9,me.gold,Def(id).cost,me.shop.Contains(id));
        if(me.eliminated){preview.available=false;preview.reason="탈락 후에는 모집할 수 없습니다";}
        else if(room!=null&&room.phase=="prepare"&&me.ready){preview.available=false;preview.reason="준비 완료를 해제하면 모집할 수 있습니다";}
        else if(room!=null&&room.phase!="prepare"&&room.phase!="battle"){preview.available=false;preview.reason="지금은 모집 단계가 아닙니다";}
        return preview;
    }
    void SetOnlineShopLock(bool locked)
    {Send("/action",new Command{action="shop_lock",shopLocked=locked});}
    void DrawOnlineShopLock(Player me,Room room,bool fresh)
    {
        BattleHudTheme.Surface(new Rect(302,782,972,151));
        BattleHudTheme.Label(new Rect(314,785,77,25),"모집",16,null,true);
        BattleHudTheme.Label(new Rect(391,783,105,28),me.gold+" G",21,BattleHudTheme.Gold,true);
        ArenaPresentationUI.SeatMeter(new Rect(505,788,73,4),Mathf.Min(5,me.gold/10),5,BattleHudTheme.Gold);
        BattleHudTheme.Label(new Rect(505,796,90,13),"이자 +"+RecruitmentAdvice.Interest(me.gold)+" G",10,BattleHudTheme.Muted);
        int cost=-1;Vector2 mouse=Event.current.mousePosition;
        for(int i=0;i<me.shop.Length;i++)
            if(new Rect(310+i*191,814,181,108).Contains(mouse)){var def=Def(me.shop[i]);if(def!=null)cost=def.cost;}
        if(new Rect(30,390,245,43).Contains(mouse)&&me.level<9)cost=4;
        if(new Rect(30,440,245,43).Contains(mouse))cost=2;
        BattleHudTheme.Label(new Rect(615,785,489,25),cost>=0?RecruitmentAdvice.Spend(me.gold,cost):RecruitmentAdvice.Bank(me.gold),12,cost>=0?BattleHudTheme.Gold:BattleHudTheme.Muted);
        bool available=fresh&&!busy&&!me.eliminated&&(room.phase=="prepare"||room.phase=="battle");
        string tip=RecruitmentAdvice.ShopLockHint;
        if(onlineInterface.Button(new Rect(1115,782,150,28),new GUIContent(me.shopLocked?"잠금 유지 중":"상점 잠금",tip),available,me.shopLocked,13))SetOnlineShopLock(!me.shopLocked);
    }

    bool TryOnlineFormationSource(Player me,out string area,out int slot)
    {
        area=onlineDragging?onlineDragArea:selectedArea;slot=onlineDragging?onlineDragSlot:selectedSlot;
        return slot>=0&&(area=="board"||area=="bench")&&(!onlineDragging||OnlineDraggedUnit(me)!=null);
    }
    FormationForecast.Plan OnlineFormationForecast(Player me,bool editable)
    {
        if(artPack!=0||!editable||!GUI.enabled||busy||onlineItem>=0||arena==null||OnlineScouting||state==null||state.room==null||state.room.phase!="prepare")return null;
        string sourceArea;int sourceSlot;if(!TryOnlineFormationSource(me,out sourceArea,out sourceSlot))return null;
        int seat=arena.HitBench(Event.current.mousePosition),cell=arena.HitCell(Event.current.mousePosition);
        if(seat<0&&cell<28)return null;
        var board=new string[28];var bench=new string[9];
        foreach(var unit in me.board)board[unit.slot]=unit.id;
        foreach(var unit in me.bench)bench[unit.slot]=unit.id;
        return FormationForecast.Preview(board,bench,sourceArea=="board",sourceSlot,seat<0,seat>=0?seat:cell-28,OnlineFormationLimit(me),
            Enumerable.Range(0,28).Select(i=>BuildMember(At(me.board,i))).ToArray(),Enumerable.Range(0,9).Select(i=>BuildMember(At(me.bench,i))).ToArray());
    }
    void OpenOnlineShopSkill(UnitDef definition)
    {onlineSkillId=definition.id;onlineSkillArea="shop";onlineSkillSlot=-1;onlineItemGuide=-1;arenaPointer.Reset();}
    void DrawOnlineRecruitCard(Rect r,string id,int slot,Player me,bool editable)
    {
        var def=Def(id);float hover=onlineInterface.HoverAmount(r,GUI.enabled);
        Color rarity=def==null?new Color(.15f,.25f,.3f):ArenaInterface.Rarity(def.cost);
        Card(r,Color.Lerp(ArenaInterface.Ink,rarity,.08f+hover*.12f),Color.Lerp(new Color(.15f,.25f,.3f),rarity,hover));
        if(def==null)
        {
            BattleHudTheme.Surface(r);
            BattleHudTheme.Label(new Rect(r.x+8,r.y+27,r.width-16,28),"모집 완료",16,BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(r.x+8,r.y+59,r.width-16,22),"다음 상점에서 만나요",11,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);return;
        }
        var skill=DigimonSkillCatalog.Find(id);bool afford=me.gold>=def.cost;
        int singles=me.board.Concat(me.bench).Count(u=>u.id==id&&u.star==1);
        int doubles=me.board.Concat(me.bench).Count(u=>u.id==id&&u.star==2);
        bool merge=singles>=2,space=me.bench.Length<9||merge;
        var caption=BattleHudTheme.Style(11);
        string hint=me.eliminated?"탈락 · 정찰 가능":!editable?(me.ready?"준비 해제 후 모집":"모집 대기"):!afford?"골드 부족":!space?"대기석 가득":merge?"구매 시 "+(doubles>=2?"★★★":"★★")+" 승급":RecruitmentAdvice.Copies(singles,doubles);
        BattleHudTheme.ShopArtwork(r,id,def.name,def.cost,hint,singles,doubles,merge,hover,true,ReadOnlineTraitHud(me.board,false).DeploymentBreakpoint(id));
        // Inspection is available even when a purchase is disabled.
        Rect icon=new Rect(r.x+7,r.y+7,23,23);
        if(DigimonSkillUI.DrawIcon(icon,skill))OpenOnlineShopSkill(def);
        if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&r.Contains(Event.current.mousePosition))
        {OpenOnlineShopSkill(def);Event.current.Use();}
        TeamPlannerUI.ShopMarker(new Rect(r.x+34,r.y+8,46,18),id,caption);
        string tip=(TeamPlan.Contains(id)?"[팀 계획 목표]\n":"")+def.name+" · "+def.cost+"G\n"+skill.ScalingRole+" · 기본 공격 "+skill.attackRange+"칸\n"+
            StarUpgradeUI.Collection(singles,doubles)+"\n"+RecruitmentAdvice.Spend(me.gold,def.cost)+"\n우클릭: 성급 비교 / 스킬 정보";
        GUI.Label(r,new GUIContent("",tip),GUIStyle.none);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&editable&&!busy&&afford&&space;
        if(GUI.Button(r,new GUIContent("",tip),GUIStyle.none))Send("/action",new Command{action="buy",slot=slot});
        GUI.enabled=enabled;
    }
}
