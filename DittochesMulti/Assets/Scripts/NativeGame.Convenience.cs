using UnityEngine;

public sealed partial class NativeGame
{
    bool showPlayHelp;int playHelpTab;
    void OpenPlayHelp()
    {
        showPlayHelp=true;selectedItem=selectedBoard=selectedBench=-1;equipmentDrag.Reset();arenaPointer.Reset();dragSource=-1;draggingUnit=false;GUI.FocusControl(null);
    }
    Event PreparePlayHelpInput()
    {
        var e=Event.current;
        if(artPack==0&&e.type==EventType.KeyDown&&e.keyCode==KeyCode.F1){if(showPlayHelp)showPlayHelp=false;else OpenPlayHelp();e.Use();}
        if(showPlayHelp&&(e.isMouse||e.isKey||e.type==EventType.ScrollWheel)){var saved=new Event(e);e.Use();return saved;}
        return null;
    }
    void DrawPlayHelp(Event input)
    {
        if(artPack!=0)return;
        if(input!=null)Event.current=input;
        if(showPlayHelp){showPlayHelp=PlayHelpUI.Draw(new Rect(0,0,1920,1080),ref playHelpTab,TrialsMode?"아구몬박사의 시험":lobby?"솔로 준비":"솔로 리그",false,TrialsMode,battling);return;}
        if(showTeamPlan||showTrialDossier||showCarousel||showRecipeGuide||skillDetailUnit!=null||traitFocus!=null||confirmSoloReset)return;
        bool enabled=GUI.enabled;
        if(arenaInterface.Button(lobby?new Rect(272,27,35,34):new Rect(231,22,36,38),new GUIContent("?","F1 · 조작 도움말 / 소리 설정"),true,false,20))OpenPlayHelp();
        GUI.enabled=enabled;
    }
    void CraftEquipmentFromGuide(int result)
    {
        if(artPack!=0||lobby||showCarousel||hp<=0)return;
        int[] slots=DigimonEquipmentUI.CraftSlots(result,inventory);if(slots.Length!=2)return;
        // Reuse the same validated inventory operation, including save and feedback.
        selectedItem=slots[0];SelectInventoryItem(slots[1]);selectedItem=-1;
    }
}
