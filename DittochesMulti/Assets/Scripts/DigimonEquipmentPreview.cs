using System.Collections.Generic;
using UnityEngine;

/// <summary>Read-only projection using the same recipe and stat resolver as equipment application.</summary>
public static class DigimonEquipmentPreview
{
    public sealed class Projection
    {
        public DigimonBuildCatalog.EquipmentChange change;
        public DigimonSkillCatalog.Stats before,after;
        public float damageBefore,damageAfter;
    }
    public static Projection Create(string id,int star,IEnumerable<string> team,IEnumerable<int> items,int incoming)
    {
        var skill=DigimonSkillCatalog.Find(id);var change=DigimonBuildCatalog.PreviewEquipment(items,incoming);
        var before=new DigimonSkillCatalog.Stats(skill,star,DigimonBuildCatalog.Resolve(id,team,items));
        var after=new DigimonSkillCatalog.Stats(skill,star,DigimonBuildCatalog.Resolve(id,team,change.items));
        return new Projection{change=change,before=before,after=after,
            damageBefore=skill.Damage(star,before.attack,before.abilityPower),damageAfter=skill.Damage(star,after.attack,after.abilityPower)};
    }
    static void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    public static void Draw(Rect r,string name,DigimonSkillCatalog.Entry skill,Projection p,string context,string blocked="")
    {
        Fill(r,new Color(.025f,.045f,.056f,.99f));Fill(new Rect(r.x,r.y,r.width,2),new Color(.72f,.59f,.30f));
        var body=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};body.normal.textColor=new Color(.82f,.89f,.91f);
        var heading=new GUIStyle(body){fontSize=17,fontStyle=FontStyle.Bold};
        float x=r.x+14,w=r.width-28,y=r.y+14;
        GUI.Label(new Rect(x,y,w,26),"장착 미리보기",heading);
        GUI.Label(new Rect(x,y+34,w,42),name,heading);
        bool allowed=p.change.allowed&&string.IsNullOrEmpty(blocked);
        var status=new GUIStyle(body);status.normal.textColor=allowed?new Color(.44f,.9f,.72f):new Color(1,.57f,.4f);
        GUI.Label(new Rect(x,y+84,w,65),string.IsNullOrEmpty(blocked)?p.change.message:blocked,status);
        if(!allowed){GUI.Label(new Rect(x,y+164,w,70),"능력치와 장비는 변경되지 않습니다.",body);return;}
        GUI.Label(new Rect(x,y+155,w,43),context+"\n현재 → 장착 후",body);
        y+=206;
        Row(x,ref y,w,"공격력",p.before.attack,p.after.attack,body);
        Row(x,ref y,w,"주문력",p.before.abilityPower,p.after.abilityPower,body);
        Row(x,ref y,w,"체력",p.before.health,p.after.health,body);
        Row(x,ref y,w,"방어력",p.before.armor,p.after.armor,body);
        Row(x,ref y,w,"마법 저항",p.before.magicResist,p.after.magicResist,body);
        Row(x,ref y,w,"공격 속도",p.before.speed,p.after.speed,body,"0.00");
        Row(x,ref y,w,"시작 마나",p.before.startMana,p.after.startMana,body);
        Fill(new Rect(x,y+5,w,1),new Color(.2f,.32f,.35f));y+=17;
        GUI.Label(new Rect(x,y,w,40),skill.name+" · "+skill.DamageLabel,body);y+=43;
        Row(x,ref y,w,"스킬 피해",p.damageBefore,p.damageAfter,body);
        GUI.Label(new Rect(x,y+8,w,55),"대상 1명 기준 · 모든 타격 합계\n방어·조건부 효과 적용 전",body);y+=70;
        if(p.change.result>=0)GUI.Label(new Rect(x,y,w,Mathf.Max(0,r.yMax-y-8)),DigimonBuildCatalog.Data.items[p.change.result].description,body);
        else GUI.Label(new Rect(x,y,w,48),"추출기 1개 소모 · 장비 전부 반환",body);
    }
    static void Row(float x,ref float y,float w,string label,float before,float after,GUIStyle style,string format="0.#")
    {
        var value=new GUIStyle(style){alignment=TextAnchor.MiddleRight,wordWrap=false};
        if(Mathf.Abs(after-before)>.005f)value.normal.textColor=after>before?new Color(.44f,.9f,.72f):new Color(1,.57f,.4f);
        GUI.Label(new Rect(x,y,w,27),label,style);
        GUI.Label(new Rect(x+63,y,w-63,27),before.ToString(format)+" → "+after.ToString(format),value);y+=29;
    }
}
