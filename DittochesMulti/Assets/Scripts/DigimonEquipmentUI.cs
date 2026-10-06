using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class DigimonEquipmentUI
{
    static GUIStyle body,small,title,tag;
    static readonly ArenaInterface ui=new ArenaInterface();
    static void Styles()
    {
        if(body!=null)return;
        body=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};body.normal.textColor=new Color(.85f,.91f,.95f);
        small=new GUIStyle(body){fontSize=13};small.normal.textColor=new Color(.56f,.69f,.76f);
        title=new GUIStyle(body){fontSize=23,fontStyle=FontStyle.Bold};title.normal.textColor=Color.white;
        tag=new GUIStyle(small){fontSize=12,alignment=TextAnchor.MiddleCenter};
    }
    public static bool CanCraft(int id,IEnumerable<int> inventory)
    {
        var recipe=DigimonBuildCatalog.Data.items[id].recipe;if(recipe==null||recipe.Length!=2)return false;
        var held=inventory.ToList();foreach(int material in recipe){if(!held.Remove(material))return false;}return true;
    }
    public static string Materials(int id,IEnumerable<int> inventory)
    {
        var recipe=DigimonBuildCatalog.Data.items[id].recipe;
        if(recipe==null||recipe.Length!=2)return "";
        var held=inventory.ToList();var missing=new List<string>();
        foreach(int material in recipe)if(!held.Remove(material))missing.Add(DigimonBuildCatalog.Data.items[material].name);
        return missing.Count==0?"보관함 재료로 합성 가능":"부족: "+string.Join(" + ",missing.ToArray());
    }
    public static string Stats(int id)
    {
        var b=DigimonBuildCatalog.Data.items[id].bonus;var list=new List<string>();
        if(b.attack>0)list.Add("공격력 +"+(b.attack*100).ToString("0.#")+"%");
        if(b.abilityPower>0)list.Add("주문력 +"+b.abilityPower.ToString("0.#"));
        if(b.health>0)list.Add("체력 +"+b.health.ToString("0.#"));
        if(b.armor>0)list.Add("방어력 +"+b.armor.ToString("0.#"));
        if(b.magicResist>0)list.Add("마법 저항 +"+b.magicResist.ToString("0.#"));
        if(b.speed>0)list.Add("공격 속도 +"+(b.speed*100).ToString("0.#")+"%");
        if(b.startMana>0)list.Add("시작 마나 +"+b.startMana.ToString("0.#"));
        return list.Count>0?string.Join(" · ",list.ToArray()):"장비 슬롯을 사용하지 않는 소모품";
    }
    public static bool DrawGuide(Rect r,ref int focus,IEnumerable<int> inventory)
    {
        Styles();focus=Mathf.Clamp(focus,0,14);int[] held=inventory.ToArray();
        if(Event.current.type==EventType.KeyDown&&Event.current.keyCode==KeyCode.Escape){Event.current.Use();return false;}
        ArenaInterface.Fill(r,new Color(.018f,.03f,.048f,.995f));ArenaInterface.Fill(new Rect(r.x,r.y,r.width,3),new Color(.23f,.76f,.83f));
        GUI.Label(new Rect(r.x+24,r.y+16,r.width-100,34),"디지털 무장 도감",title);
        GUI.Label(new Rect(r.x+25,r.y+53,r.width-75,24),"재료 4종 · 완성 무장 10종 · 회수 도구 1종",small);
        if(ui.Button(new Rect(r.xMax-54,r.y+16,34,32),new GUIContent("×")))return false;
        GUI.Label(new Rect(r.x+25,r.y+100,298,28),"데이터 융합 · 전체 조합표",body);
        GUI.Label(new Rect(r.x+25,r.y+133,298,22),"행 + 열 재료로 합성 · 클릭하면 상세",small);
        float gx=r.x+25,gy=r.y+170;const float step=54;
        for(int a=0;a<4;a++)
        {
            Rect column=new Rect(gx+(a+1)*step,gy,46,46),row=new Rect(gx,gy+(a+1)*step,46,46);
            bool columnClick=DigimonEquipmentArt.Button(column,a,focus==a),rowClick=DigimonEquipmentArt.Button(row,a,focus==a);
            if(columnClick||rowClick)focus=a;
            for(int b=0;b<4;b++)
            {
                int result=DigimonBuildCatalog.Combine(a,b);Rect cell=new Rect(gx+(b+1)*step,gy+(a+1)*step,46,46);
                bool ready=CanCraft(result,held);Color old=GUI.color;
                if(focus<4&&a!=focus&&b!=focus)GUI.color=new Color(old.r,old.g,old.b,old.a*.45f);
                if(DigimonEquipmentArt.Button(cell,result,focus==result))focus=result;GUI.color=old;
                if(ready)ArenaInterface.Fill(new Rect(cell.x+3,cell.yMax-4,cell.width-6,3),new Color(.3f,1,.64f));
            }
        }
        GUI.Label(new Rect(gx,gy+277,290,37),"초록 밑줄: 보관함 재료로 합성 가능\n같은 재료 조합은 2개 필요",small);
        if(DigimonEquipmentArt.Button(new Rect(gx,r.y+507,46,46),14,focus==14))focus=14;
        GUI.Label(new Rect(gx+58,r.y+508,230,48),"데이터 추출기\n장비 전부 회수 · 1회 사용",small);
        float dx=r.x+343,dw=r.width-367;
        ArenaInterface.Fill(new Rect(dx-13,r.y+92,1,r.height-118),new Color(.12f,.22f,.28f));
        var item=DigimonBuildCatalog.Data.items[focus];Color tint=DigimonEquipmentArt.Tint(focus);
        DigimonEquipmentArt.Draw(new Rect(dx,r.y+99,78,78),focus);
        GUI.Label(new Rect(dx+94,r.y+99,dw-94,34),item.name,title);
        GUI.Label(new Rect(dx+94,r.y+136,dw-94,42),(focus<4?"융합 재료":focus<14?"완성 무장":"회수 도구")+" · "+item.role,body);
        ArenaInterface.Fill(new Rect(dx,r.y+198,dw,119),new Color(.038f,.064f,.09f));
        ArenaInterface.Fill(new Rect(dx,r.y+198,3,119),tint);
        GUI.Label(new Rect(dx+13,r.y+207,dw-26,48),Stats(focus),body);
        GUI.Label(new Rect(dx+13,r.y+257,dw-26,56),string.IsNullOrEmpty(item.passive)?"재료 2개를 융합하면 전용 효과가 해금됩니다.":item.passive,small);
        GUI.Label(new Rect(dx,r.y+333,dw,22),"활용 방향",small);
        GUI.Label(new Rect(dx,r.y+359,dw,59),item.usage??item.role,body);
        if(item.recipe.Length==2)
        {
            float y=r.y+431;
            for(int i=0;i<2;i++)
            {
                int material=item.recipe[i];Rect icon=new Rect(dx+i*94,y,54,54);
                if(DigimonEquipmentArt.Button(icon,material))focus=material;
                GUI.Label(new Rect(icon.x,icon.yMax+2,82,23),"보유 "+held.Count(v=>v==material),small);
            }
            GUI.Label(new Rect(dx+62,y+15,25,25),"+",body);GUI.Label(new Rect(dx+162,y+15,30,25),"→",body);
            DigimonEquipmentArt.Draw(new Rect(dx+204,y,54,54),item.id);
            GUI.Label(new Rect(dx,y+86,dw,46),Materials(item.id,held),body);
        }
        else GUI.Label(new Rect(dx,r.y+439,dw,91),focus<4?"보관함에 "+held.Count(v=>v==item.id)+"개 보유\n왼쪽에서 같은 행 또는 열의 완성 무장을 확인하세요.":"장비가 있는 아군에게 사용하세요.\n유닛은 유지하고 장비만 전부 보관함으로 회수합니다.",body);
        GUI.Label(new Rect(dx,r.y+576,dw,40),item.flavor,small);
        GUI.Label(new Rect(r.x+25,r.yMax-29,295,22),"유닛당 2칸 · 판매/합성 초과분은 반환",small);
        return true;
    }
}
