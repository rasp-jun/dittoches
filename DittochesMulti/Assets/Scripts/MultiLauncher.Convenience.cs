using UnityEngine;

public sealed partial class MultiLauncher
{
    bool showPlayHelp;int playHelpTab;
    void OpenPlayHelp()
    {
        showPlayHelp=true;ResetEquipmentSelection();onlineEquipmentDrag.Reset();ResetOnlineDrag();arenaPointer.Reset();GUI.FocusControl(null);
    }
    Event PreparePlayHelpInput()
    {
        var e=Event.current;
        if(artPack==0&&e.type==EventType.KeyDown&&e.keyCode==KeyCode.F1){if(showPlayHelp)showPlayHelp=false;else OpenPlayHelp();e.Use();}
        if(showPlayHelp&&(e.isMouse||e.isKey||e.type==EventType.ScrollWheel)){var saved=new Event(e);e.Use();return saved;}return null;
    }
    void DrawPlayHelp(Event input)
    {
        if(artPack!=0)return;if(input!=null)Event.current=input;
        if(showPlayHelp)
        {
            bool match=state!=null&&state.room!=null;
            showPlayHelp=PlayHelpUI.Draw(new Rect(0,0,1600,1000),ref playHelpTab,match?"온라인 리그":LobbyQueued?"대전 탐색 중":"대전 로비",match||LobbyQueued,!match&&lobbyMode==3,match&&state.room.phase=="battle");return;
        }
        if(onlineInterface.Button(new Rect(1408,954,168,34),new GUIContent("F1  도움말 · 소리"),true,false,13))OpenPlayHelp();
    }
    void CraftOnlineEquipmentFromGuide(int result)
    {
        var me=OnlineMe;
        if(me==null||busy||!OnlineManagementAllowed(state.room,me)||!OnlineConnectionFresh())return;
        int[] slots=DigimonEquipmentUI.CraftSlots(result,me.inventory);if(slots.Length!=2)return;
        ResetEquipmentSelection();
        Send("/action",new Command{action="combine_items",itemSlot=slots[0],targetItemSlot=slots[1],inventoryRevision=me.inventoryRevision});
    }
}
