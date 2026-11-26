using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DigimonEquipmentUI
{
    const float GuideWidth=1080,GuideHeight=780;
    static GUIStyle body,small;
    static int materialFocus=-1,observedFocus=-1;
    static bool craftableOnly;
    public static bool CraftableOnly {get{return craftableOnly;}}
    public static int SelectedMaterial {get{return materialFocus;}}
    public static int CurrentComponent {get{return materialFocus;}}
    public static int DisplayedItem {get{return observedFocus;}}

    public sealed class RecipeMaterial
    {
        public int item,required,held;
        public int Missing {get{return Mathf.Max(0,required-held);}}
        public bool Ready {get{return held>=required;}}
    }

    static int ValidId(int id){return Mathf.Clamp(id,0,DigimonBuildCatalog.Data.items.Length-1);}
    static Rect At(Rect r,float x,float y,float width,float height)
    {return new Rect(r.x+x*r.width/GuideWidth,r.y+y*r.height/GuideHeight,width*r.width/GuideWidth,height*r.height/GuideHeight);}
    static int Font(Rect r,int size){return Mathf.Max(9,Mathf.RoundToInt(size*Mathf.Min(r.width/GuideWidth,r.height/GuideHeight)));}
    static void Styles(Rect r)
    {
        if(body==null)
        {
            body=new GUIStyle(GUI.skin.label){wordWrap=true,padding=new RectOffset(0,0,0,0),alignment=TextAnchor.UpperLeft};
            body.normal.textColor=BattleHudTheme.Text;
            small=new GUIStyle(body);small.normal.textColor=BattleHudTheme.Muted;
        }
        body.fontSize=Font(r,15);small.fontSize=Font(r,12);
    }

    public static int[] ComponentIds()
    {return DigimonBuildCatalog.Data.items.Where(item=>DigimonBuildCatalog.IsComponent(item.id)).Select(item=>item.id).ToArray();}
    public static int[] RecipeResults(int component)
    {
        if(!DigimonBuildCatalog.IsComponent(component))return new int[0];
        return ComponentIds().Select(partner=>DigimonBuildCatalog.Combine(component,partner)).Where(id=>id>=0).ToArray();
    }
    public static int[] CraftSlots(int id,IList<int> inventory)
    {
        if(id<0||id>=DigimonBuildCatalog.Data.items.Length||inventory==null)return new int[0];
        var recipe=DigimonBuildCatalog.Data.items[id].recipe;if(recipe==null||recipe.Length!=2)return new int[0];
        int first=-1,second=-1;
        for(int i=0;i<inventory.Count;i++)if(inventory[i]==recipe[0]){first=i;break;}
        for(int i=0;i<inventory.Count;i++)if(i!=first&&inventory[i]==recipe[1]){second=i;break;}
        return first<0||second<0?new int[0]:new[]{first,second};
    }
    public static int[] VisibleRecipes(int component,IEnumerable<int> inventory)
    {return RecipeResults(component).Where(id=>!craftableOnly||CanCraft(id,inventory)).ToArray();}
    public static RecipeMaterial[] RecipeRequirements(int id,IEnumerable<int> inventory)
    {
        if(id<0||id>=DigimonBuildCatalog.Data.items.Length)return new RecipeMaterial[0];
        var recipe=DigimonBuildCatalog.Data.items[id].recipe;
        if(recipe==null||recipe.Length!=2)return new RecipeMaterial[0];
        var held=(inventory??Enumerable.Empty<int>()).ToArray();
        return recipe.Distinct().Select(material=>new RecipeMaterial{item=material,required=recipe.Count(value=>value==material),held=held.Count(value=>value==material)}).ToArray();
    }
    public static bool CanCraft(int id,IEnumerable<int> inventory)
    {
        var requirements=RecipeRequirements(id,inventory);
        return requirements.Length>0&&requirements.All(material=>material.Ready);
    }
    public static string Materials(int id,IEnumerable<int> inventory)
    {
        var requirements=RecipeRequirements(id,inventory);if(requirements.Length==0)return "";
        var missing=requirements.Where(material=>!material.Ready).Select(material=>DigimonBuildCatalog.Data.items[material.item].name+(material.Missing>1?" ×"+material.Missing:""));
        return requirements.All(material=>material.Ready)?"보관함 재료로 합성 가능":"부족: "+string.Join(" + ",missing.ToArray());
    }
    public static string Stats(int id)
    {
        var item=DigimonBuildCatalog.Data.items[ValidId(id)];var b=item.bonus;var list=new List<string>();
        if(item.teamSize>0)list.Add("보유 시 배치 인원 +"+item.teamSize);
        if(!string.IsNullOrEmpty(item.grantsTrait))list.Add(DigimonBuildCatalog.Find(item.grantsTrait).name+" 시너지 부여");
        if(b.attack>0)list.Add("공격력 +"+(b.attack*100).ToString("0.#")+"%");
        if(b.abilityPower>0)list.Add("주문력 +"+b.abilityPower.ToString("0.#"));
        if(b.health>0)list.Add((item.teamSize>0?"장착 시 ":"")+"체력 +"+b.health.ToString("0.#"));
        if(b.hp>0)list.Add("최대 체력 +"+(b.hp*100).ToString("0.#")+"%");
        if(b.armor>0)list.Add("방어력 +"+b.armor.ToString("0.#"));
        if(b.magicResist>0)list.Add("마법 저항 +"+b.magicResist.ToString("0.#"));
        if(b.speed>0)list.Add("공격 속도 +"+(b.speed*100).ToString("0.#")+"%");
        if(b.startMana>0)list.Add("시작 마나 +"+b.startMana.ToString("0.#"));
        return list.Count>0?string.Join(" · ",list.ToArray()):DigimonBuildCatalog.IsComponent(id)?"단독 능력치 없음 · 시너지 / 인원 확장 합성 재료":"장비 슬롯을 사용하지 않는 소모품";
    }

    // Selecting a result retains its source material and recipe list, keeping the
    // inspection surface stable. Call BeginGuide for each explicit new open.
    public static void BeginGuide(int focus)
    {
        focus=ValidId(focus);observedFocus=focus;craftableOnly=false;
        var recipe=DigimonBuildCatalog.Data.items[focus].recipe;
        materialFocus=DigimonBuildCatalog.IsComponent(focus)?focus:recipe!=null&&recipe.Length==2?recipe[0]:-1;
    }
    static void SelectItem(int id,ref int focus)
    {
        focus=ValidId(id);observedFocus=focus;
        if(DigimonBuildCatalog.IsComponent(focus))materialFocus=focus;
    }

    public static Rect PopupRect(Vector2 anchor,Rect canvas,int focus)
    {
        float scale=Mathf.Min(1,Mathf.Min(Mathf.Max(1,canvas.width-24)/GuideWidth,Mathf.Max(1,canvas.height-24)/GuideHeight));
        float width=GuideWidth*scale,height=GuideHeight*scale;
        float x=anchor.x+14;if(x+width>canvas.xMax-12)x=anchor.x-width-14;
        return new Rect(Mathf.Clamp(x,canvas.x+12,canvas.xMax-width-12),Mathf.Clamp(anchor.y-36,canvas.y+12,canvas.yMax-height-12),width,height);
    }
    public static Rect CloseRect(Rect r){return At(r,1025,23,32,32);}
    public static Rect BackRect(Rect r){return At(r,801,25,198,32);}
    public static Rect ComponentRect(Rect r,int index){return At(r,36+76*index,121,64,60);}
    public static Rect RecipeRowRect(Rect r,int index){return At(r,35,306+65*index,455,60);}
    public static Rect MaterialRect(Rect r,int index){return At(r,544+253*index,491,241,72);}
    public static Rect FilterRect(Rect r){return At(r,340,273,150,27);}
    public static Rect CraftRect(Rect r){return At(r,781,615,256,35);}
    public static bool RightClick(Rect r)
    {
        Event e=Event.current;
        if(!GUI.enabled||e==null||e.type!=EventType.MouseDown||e.button!=1||!r.Contains(e.mousePosition))return false;
        e.Use();return true;
    }

    static bool TextButton(Rect rect,string text,int size,bool selected=false)
    {
        bool hover=GUI.enabled&&rect.Contains(Event.current.mousePosition);
        BattleHudTheme.Round(rect,Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,selected?.22f:hover?.13f:0));
        BattleHudTheme.Label(rect,text,size,selected?BattleHudTheme.Teal:BattleHudTheme.Text,true,TextAnchor.MiddleCenter);
        return GUI.Button(rect,GUIContent.none,GUIStyle.none);
    }

    public static bool DrawGuide(Rect r,ref int focus,IEnumerable<int> inventory,System.Action<int> craft=null,bool canManage=false,string unavailable="현재 합성 불가")
    {
        Styles(r);focus=ValidId(focus);if(observedFocus!=focus)BeginGuide(focus);
        int[] held=(inventory??Enumerable.Empty<int>()).ToArray();Event e=Event.current;
        if((e.type==EventType.KeyDown&&e.keyCode==KeyCode.Escape)||(e.type==EventType.MouseDown&&(e.button==1||!r.Contains(e.mousePosition))))
        {e.Use();return false;}
        BattleHudTheme.Surface(r,true,BattleHudTheme.Teal);
        BattleHudTheme.Label(At(r,24,17,725,18),"EQUIPMENT / INSPECT & PLAN",Font(r,11),BattleHudTheme.Teal,true);
        BattleHudTheme.Label(At(r,22,42,225,32),"장비 정보와 합성",Font(r,25),BattleHudTheme.Text,true);
        BattleHudTheme.Label(At(r,260,47,505,23),"재료와 완성 장비를 눌러 옵션을 확인하세요",Font(r,12),BattleHudTheme.Muted);
        if(TextButton(CloseRect(r),"×",Font(r,21))){e.Use();return false;}
        if(materialFocus>=0&&focus!=materialFocus&&TextButton(BackRect(r),"← 재료 옵션 보기",Font(r,12)))SelectItem(materialFocus,ref focus);
        ArenaInterface.Fill(At(r,22,88,1036,1),BattleHudTheme.Line);
        BattleHudTheme.Surface(At(r,20,104,486,613));
        BattleHudTheme.Surface(At(r,522,104,538,613),false,DigimonEquipmentArt.Tint(focus));
        DrawComponents(r,held,ref focus);
        DrawRecipes(r,held,ref focus);
        DrawSelectedItem(r,held,ref focus,craft,canManage,unavailable);
        ArenaInterface.Fill(At(r,22,739,1036,1),BattleHudTheme.Line);
        BattleHudTheme.Label(At(r,24,750,650,20),"장비 클릭은 효과 조회 · 합성하기 버튼은 보관함의 재료 2개를 소비합니다",Font(r,12),BattleHudTheme.Muted);
        BattleHudTheme.Label(At(r,697,750,360,20),"우클릭 / ESC / 바깥 클릭으로 닫기",Font(r,12),BattleHudTheme.Muted,false,TextAnchor.MiddleRight);
        if(e.isMouse||e.type==EventType.ScrollWheel||e.type==EventType.KeyDown||e.type==EventType.KeyUp)e.Use();
        return true;
    }

    static void DrawComponents(Rect r,int[] held,ref int focus)
    {
        var components=ComponentIds();
        for(int i=0;i<components.Length;i++)
        {
            int id=components[i],count=held.Count(value=>value==id);Rect slot=ComponentRect(r,i);bool selected=id==materialFocus;
            BattleHudTheme.Round(slot,selected?Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.23f):BattleHudTheme.Raised);
            DigimonEquipmentArt.Draw(At(r,49+76*i,125,38,38),id,selected);
            BattleHudTheme.Label(At(r,36+76*i,164,64,15),"보유 "+count,Font(r,10),count>0?BattleHudTheme.Text:BattleHudTheme.Muted,true,TextAnchor.MiddleCenter);
            if(GUI.Button(slot,new GUIContent("",DigimonBuildCatalog.Data.items[id].name+" · 클릭하여 조합 보기"),GUIStyle.none))SelectItem(id,ref focus);
        }
    }

    static void DrawRecipes(Rect r,int[] held,ref int focus)
    {
        if(materialFocus<0)
        {
            BattleHudTheme.Label(At(r,37,203,450,30),"합성하지 않는 소모품",Font(r,22),BattleHudTheme.Text,true);
            GUI.Label(At(r,38,249,442,64),"위의 재료를 선택하면 만들 수 있는 장비와 필요한 수량을 살펴볼 수 있습니다.",body);
            DigimonEquipmentArt.Draw(At(r,177,367,168,168),focus);
            BattleHudTheme.Label(At(r,37,579,450,26),"소모품의 효과와 사용법은 오른쪽에서 확인하세요",Font(r,12),BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
            return;
        }
        var component=DigimonBuildCatalog.Data.items[materialFocus];
        BattleHudTheme.FitLabel(At(r,36,198,454,31),component.name,Font(r,23),BattleHudTheme.Text,true);
        GUI.Label(At(r,37,237,451,40),Stats(materialFocus),small);
        int readyCount=RecipeResults(materialFocus).Count(id=>CanCraft(id,held));
        BattleHudTheme.Label(At(r,37,280,300,19),"조합 결과 · 합성 가능 "+readyCount+"개",Font(r,11),BattleHudTheme.Muted);
        if(TextButton(FilterRect(r),craftableOnly?"✓ 합성 가능만":"합성 가능만 보기",Font(r,11),craftableOnly))craftableOnly=!craftableOnly;
        var results=VisibleRecipes(materialFocus,held);
        if(results.Length==0)
        {
            BattleHudTheme.Label(At(r,54,380,414,36),"이 재료로 합성할 장비가 없습니다",Font(r,18),BattleHudTheme.Muted,true);
            GUI.Label(At(r,55,432,413,95),"다른 재료를 선택하거나 필터를 꺼서 전체 조합과 필요한 재료를 확인하세요.",body);
        }
        for(int i=0;i<results.Length;i++)
        {
            int made=results[i];var result=DigimonBuildCatalog.Data.items[made];
            int partner=result.recipe[0]==materialFocus?result.recipe[1]:result.recipe[0];
            bool ready=CanCraft(made,held),selected=focus==made;Rect line=RecipeRowRect(r,i);
            bool hover=GUI.enabled&&line.Contains(Event.current.mousePosition);
            BattleHudTheme.Round(line,selected?Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.23f):hover?Color.Lerp(BattleHudTheme.Raised,BattleHudTheme.Teal,.10f):BattleHudTheme.Raised);
            ArenaInterface.Fill(At(r,36,314+65*i,3,44),selected?BattleHudTheme.Teal:ready?BattleHudTheme.Gold:BattleHudTheme.Line);
            DigimonEquipmentArt.Draw(At(r,45,319+65*i,33,33),partner);
            BattleHudTheme.Label(At(r,81,322+65*i,21,24),"→",Font(r,13),BattleHudTheme.Muted,false,TextAnchor.MiddleCenter);
            DigimonEquipmentArt.Draw(At(r,107,316+65*i,40,40),made,selected);
            BattleHudTheme.FitLabel(At(r,158,313+65*i,234,22),result.name,Font(r,15),BattleHudTheme.Text,true);
            BattleHudTheme.Badge(At(r,403,315+65*i,78,20),ready?"합성 가능":"재료 부족",ready?BattleHudTheme.Teal:BattleHudTheme.Muted,Font(r,10));
            BattleHudTheme.FitLabel(At(r,158,340+65*i,325,17),ready?result.role:Materials(made,held),Font(r,11),BattleHudTheme.Muted,false);
            if(GUI.Button(line,new GUIContent("",result.name+" · 효과 확인\n"+Stats(made)),GUIStyle.none))SelectItem(made,ref focus);
        }
    }

    static void DrawSelectedItem(Rect r,int[] held,ref int focus,System.Action<int> craft,bool canManage,string unavailable)
    {
        var item=DigimonBuildCatalog.Data.items[focus];bool component=DigimonBuildCatalog.IsComponent(focus);
        var requirements=RecipeRequirements(focus,held);bool craftable=requirements.Length>0,ready=craftable&&requirements.All(material=>material.Ready);
        DigimonEquipmentArt.Draw(At(r,545,124,80,80),focus);
        BattleHudTheme.Label(At(r,644,124,391,17),component?"COMPONENT / 재료 옵션":item.kind=="utility"?"UTILITY / 소모품":"COMPLETED / 완성 장비",Font(r,10),BattleHudTheme.Teal,true);
        BattleHudTheme.FitLabel(At(r,642,151,394,32),item.name,Font(r,25),BattleHudTheme.Text,true);
        BattleHudTheme.Label(At(r,644,190,393,19),item.role+"   ·   보관함 "+held.Count(id=>id==item.id)+"개",Font(r,12),BattleHudTheme.Muted);
        BattleHudTheme.Round(At(r,542,230,498,86),BattleHudTheme.Raised);
        BattleHudTheme.Label(At(r,557,241,468,17),"기본 능력치",Font(r,11),BattleHudTheme.Teal,true);
        GUI.Label(At(r,557,266,468,42),Stats(focus),body);
        BattleHudTheme.Round(At(r,542,328,498,125),BattleHudTheme.Raised);
        BattleHudTheme.Label(At(r,557,342,468,17),component?"재료 효과":"고유 효과",Font(r,11),BattleHudTheme.Gold,true);
        GUI.Label(At(r,557,371,468,71),string.IsNullOrEmpty(item.passive)?item.description:item.passive,body);
        BattleHudTheme.Label(At(r,544,466,300,19),craftable?"조합식 · 재료를 누르면 조합 목록으로":"사용 안내",Font(r,11),BattleHudTheme.Muted);
        if(craftable)
        {
            for(int i=0;i<requirements.Length;i++)
            {
                var material=requirements[i];var source=DigimonBuildCatalog.Data.items[material.item];Rect cell=MaterialRect(r,i);
                BattleHudTheme.Round(cell,BattleHudTheme.Raised);
                DigimonEquipmentArt.Draw(At(r,554+253*i,508,38,38),material.item);
                BattleHudTheme.FitLabel(At(r,600+253*i,502,175,20),source.name,Font(r,13),BattleHudTheme.Text,true);
                BattleHudTheme.Label(At(r,600+253*i,532,175,19),"보유 "+material.held+" / 필요 "+material.required,Font(r,12),material.Ready?BattleHudTheme.Teal:BattleHudTheme.Gold,true);
                if(GUI.Button(cell,new GUIContent("",source.name+"의 조합 보기"),GUIStyle.none))SelectItem(material.item,ref focus);
            }
            if(requirements.Length==1)BattleHudTheme.Badge(MaterialRect(r,1),"같은 재료 2개가 필요합니다",BattleHudTheme.Muted,Font(r,12));
            BattleHudTheme.Badge(At(r,544,575,494,29),Materials(item.id,held),ready?BattleHudTheme.Teal:BattleHudTheme.Gold,Font(r,12));
        }
        else
        {
            GUI.Label(At(r,545,501,493,92),component?"왼쪽의 완성 장비를 선택하면 능력치와 고유 효과, 필요한 재료 수량을 확인할 수 있습니다.\n\n실제 합성은 보관함에서 재료 두 개를 선택하세요.":"장비를 장착한 아군에게 사용하면 장비를 모두 회수합니다.\n\n실제로 장비를 회수할 때만 소모됩니다. 합성에는 사용하지 않습니다.",small);
        }
        BattleHudTheme.Label(At(r,544,621,494,18),"활용 팁",Font(r,11),BattleHudTheme.Teal,true);
        GUI.Label(At(r,545,657,493,48),item.usage??"",small);
        if(craftable&&craft!=null)
        {
            bool enabled=GUI.enabled;GUI.enabled=enabled&&canManage&&ready;
            string action=!canManage?unavailable:!ready?"재료 부족":"합성하기 · 재료 2개";
            bool clicked=TextButton(CraftRect(r),action,Font(r,13),ready&&canManage);
            GUI.enabled=enabled;
            if(clicked)craft(item.id);
        }
    }
}

