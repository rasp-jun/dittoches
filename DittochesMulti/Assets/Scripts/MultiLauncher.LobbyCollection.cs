using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    string lobbySearch="",lobbySearchCache,collectionFocus="agumon";
    int lobbyCost,lobbyCostCache=-1,collectionPage;
    UnitDef[] collectionMatches;
    const int CollectionPageSize=8;
    static Rect CollectionCardRect(int i){return new Rect(242+i%4*222,278+i/4*268,208,251);}
    static Rect CollectionCostRect(int i){return new Rect(624+i*66,209,58,43);}
    static readonly Rect CollectionResetRect=new Rect(1035,209,81,43),CollectionNextRect=new Rect(1016,851,100,43);
    static readonly Rect CollectionAddRect=new Rect(1176,817,378,64),CollectionSkillRect=new Rect(1176,750,378,47);
    void RefreshLobbyCollection()
    {
        if(collectionMatches!=null&&lobbySearchCache==lobbySearch&&lobbyCostCache==lobbyCost)return;
        string query=lobbySearch.Trim();
        collectionMatches=catalog.units.Where(u=>(lobbyCost==0||u.cost==lobbyCost)&&(query.Length==0||u.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||u.id.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||DigimonSkillCatalog.Find(u.id).name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0||DigimonBuildCatalog.ForUnit(u.id).Any(t=>t.name.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0))).ToArray();
        lobbySearchCache=lobbySearch;lobbyCostCache=lobbyCost;collectionPage=0;
        if(!collectionMatches.Any(u=>u.id==collectionFocus))collectionFocus=collectionMatches.Length>0?collectionMatches[0].id:"";
    }
    void DrawModernCodex()
    {
        SeasonLobbyArt.Panel(new Rect(222,120,914,804),new Color(.023f,.044f,.069f,.96f));
        BattleHudTheme.Label(new Rect(243,143,611,32),"전장을 함께할 파트너",23,SeasonLobbyArt.Cream,true);
        BattleHudTheme.Label(new Rect(843,149,273,25),"THE DIGIMON ARCHIVE",11,SeasonLobbyArt.Mint,true,TextAnchor.MiddleRight);
        lobbySearch=SeasonLobbyArt.Input(new Rect(242,209,366,43),"lobby-codex-search",lobbySearch,40,input,"이름 · 스킬 · 시너지 검색");
        for(int i=0;i<6;i++)if(SeasonLobbyArt.Tab(CollectionCostRect(i),i==0?"전체":i+" G",lobbyCost==i,true,13))lobbyCost=i;
        if(SeasonLobbyArt.Tab(CollectionResetRect,"초기화",false,true,12)){lobbyCost=0;lobbySearch="";GUI.FocusControl(null);}
        RefreshLobbyCollection();int pages=Mathf.Max(1,(collectionMatches.Length+CollectionPageSize-1)/CollectionPageSize);collectionPage=Mathf.Clamp(collectionPage,0,pages-1);
        if(collectionMatches.Length==0)
        {
            LobbyVisuals.FeatureCard(new Rect(1152,120,424,804),0,false);
            BattleHudTheme.Label(new Rect(290,435,786,55),"검색 결과가 없습니다",28,SeasonLobbyArt.Cream,true,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(290,505,786,50),"다른 이름을 검색하거나 필터를 초기화하세요.",16,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
            BattleHudTheme.Label(new Rect(1177,169,374,31),"파트너 상세",21,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(1178,218,372,60),"검색 조건과 일치하는 파트너를\n선택하면 상세 정보가 나타납니다.",14,BattleHudTheme.Muted);return;
        }
        for(int i=0;i<CollectionPageSize;i++)
        {
            int index=collectionPage*CollectionPageSize+i;if(index>=collectionMatches.Length)break;
            var def=collectionMatches[index];bool selected=collectionFocus==def.id;
            Rect card=CollectionCardRect(i);Color rarity=ArenaInterface.Rarity(def.cost);
            LobbyVisuals.FeatureCard(card,def.cost%3,selected);CharacterCardArt.Stage(new Rect(card.x+1,card.y+1,card.width-2,171),def.cost);
            FaithfulPortraits.Draw(new Rect(card.x+8,card.y+8,card.width-16,157),def.id,true);
            ArenaInterface.Fill(new Rect(card.x+1,card.y+171,card.width-2,79),selected?new Color(.039f,.115f,.146f):new Color(.03f,.058f,.083f));
            ArenaInterface.Fill(new Rect(card.x+14,card.y+178,3,58),rarity);
            BattleHudTheme.Badge(new Rect(card.x+12,card.y+12,43,26),def.cost+" G",rarity,12);
            if(TeamPlan.Contains(def.id))BattleHudTheme.Badge(new Rect(card.xMax-53,card.y+12,41,26),"목표",SeasonLobbyArt.Mint,10);
            BattleHudTheme.FitLabel(new Rect(card.x+27,card.y+183,card.width-39,27),def.name,19,SeasonLobbyArt.Cream,true);
            BattleHudTheme.Label(new Rect(card.x+28,card.y+220,card.width-41,20),string.Join(" · ",DigimonBuildCatalog.ForUnit(def.id).Select(t=>t.name).ToArray()),11,BattleHudTheme.Muted);
            if(selected)ArenaInterface.Fill(new Rect(card.x,card.yMax-2,card.width,2),SeasonLobbyArt.Mint);
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&card.Contains(Event.current.mousePosition)){OpenCollectionSkill(def.id);Event.current.Use();}
            if(GUI.Button(card,new GUIContent("","선택: 상세 정보 · 우클릭: 스킬 정보"),GUIStyle.none))collectionFocus=def.id;
        }
        if(SeasonLobbyArt.Tab(new Rect(242,851,100,43),"‹ 이전",false,collectionPage>0,13))collectionPage--;
        BattleHudTheme.Label(new Rect(358,858,642,30),(collectionPage+1)+" / "+pages+"    ·    "+collectionMatches.Length+"종의 파트너",13,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
        if(SeasonLobbyArt.Tab(CollectionNextRect,"다음 ›",false,collectionPage+1<pages,13))collectionPage++;
        var focus=Def(collectionFocus);if(focus==null)return;
        Rect panel=new Rect(1152,120,424,804);LobbyVisuals.FeatureCard(panel,focus.cost%3,false);
        CharacterCardArt.Stage(new Rect(1153,121,422,318),focus.cost);FaithfulPortraits.Draw(new Rect(1170,133,388,292),focus.id);
        BattleHudTheme.Badge(new Rect(1175,143,79,29),focus.cost+" GOLD",ArenaInterface.Rarity(focus.cost),12);
        BattleHudTheme.Label(new Rect(1385,147,168,22),"PARTNER PROFILE",10,SeasonLobbyArt.Mint,true,TextAnchor.MiddleRight);
        BattleHudTheme.FitLabel(new Rect(1175,458,378,43),focus.name,32,SeasonLobbyArt.Cream,true);
        var skill=DigimonSkillCatalog.Find(focus.id);
        BattleHudTheme.Label(new Rect(1178,512,373,26),DigimonVisualScale.StageName(focus.id)+" · "+skill.ScalingRole,14,BattleHudTheme.Muted);
        int row=0;foreach(var trait in DigimonBuildCatalog.ForUnit(focus.id))
        {
            float y=559+row++*40;GUI.DrawTexture(new Rect(1178,y+2,27,27),DigimonTraitUI.Icon(trait.id));
            BattleHudTheme.Label(new Rect(1219,y,331,30),trait.name,17,DigimonTraitUI.ColorFor(trait.id),true);
        }
        LobbyVisuals.Rule(new Rect(1177,655,376,1),LobbyVisuals.Edge);
        BattleHudTheme.Label(new Rect(1178,676,373,22),"고유 스킬 / 사거리 "+skill.attackRange,11,SeasonLobbyArt.Mint,true);
        BattleHudTheme.FitLabel(new Rect(1176,707,377,31),skill.name,19,SeasonLobbyArt.Cream,true);
        if(SeasonLobbyArt.Tab(CollectionSkillRect,"유닛 · 스킬 상세  ↗",false,true,15))OpenCollectionSkill(focus.id);
        bool planned=TeamPlan.Contains(focus.id);
        if(SeasonLobbyArt.Play(CollectionAddRect,planned?"팀 계획에서 제외":TeamPlan.Units.Count>=9?"팀 계획 가득 참":"팀 계획에 추가",planned||TeamPlan.Units.Count<9,planned))TeamPlan.Toggle(focus.id);
        BattleHudTheme.Label(new Rect(1178,892,373,19),"팀 계획 "+TeamPlan.Units.Count+" / 9  ·  우클릭으로 스킬 바로 보기",11,BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
    }
    void OpenCollectionSkill(string id){onlineSkillId=id;onlineSkillArea="";onlineSkillSlot=-1;GUI.FocusControl(null);}
}
