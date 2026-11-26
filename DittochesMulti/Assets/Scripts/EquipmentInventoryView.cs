using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Display indices never replace inventory slots used by equip/crafting commands.
public sealed class EquipmentInventoryView
{
    public const int PageSize=12;
    readonly List<int> visible=new List<int>();
    int[] snapshot=new int[0];
    readonly int[] counts=new int[3];
    int hintResult=-1,hintSelection=-1;
    GUIStyle tabStyle,hintStyle;
    public int Filter { get; private set; }
    public int Count { get { return visible.Count; } }
    public int Pages { get { return Mathf.Max(1,(Count+PageSize-1)/PageSize); } }
    public void Sync(IList<int> inventory,ref int page)
    {
        if(!snapshot.SequenceEqual(inventory)){snapshot=inventory.ToArray();hintResult=-1;hintSelection=-1;Rebuild();}
        page=Mathf.Clamp(page,0,Pages-1);
    }
    void Rebuild()
    {
        visible.Clear();counts[0]=snapshot.Length;counts[1]=counts[2]=0;
        for(int i=0;i<snapshot.Length;i++)
        {
            int category=DigimonBuildCatalog.IsComponent(snapshot[i])?1:2;counts[category]++;
            if(Filter==0||Filter==category)visible.Add(i);
        }
    }
    public void SetFilter(int value,ref int page)
    {value=Mathf.Clamp(value,0,2);if(Filter==value)return;Filter=value;page=0;Rebuild();}
    public int Index(int page,int slot)
    {int i=page*PageSize+slot;return slot>=0&&slot<PageSize&&i>=0&&i<visible.Count?visible[i]:-1;}
    public int PageOf(int inventorySlot){int i=visible.IndexOf(inventorySlot);return i<0?-1:i/PageSize;}
    public Rect TabRect(Rect r,int tab){return new Rect(r.x+tab*(r.width+3)/3,r.y,(r.width-6)/3,r.height);}
    public void DrawTabs(Rect r,GUIStyle style,bool gesture,ref int page)
    {
        if(tabStyle==null)tabStyle=new GUIStyle(style){fontSize=11,alignment=TextAnchor.MiddleCenter,wordWrap=false,padding=new RectOffset()};
        bool enabled=GUI.enabled;GUI.enabled=enabled&&!gesture;
        for(int i=0;i<3;i++)
        {
            Rect button=TabRect(r,i);bool active=Filter==i;
            BattleHudTheme.Round(button,active?Color.Lerp(BattleHudTheme.Panel,BattleHudTheme.Teal,.23f):BattleHudTheme.Raised);
            if(active)ArenaInterface.Fill(new Rect(button.x+8,button.yMax-2,button.width-16,2),BattleHudTheme.Teal);
            tabStyle.normal.textColor=active?BattleHudTheme.Teal:BattleHudTheme.Muted;
            string name=i==0?"전체":i==1?"재료":"장비";
            if(GUI.Button(button,new GUIContent(name+" "+counts[i],i==2?"완성 무장 · 인장 · 배치 확장 · 추출기":name+" 보기"),tabStyle))SetFilter(i,ref page);
        }
        GUI.enabled=enabled;
    }
    public void Scroll(Event e,Rect region,bool allowed,bool gesture,ref int page)
    {
        if(!allowed||!region.Contains(e.mousePosition)||e.type!=EventType.ScrollWheel)return;
        if(!gesture&&Mathf.Abs(e.delta.y)>.01f)page=Mathf.Clamp(page+(e.delta.y>0?1:-1),0,Pages-1);
        e.Use();
    }
    public static int CombinePreview(IList<int> inventory,int selected,int hovered)
    {return selected>=0&&selected<inventory.Count&&hovered>=0&&hovered<inventory.Count&&selected!=hovered?DigimonBuildCatalog.Combine(inventory[selected],inventory[hovered]):-1;}
    // Inspecting the result is separate from the two material clicks that craft it.
    public int DrawHint(Rect r,GUIStyle style,IList<int> inventory,int selected,int hovered,string fallback)
    {
        if(hintStyle==null)hintStyle=new GUIStyle(style){fontSize=12,wordWrap=true,alignment=TextAnchor.UpperLeft};
        int result=CombinePreview(inventory,selected,hovered);
        if(hintSelection!=selected){hintSelection=selected;hintResult=-1;}
        if(hovered>=0)hintResult=result;
        else if(r.Contains(Event.current.mousePosition)&&selected>=0&&selected<inventory.Count)result=hintResult;
        int focus=result>=0?result:selected>=0&&selected<inventory.Count?inventory[selected]:-1;
        if(focus<0){GUI.Label(r,Count==0?(Filter==1?"보관 중인 재료가 없습니다":Filter==2?"보관 중인 장비가 없습니다":"보관함이 비어 있습니다"):fallback,hintStyle);return -1;}
        BattleHudTheme.Round(r,Color.Lerp(BattleHudTheme.Raised,result>=0?BattleHudTheme.Gold:BattleHudTheme.Teal,.12f));
        DigimonEquipmentArt.Draw(new Rect(r.x+5,r.y+6,27,27),focus);
        string title=DigimonBuildCatalog.Data.items[focus].name;
        string action="이 카드 클릭 → 효과 보기";
        BattleHudTheme.Label(new Rect(r.x+40,r.y+2,r.width-45,13),result>=0?"합성 미리보기":"장착할 장비",10,result>=0?BattleHudTheme.Gold:BattleHudTheme.Teal);
        BattleHudTheme.FitLabel(new Rect(r.x+40,r.y+16,r.width-45,20),title,12,BattleHudTheme.Text);
        GUI.Label(new Rect(r.x+40,r.y+2,r.width-45,34),new GUIContent("",DigimonEquipmentArt.Tooltip(focus)),GUIStyle.none);
        BattleHudTheme.Label(new Rect(r.x+6,r.y+36,r.width-12,17),action,11,BattleHudTheme.Muted);
        return DigimonEquipmentUI.RightClick(r)||GUI.Button(r,new GUIContent("",DigimonEquipmentArt.Tooltip(focus)),GUIStyle.none)?focus:-1;
    }
}
