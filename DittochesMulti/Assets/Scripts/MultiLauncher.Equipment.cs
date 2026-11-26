using System;
using System.Linq;
using UnityEngine;

public sealed partial class MultiLauncher
{
    int onlineItem=-1,onlineItemRevision=-1,onlineItemPage,onlineItemGuide=-1;
    Vector2 onlineEquipmentAnchor=new Vector2(290,710);
    void OpenOnlineEquipmentGuide(int id)
    {
        onlineEquipmentAnchor=Event.current!=null?Event.current.mousePosition:new Vector2(290,710);
        onlineEquipmentDrag.Reset();ResetEquipmentSelection();ResetOnlineDrag();arenaPointer.Reset();
        onlineSkillId="";showTraitGuide=false;showTeamPlan=false;DigimonEquipmentUI.BeginGuide(id);onlineItemGuide=id;
    }
    string equipmentRoom="";
    Player OnlineMe { get { return state==null||state.room==null||state.room.players==null||state.room.side<0||state.room.side>=state.room.players.Length?null:state.room.players[state.room.side]; } }
    void ResetEquipmentSelection(){onlineItem=-1;onlineItemRevision=-1;}
    void ReconcileEquipmentSelection()
    {
        var me=OnlineMe;
        if(me==null||state.room.id!=equipmentRoom)
        {ResetEquipmentSelection();onlineItemGuide=-1;onlineItemPage=0;onlineInventoryView.SetFilter(0,ref onlineItemPage);equipmentRoom=me==null?"":state.room.id;}
        if(me==null)return;
        if(me.inventory==null)me.inventory=new int[0];
        if(state.room.phase=="finished")onlineItemGuide=-1;
        if(onlineItem>=me.inventory.Length||me.inventoryRevision!=onlineItemRevision||!OnlineManagementAllowed(state.room,me))ResetEquipmentSelection();
    }
    void SelectOnlineItem(int index)
    {
        var me=OnlineMe;if(me==null||!OnlineManagementAllowed(state.room,me)||!OnlineConnectionFresh()||busy||index<0||index>=me.inventory.Length)return;
        if(onlineItem==index){ResetEquipmentSelection();return;}
        if(onlineItem>=0&&onlineItemRevision==me.inventoryRevision&&DigimonBuildCatalog.Combine(me.inventory[onlineItem],me.inventory[index])>=0)
        {
            var command=new Command{action="combine_items",itemSlot=onlineItem,targetItemSlot=index,inventoryRevision=onlineItemRevision};
            ResetEquipmentSelection();Send("/action",command);return;
        }
        if(OnlineScouting)SetScoutedPlayer(-1);
        onlineItem=index;onlineItemRevision=me.inventoryRevision;selectedSlot=-1;selectedArea="";
    }
    bool EquipOnlineSelection(string area,int slot,Unit unit)
    {
        if(onlineItem<0)return false;
        if(OnlineScouting)return true;
        if(unit==null){notice="장비를 장착할 아군 유닛을 선택하세요.";return true;}
        if(busy||!OnlineManagementAllowed(state.room,OnlineMe)||!OnlineConnectionFresh())return true;
        var command=new Command{action="equip",itemSlot=onlineItem,inventoryRevision=onlineItemRevision,area=area,slot=slot};
        selectedArea=area;selectedSlot=slot;
        onlineReport=false;
        ResetEquipmentSelection();Send("/action",command);return true;
    }
    void DrawOnlineEquipment(Player me,bool editable)
    {
        Card(new Rect(25,650,260,278),surface,new Color(.2f,.38f,.43f));
        GUI.Label(new Rect(39,658,153,25),"보관함 · "+me.inventory.Length,eyebrow);
        if(Btn(new Rect(193,657,76,26),"합성 도감"))OpenOnlineEquipmentGuide(0);
        int pages=Mathf.Max(1,(me.inventory.Length+11)/12);onlineItemPage=Mathf.Clamp(onlineItemPage,0,pages-1);
        if(artPack==0)
        {
            onlineInventoryView.Sync(me.inventory,ref onlineItemPage);
            onlineInventoryView.DrawTabs(OnlineInventoryTabs,small,onlineEquipmentDrag.Slot>=0,ref onlineItemPage);
            pages=onlineInventoryView.Pages;
        }
        int hover=-1,hoveredSlot=-1;
        for(int i=0;i<12;i++)
        {
            Rect r=OnlineInventoryRect(i);int index=artPack==0?onlineInventoryView.Index(onlineItemPage,i):onlineItemPage*12+i;
            if(index<0||index>=me.inventory.Length){Panel(r,surface2);continue;}
            int id=me.inventory[index];var item=DigimonBuildCatalog.Data.items[id];
            if(r.Contains(Event.current.mousePosition)){hover=id;hoveredSlot=index;}
            if(GUI.enabled&&Event.current.type==EventType.MouseDown&&Event.current.button==1&&r.Contains(Event.current.mousePosition))
            {OpenOnlineEquipmentGuide(id);Event.current.Use();}
            if(artPack==0)
            {if(DigimonEquipmentArt.Button(r,id,index==onlineItem,editable,onlineItem>=0&&onlineItem!=index?me.inventory[onlineItem]:-1))SelectOnlineItem(index);}
            else if(Btn(r,item.icon,editable))SelectOnlineItem(index);
        }
        if(pages>1)
        {
            float pageY=artPack==0?849:824;
            if(Btn(new Rect(39,pageY,40,25),"‹",onlineItemPage>0&&onlineEquipmentDrag.Slot<0))onlineItemPage--;
            GUI.Label(new Rect(87,pageY,124,25),new GUIContent((onlineItemPage+1)+" / "+pages,"보관함 위에서 휠로 페이지 이동 · 선택한 장비 유지"),centered);
            if(Btn(new Rect(228,pageY,40,25),"›",onlineItemPage+1<pages&&onlineEquipmentDrag.Slot<0))onlineItemPage++;
        }
        string hint=!editable?"준비 취소·연결 확인 후 장착\n우클릭으로 장비 정보 확인":"장비 드래그 / 클릭 → 아군에게 장착\n재료 2개 클릭 → 합성";
        if(editable&&state.room.phase=="battle")hint="장비 즉시 반영 · 대기석 관리 가능\n전장 이동·판매는 준비 단계에서";
        int focus=hover>=0?hover:onlineItem>=0?me.inventory[onlineItem]:-1;
        if(focus>=0)hint=DigimonBuildCatalog.Data.items[focus].name+"\n"+(onlineItem>=0?"아군에게 장착 · 같은 장비 클릭 시 취소":"우클릭 → 효과와 조합 확인");
        if(artPack==0)
        {
            int inspect=onlineInventoryView.DrawHint(new Rect(39,875,230,53),small,me.inventory,onlineItem,editable?hoveredSlot:-1,hint);
            if(inspect>=0)OpenOnlineEquipmentGuide(inspect);
        }
        else GUI.Label(new Rect(39,pages>1?861:830,230,pages>1?61:90),hint,new GUIStyle(small){fontSize=13});
    }
    static Rect OnlineEquippedSlotRect(int slot){return new Rect(1320,347+slot*31,240,29);}
    void DrawOnlineUnitEquipment(Player me,Room room)
    {
        Unit unit=selectedSlot>=0?At(selectedArea=="board"?me.board:me.bench,selectedSlot):null;
        Card(new Rect(1305,290,270,180),surface,new Color(.2f,.38f,.43f));
        if(unit==null)
        {DrawOnlineRoundOverview(room);return;}
        GUI.Label(new Rect(1320,303,240,45),Def(unit.id).name+"  "+new string('★',unit.star),text);
        int[] items=unit.items??new int[0];
        for(int i=0;i<DigimonBuildCatalog.EquipmentSlots;i++)
        {
            Rect r=OnlineEquippedSlotRect(i);
            if(i<items.Length)
            {
                int id=items[i];bool enabled=GUI.enabled;GUI.enabled=enabled&&!busy;
                if(artPack==0)
                {
                    DigimonEquipmentArt.Draw(new Rect(r.x,r.y,27,27),id);
                    GUI.Label(new Rect(r.x+41,r.y+4,r.width-41,28),DigimonBuildCatalog.Data.items[id].name,new GUIStyle(small){fontSize=13});
                    if(DigimonEquipmentUI.RightClick(r)||GUI.Button(r,new GUIContent("",DigimonEquipmentArt.Tooltip(id)),GUIStyle.none))OpenOnlineEquipmentGuide(id);
                }
                else if(GUI.Button(r,DigimonBuildCatalog.Data.items[id].name,new GUIStyle(button){fontSize=13,padding=new RectOffset(6,6,3,3)}))onlineItemGuide=id;
                GUI.enabled=enabled;
            }
            else GUI.Label(r,"빈 장비 슬롯",small);
        }
        GUI.Label(new Rect(1320,440,240,23),"장비 3칸 · 클릭 / 우클릭으로 상세",new GUIStyle(small){fontSize=12});
    }
    void DrawOnlineEquipmentGuide()
    {
        if(onlineItemGuide<0)return;
        if(OnlineMe==null){onlineItemGuide=-1;return;}
        if(artPack==0)
        {
            if(!DigimonEquipmentUI.DrawGuide(DigimonEquipmentUI.PopupRect(onlineEquipmentAnchor,new Rect(0,76,1600,868),onlineItemGuide),ref onlineItemGuide,OnlineMe.inventory,CraftOnlineEquipmentFromGuide,!busy&&OnlineManagementAllowed(state.room,OnlineMe)&&OnlineConnectionFresh(),busy?"처리 중…":!OnlineConnectionFresh()?"연결 확인 후 합성":"준비 취소 후 합성"))onlineItemGuide=-1;
            return;
        }
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
            GUI.Label(new Rect(r.x+24,r.y+375,610,75),"장비는 유닛당 3칸 · 판매하면 보관함으로 회수\n별 합성 때 넘치는 장비도 보관함으로 반환됩니다.",small);
        }
        GUI.Label(new Rect(r.x+24,r.y+493,610,36),"첫 지급: 기본 재료 4종 + 추출기 · 이후 라운드마다 생존자 동일 보급",new GUIStyle(small){fontSize=13});
    }
    void DrawOnlineEquipmentPreview(Player me,bool editable)
    {
        if(OnlineScouting||artPack!=0||!editable||!GUI.enabled||onlineItem<0||onlineItem>=me.inventory.Length||arena==null)return;
        string area,blocked;int slot;Fighter live;
        Unit unit=OnlineEquipmentTarget(out area,out slot,out live,out blocked);bool onBoard=area=="board";
        if(unit==null)return;
        var ids=live!=null?state.room.frames[0].units.Where(f=>f.side==state.room.side).Select(f=>new DigimonBuildCatalog.Member(f.id,(At(me.board,f.slot)??new Unit()).items)):onBoard?me.board.Select(BuildMember):Enumerable.Empty<DigimonBuildCatalog.Member>();
        int star=live!=null?live.star:unit.star;
        var preview=DigimonEquipmentPreview.Create(unit.id,star,ids,unit.items??new int[0],me.inventory[onlineItem],onBoard);
        if(live!=null)DigimonEquipmentPreview.SetCombatState(preview,live.hp,live.maxHp,live.mana,live.combatAge);
        DigimonEquipmentPreview.Draw(new Rect(1305,290,270,638),Def(unit.id).name+" "+new string('★',star),
            DigimonSkillCatalog.Find(unit.id),preview,live!=null?"전투 중 · 현재 팀 시너지":onBoard?"현재 전장 시너지 적용":"대기석 · 시너지 미포함");
    }
    void DrawUnitItemBadges(Unit unit,Vector3 ground)
    {
        if(unit.items==null||unit.items.Length==0)return;
        Vector2 p=arena.Project(ground);int count=Mathf.Min(DigimonBuildCatalog.EquipmentSlots,unit.items.Length);
        for(int i=0;i<count;i++)
        {
            Rect r=new Rect(p.x-count*12+i*24,p.y+18,22,20);Panel(r,surface);Border(r,gold,1);
            if(artPack==0)DigimonEquipmentArt.Draw(r,unit.items[i]);
            else GUI.Label(r,DigimonBuildCatalog.Data.items[unit.items[i]].icon,new GUIStyle(centered){fontSize=11});
        }
    }
}
