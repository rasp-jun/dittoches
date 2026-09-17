using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    int onlineItem=-1,onlineItemRevision=-1,onlineItemPage,onlineItemGuide=-1;
    string equipmentRoom="";
    Player OnlineMe { get { return state==null||state.room==null||state.room.players==null||state.room.side<0||state.room.side>=state.room.players.Length?null:state.room.players[state.room.side]; } }
    void ResetEquipmentSelection(){onlineItem=-1;onlineItemRevision=-1;}
    void ReconcileEquipmentSelection()
    {
        var me=OnlineMe;
        if(me==null||state.room.id!=equipmentRoom)
        {ResetEquipmentSelection();onlineItemGuide=-1;onlineItemPage=0;equipmentRoom=me==null?"":state.room.id;}
        if(me==null)return;
        if(me.inventory==null)me.inventory=new int[0];
        if(state.room.phase=="finished")onlineItemGuide=-1;
        if(onlineItem>=me.inventory.Length||me.inventoryRevision!=onlineItemRevision||me.ready||state.room.phase!="prepare")ResetEquipmentSelection();
    }
    void SelectOnlineItem(int index)
    {
        var me=OnlineMe;if(me==null||busy||index<0||index>=me.inventory.Length)return;
        if(onlineItem==index){ResetEquipmentSelection();return;}
        if(onlineItem>=0&&onlineItemRevision==me.inventoryRevision&&DigimonBuildCatalog.Combine(me.inventory[onlineItem],me.inventory[index])>=0)
        {
            var command=new Command{action="combine_items",itemSlot=onlineItem,targetItemSlot=index,inventoryRevision=onlineItemRevision};
            ResetEquipmentSelection();Send("/action",command);return;
        }
        onlineItem=index;onlineItemRevision=me.inventoryRevision;selectedSlot=-1;selectedArea="";
    }
    bool EquipOnlineSelection(string area,int slot,Unit unit)
    {
        if(onlineItem<0)return false;
        if(unit==null){notice="장비를 장착할 아군 유닛을 선택하세요.";return true;}
        if(busy)return true;
        var command=new Command{action="equip",itemSlot=onlineItem,inventoryRevision=onlineItemRevision,area=area,slot=slot};
        selectedArea=area;selectedSlot=slot;
        onlineReport=false;
        ResetEquipmentSelection();Send("/action",command);return true;
    }
    void DrawOnlineEquipment(Player me,bool editable)
    {
        Card(new Rect(25,650,260,278),surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(39,658,165,25),"장비 보관함 · "+me.inventory.Length,eyebrow);
        if(Btn(new Rect(205,655,65,28),"조합표"))onlineItemGuide=onlineItem>=0?me.inventory[onlineItem]:0;
        int pages=Mathf.Max(1,(me.inventory.Length+11)/12);onlineItemPage=Mathf.Clamp(onlineItemPage,0,pages-1);
        int hover=-1;
        for(int i=0;i<12;i++)
        {
            Rect r=new Rect(39+i%4*57,695+i/4*42,49,36);int index=onlineItemPage*12+i;
            if(index>=me.inventory.Length){Panel(r,surface2);continue;}
            int id=me.inventory[index];var item=DigimonBuildCatalog.Data.items[id];
            if(r.Contains(Event.current.mousePosition))hover=id;
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&r.Contains(Event.current.mousePosition))
            {onlineItemGuide=id;Event.current.Use();}
            if(Btn(r,item.icon,editable))SelectOnlineItem(index);
            if(index==onlineItem)Border(r,paleGold,2);else if(id>=4&&id<14)Border(r,gold,1);
        }
        if(pages>1)
        {
            if(Btn(new Rect(39,824,40,25),"‹",onlineItemPage>0))onlineItemPage--;
            GUI.Label(new Rect(87,824,124,25),(onlineItemPage+1)+" / "+pages,centered);
            if(Btn(new Rect(228,824,40,25),"›",onlineItemPage+1<pages))onlineItemPage++;
        }
        string hint=!editable?"준비 단계에서 장착·합성\n우클릭으로 장비 정보 확인":"장비 클릭 → 아군에게 장착\n재료 2개 클릭 → 합성";
        int focus=hover>=0?hover:onlineItem>=0?me.inventory[onlineItem]:-1;
        if(focus>=0)hint=DigimonBuildCatalog.Data.items[focus].name+"\n"+(onlineItem>=0?"아군에게 장착 · 같은 장비 클릭 시 취소":"우클릭 → 효과와 조합 확인");
        GUI.Label(new Rect(39,pages>1?861:830,230,pages>1?61:90),hint,new GUIStyle(small){fontSize=13});
    }
    void DrawOnlineUnitEquipment(Player me,Room room)
    {
        Unit unit=selectedSlot>=0?At(selectedArea=="board"?me.board:me.bench,selectedSlot):null;
        Card(new Rect(1305,290,270,180),surface,new Color(.2f,.38f,.43f));
        if(unit==null)
        {GUI.Label(new Rect(1325,310,225,25),"BATTLE LOG",eyebrow);GUI.Label(new Rect(1325,345,225,105),string.IsNullOrEmpty(room.message)?"양쪽 준비 완료 또는 시간 종료 시 전투 시작\n\n매 라운드 양쪽에 동일 장비 보급":room.message,small);return;}
        GUI.Label(new Rect(1320,303,240,45),Def(unit.id).name+"  "+new string('★',unit.star),text);
        int[] items=unit.items??new int[0];
        for(int i=0;i<2;i++)
        {
            Rect r=new Rect(1320,354+i*40,240,34);
            if(i<items.Length)
            {
                int id=items[i];bool enabled=GUI.enabled;GUI.enabled=enabled&&!busy;
                if(GUI.Button(r,DigimonBuildCatalog.Data.items[id].name,new GUIStyle(button){fontSize=13,padding=new RectOffset(6,6,3,3)}))onlineItemGuide=id;
                GUI.enabled=enabled;
            }
            else GUI.Label(r,"빈 장비 슬롯",small);
        }
        GUI.Label(new Rect(1320,440,240,23),"장비 클릭 → 상세 · 추출기로 회수",new GUIStyle(small){fontSize=12});
    }
    void DrawOnlineEquipmentGuide()
    {
        if(onlineItemGuide<0)return;
        var item=DigimonBuildCatalog.Data.items[onlineItemGuide];Rect r=new Rect(470,225,660,550);
        Panel(new Rect(0,76,1600,868),new Color(0,0,0,.6f));Card(r,surface,gold);
        GUI.Label(new Rect(r.x+24,r.y+18,555,50),item.name,text);
        if(Btn(new Rect(r.xMax-59,r.y+15,42,33),"×")){onlineItemGuide=-1;return;}
        GUI.Label(new Rect(r.x+24,r.y+57,610,25),(item.id<4?"기본 재료":item.id<14?"완성 장비":"소모품")+" · "+item.role,eyebrow);
        GUI.Label(new Rect(r.x+24,r.y+91,610,60),item.description,small);
        if(item.id<4)
        {
            for(int i=0;i<4;i++)
            {
                var made=DigimonBuildCatalog.Data.items[DigimonBuildCatalog.Combine(item.id,i)];float y=r.y+157+i*78;
                Panel(new Rect(r.x+20,y,620,72),surface2);
                GUI.Label(new Rect(r.x+30,y+4,600,26),"+ "+DigimonBuildCatalog.Data.items[i].name+"  →  "+made.name,small);
                GUI.Label(new Rect(r.x+30,y+32,600,36),made.description,new GUIStyle(small){fontSize=13});
            }
        }
        else
        {
            string recipe=item.recipe.Length==2?string.Join(" + ",item.recipe.Select(i=>DigimonBuildCatalog.Data.items[i].name).ToArray()):"합성 불가 · 사용하면 소모됩니다";
            GUI.Label(new Rect(r.x+24,r.y+167,610,80),"조합\n"+recipe,text);
            GUI.Label(new Rect(r.x+24,r.y+269,610,65),item.flavor,small);
            GUI.Label(new Rect(r.x+24,r.y+375,610,75),"장비는 유닛당 2칸 · 판매하면 보관함으로 회수\n별 합성 때 넘치는 장비도 보관함으로 반환됩니다.",small);
        }
        GUI.Label(new Rect(r.x+24,r.y+493,610,36),"첫 지급: 기본 재료 4종 + 추출기 · 이후 라운드마다 양쪽 동일 보급",new GUIStyle(small){fontSize=13});
    }
    void DrawOnlineEquipmentPreview(Player me,bool editable)
    {
        if(artPack!=0||!editable||!GUI.enabled||onlineItem<0||onlineItem>=me.inventory.Length||arena==null)return;
        int seat=arena.HitBench(Event.current.mousePosition),cell=arena.HitCell(Event.current.mousePosition);
        bool onBoard=seat<0&&cell>=28;
        Unit unit=seat>=0?At(me.bench,seat):onBoard?At(me.board,cell-28):null;
        if(unit==null)return;
        var ids=onBoard?me.board.Select(u=>u.id):Enumerable.Empty<string>();
        var preview=DigimonEquipmentPreview.Create(unit.id,unit.star,ids,unit.items??new int[0],me.inventory[onlineItem]);
        DigimonEquipmentPreview.Draw(new Rect(1305,290,270,638),Def(unit.id).name+" "+new string('★',unit.star),
            DigimonSkillCatalog.Find(unit.id),preview,onBoard?"현재 전장 시너지 적용":"대기석 · 시너지 미포함");
    }
    void DrawUnitItemBadges(Unit unit,Vector3 ground)
    {
        if(unit.items==null||unit.items.Length==0)return;
        Vector2 p=arena.Project(ground);int count=Mathf.Min(2,unit.items.Length);
        for(int i=0;i<count;i++)
        {
            Rect r=new Rect(p.x-count*12+i*24,p.y+18,22,20);Panel(r,surface);Border(r,gold,1);
            GUI.Label(r,DigimonBuildCatalog.Data.items[unit.items[i]].icon,new GUIStyle(centered){fontSize=11});
        }
    }
}
