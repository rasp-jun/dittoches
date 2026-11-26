using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Read-only projection using the same recipe and stat resolver as equipment application.</summary>
public static class DigimonEquipmentPreview
{
    public sealed class Projection
    {
        public DigimonBuildCatalog.EquipmentChange change;
        public DigimonBuildCatalog.Bonus beforeBonus,afterBonus;
        public int incoming;
        public DigimonSkillCatalog.Stats before,after;
        public float damageBefore,damageAfter,baseSpeed,combatAge;
        public bool combat;
        public float currentHealth,projectedHealth,currentMana;
    }
    public static void SetCombatState(Projection p,float health,float maxHealth,float mana,float age=0)
    {
        p.combat=true;p.currentHealth=health;p.currentMana=mana;p.combatAge=Mathf.Max(0,age);
        p.projectedHealth=p.change.allowed?Mathf.Clamp01(health/Mathf.Max(1,maxHealth))*p.after.health:health;
        // Resolve each side at the same combat age. Adding/removing an emblem may also change ramp speed.
        p.before.speed=DigimonCombatMath.AttackSpeed(p.baseSpeed,p.beforeBonus.speed,p.beforeBonus.rampSpeed,p.combatAge);
        p.after.speed=DigimonCombatMath.AttackSpeed(p.baseSpeed,p.afterBonus.speed,p.afterBonus.rampSpeed,p.combatAge);
    }
    public static Projection Create(string id,int star,IEnumerable<string> team,IEnumerable<int> items,int incoming)
    {return Create(id,star,team.Select(uid=>new DigimonBuildCatalog.Member(uid,uid==id?items:null)),items,incoming,true);}
    public static Projection Create(string id,int star,IEnumerable<DigimonBuildCatalog.Member> team,IEnumerable<int> items,int incoming,bool onBoard=true)
    {
        var skill=DigimonSkillCatalog.Find(id);var change=DigimonBuildCatalog.PreviewEquipment(items,incoming,id);
        var beforeTeam=team.ToArray();var afterTeam=(DigimonBuildCatalog.Member[])beforeTeam.Clone();
        int index=System.Array.FindIndex(afterTeam,u=>u!=null&&u.id==id&&u.items.SequenceEqual(items));
        if(index<0)index=System.Array.FindIndex(afterTeam,u=>u!=null&&u.id==id);
        if(onBoard&&index>=0)afterTeam[index]=new DigimonBuildCatalog.Member(id,change.items);
        var beforeBonus=DigimonBuildCatalog.Resolve(id,beforeTeam,items);var afterBonus=DigimonBuildCatalog.Resolve(id,afterTeam,change.items);
        var before=new DigimonSkillCatalog.Stats(skill,star,beforeBonus);
        var after=new DigimonSkillCatalog.Stats(skill,star,afterBonus);
        if(change.allowed&&change.result>=0&&!string.IsNullOrEmpty(DigimonBuildCatalog.Data.items[change.result].grantsTrait))
        {
            var trait=DigimonBuildCatalog.Find(DigimonBuildCatalog.Data.items[change.result].grantsTrait);
            change.message+="\n"+trait.name+" "+DigimonBuildCatalog.Count(trait,beforeTeam)+" → "+DigimonBuildCatalog.Count(trait,afterTeam);
        }
        return new Projection{incoming=incoming,change=change,before=before,after=after,beforeBonus=beforeBonus,afterBonus=afterBonus,baseSpeed=skill.attackSpeed,
            damageBefore=skill.Damage(star,before.attack,before.abilityPower),damageAfter=skill.Damage(star,after.attack,after.abilityPower)};
    }
    static GUIStyle body,heading,positive,negative,value,valueUp,valueDown,footnote;
    static void Styles()
    {
        if(body!=null)return;
        body=new GUIStyle(GUI.skin.label){fontSize=13,wordWrap=true};body.normal.textColor=new Color(.82f,.89f,.91f);
        heading=new GUIStyle(body){fontSize=17,fontStyle=FontStyle.Bold};
        positive=new GUIStyle(body);positive.normal.textColor=new Color(.44f,.9f,.72f);
        negative=new GUIStyle(body);negative.normal.textColor=new Color(1,.57f,.4f);
        value=new GUIStyle(body){alignment=TextAnchor.MiddleRight,wordWrap=false};
        valueUp=new GUIStyle(value);valueUp.normal.textColor=positive.normal.textColor;
        valueDown=new GUIStyle(value);valueDown.normal.textColor=negative.normal.textColor;
        footnote=new GUIStyle(body){fontSize=12};
    }
    static void Fill(Rect r,Color color){Color old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    public static void Draw(Rect r,string name,DigimonSkillCatalog.Entry skill,Projection p,string context,string blocked="")
    {
        Styles();
        Fill(r,new Color(.025f,.045f,.056f,.99f));Fill(new Rect(r.x,r.y,r.width,2),new Color(.72f,.59f,.30f));
        float x=r.x+14,w=r.width-28,y=r.y+14;
        GUI.Label(new Rect(x,y,w,26),"장착 미리보기",heading);
        GUI.Label(new Rect(x,y+34,w,42),name,heading);
        bool allowed=p.change.allowed&&string.IsNullOrEmpty(blocked);
        var status=allowed?positive:negative;
        DigimonEquipmentArt.Draw(new Rect(x,y+85,40,40),p.change.result>=0?p.change.result:p.incoming);
        GUI.Label(new Rect(x+48,y+84,w-48,65),string.IsNullOrEmpty(blocked)?p.change.message:blocked,status);
        if(!allowed){GUI.Label(new Rect(x,y+164,w,70),"능력치와 장비는 변경되지 않습니다.",body);return;}
        GUI.Label(new Rect(x,y+155,w,43),context+"\n현재 → 장착 후",body);
        y+=199;
        Row(x,ref y,w,"공격력",p.before.attack,p.after.attack,body);
        Row(x,ref y,w,"주문력",p.before.abilityPower,p.after.abilityPower,body);
        Row(x,ref y,w,"최대 체력",p.before.health,p.after.health,body);
        if(p.combat)Row(x,ref y,w,"현재 체력",p.currentHealth,p.projectedHealth,body);
        Row(x,ref y,w,"방어력",p.before.armor,p.after.armor,body);
        Row(x,ref y,w,"마법 저항",p.before.magicResist,p.after.magicResist,body);
        Row(x,ref y,w,"공격 속도",p.before.speed,p.after.speed,body,"0.00");
        if(p.combat)Row(x,ref y,w,"현재 마나",p.currentMana,p.currentMana,body);
        else Row(x,ref y,w,"시작 마나",p.before.startMana,p.after.startMana,body);
        Fill(new Rect(x,y+5,w,1),new Color(.2f,.32f,.35f));y+=15;
        GUI.Label(new Rect(x,y,w,35),skill.name+" · "+skill.DamageLabel,body);y+=36;
        Row(x,ref y,w,"스킬 피해",p.damageBefore,p.damageAfter,body);
        GUI.Label(new Rect(x,y+5,w,52),"평타: 공격력 100% · 물리 피해\n스킬: 대상당 모든 타격 합계\n방어·조건부 효과 적용 전",footnote);y+=61;
        if(p.combat)GUI.Label(new Rect(x,r.yMax-70,w,64),"전투 공속은 현재 야수 중첩 포함\n체력 비율·마나 유지\n시작 효과는 다음 전투에 적용",footnote);
        else if(p.change.result>=0)GUI.Label(new Rect(x,y,w,Mathf.Max(0,r.yMax-y-8)),DigimonBuildCatalog.Data.items[p.change.result].description,body);
        else GUI.Label(new Rect(x,y,w,48),"추출기 1개 소모 · 장비 전부 반환",body);
    }
    static void Row(float x,ref float y,float w,string label,float before,float after,GUIStyle style,string format="0.#")
    {
        var number=Mathf.Abs(after-before)<=.005f?value:after>before?valueUp:valueDown;
        GUI.Label(new Rect(x,y,w,25),label,style);
        GUI.Label(new Rect(x+63,y,w-63,25),before.ToString(format)+" → "+after.ToString(format),number);y+=26;
    }
}
