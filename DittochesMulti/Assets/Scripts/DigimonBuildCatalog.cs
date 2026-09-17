using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Shared, versioned trait/item data. Numerical effects are game balance, not anime canon.</summary>
public static class DigimonBuildCatalog
{
    [Serializable] public sealed class Bonus
    {
        public float health, hp, attack, speed, abilityPower, armor, magicResist, manaRegen, startMana, manaOnAttack, regen,
            lifesteal, reduction, startShield, lowShield, castHeal, healPower, thirdHit,
            highHealthDamage, castShield, thirdStun, teamShield;
        public void Add(Bonus b)
        {
            if(b==null)return;
            health+=b.health;hp+=b.hp;attack+=b.attack;speed+=b.speed;abilityPower+=b.abilityPower;armor+=b.armor;magicResist+=b.magicResist;
            manaRegen+=b.manaRegen;startMana+=b.startMana;manaOnAttack+=b.manaOnAttack;regen+=b.regen;
            lifesteal+=b.lifesteal;reduction+=b.reduction;startShield+=b.startShield;lowShield+=b.lowShield;
            castHeal+=b.castHeal;healPower+=b.healPower;thirdHit+=b.thirdHit;highHealthDamage+=b.highHealthDamage;
            castShield+=b.castShield;thirdStun+=b.thirdStun;teamShield+=b.teamShield;
        }
    }
    [Serializable] public sealed class Tier { public int count; public string text; public Bonus bonus; }
    [Serializable] public sealed class Trait
    {
        public string id,name,category,description;public Tier[] tiers;public string[] members;
        public int Level(int count){return tiers.Count(t=>count>=t.count);}
        public int Target(int count){var next=tiers.FirstOrDefault(t=>count<t.count);return (next??tiers[tiers.Length-1]).count;}
    }
    [Serializable] public sealed class Item
    {public int id;public string name,icon,kind,description,source,role,flavor;public int[] recipe;public Bonus bonus;}
    [Serializable] public sealed class Document { public int version;public Trait[] traits;public Item[] items; }
    static Document data;
    public static Document Data
    {
        get
        {
            if(data!=null)return data;
            var asset=Resources.Load<TextAsset>("DigimonBuilds");
#if DITTOCHES_PORTABLE_PREVIEW
            string json=asset!=null?asset.text:System.IO.File.ReadAllText(System.IO.Path.Combine(Application.streamingAssetsPath,"DigimonBuilds.json"));
#else
            if(asset==null)throw new InvalidOperationException("Missing Resources/DigimonBuilds.json");
            string json=asset.text;
#endif
            data=JsonUtility.FromJson<Document>(json);return data;
        }
    }
    public static IEnumerable<Trait> ForUnit(string id){return Data.traits.Where(t=>t.members.Contains(id));}
    public static Trait Find(string name){return Data.traits.FirstOrDefault(t=>t.name==name||t.id==name);}
    public static int Count(Trait trait,IEnumerable<string> ids){return ids.Distinct().Count(id=>trait.members.Contains(id));}
    public static Bonus Resolve(string id,IEnumerable<string> team,IEnumerable<int> equipment)
    {
        var ids=team.Distinct().ToArray();var bonus=new Bonus();
        foreach(var trait in Data.traits)
        {
            int level=trait.Level(Count(trait,ids));if(level==0)continue;
            Bonus active=trait.tiers[level-1].bonus;
            if(trait.members.Contains(id))bonus.Add(active);
            // A team aura is applied once, never once per contributing unit.
            bonus.startShield+=active.teamShield;
        }
        foreach(int item in equipment)if(item>=0&&item<14)bonus.Add(Data.items[item].bonus);
        return bonus;
    }
    public static int Combine(int a,int b)
    {
        if(a<0||a>3||b<0||b>3)return -1;
        var item=Data.items.FirstOrDefault(i=>i.recipe!=null&&i.recipe.Length==2&&
            ((i.recipe[0]==a&&i.recipe[1]==b)||(i.recipe[0]==b&&i.recipe[1]==a)));
        return item==null?-1:item.id;
    }
    public sealed class EquipmentChange
    {
        public bool allowed,removed;public int result=-1;public int[] items;public string message;
    }
    public static EquipmentChange PreviewEquipment(IEnumerable<int> equipped,int item)
    {
        var items=equipped.ToList();var change=new EquipmentChange{items=items.ToArray()};
        if(item<0||item>=Data.items.Length){change.message="유효하지 않은 장비입니다";return change;}
        if(item==14)
        {
            change.allowed=items.Count>0;change.removed=change.allowed;
            change.message=change.allowed?"장비 "+items.Count+"개를 보관함으로 회수":"회수할 장비가 없습니다";
            if(change.allowed)change.items=new int[0];return change;
        }
        int partner=items.FindIndex(i=>i>=0&&i<4),result=partner>=0?Combine(items[partner],item):-1;
        if(result>=0){items[partner]=result;change.message=Data.items[result].name+" 자동 합성";}
        else if(items.Count>=2){change.message="장비 2칸이 가득 찼습니다.\n재료 합성 또는 데이터 추출기를 이용하세요.";return change;}
        else{result=item;items.Add(item);change.message=Data.items[item].name+" 장착";}
        change.allowed=true;change.result=result;change.items=items.ToArray();return change;
    }
}
