using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One source of truth for solo, server simulation and arena presentation.</summary>
public static class DigimonSkillCatalog
{
    public sealed class Stats
    {
        public float attack,abilityPower,health,armor,magicResist,speed,startMana,maxMana;
        public int range;
        public Stats(Entry skill,int star,DigimonBuildCatalog.Bonus b,float scale=1)
        {
            attack=skill.Attack(star,b.attack)*scale;abilityPower=DigimonCombatMath.AbilityPower(b.abilityPower)*scale;
            health=(skill.baseHealth+b.health)*Mathf.Pow(1.8f,DigimonCombatMath.StarIndex(star))*(1+b.hp)*scale;
            armor=b.armor;magicResist=b.magicResist;speed=skill.attackSpeed*(1+b.speed);range=skill.attackRange;
            maxMana=skill.maxMana;startMana=Mathf.Min(maxMana,skill.startMana+b.startMana);
        }
    }
    [Serializable] public sealed class Entry
    {
        public string id, name, technique, description, visual, motion, shape, color, source, anime,damageType,attackStyle;
        public float windup, travel, recovery, interval, radius, reach, stun, size, hover;
        public float baseHealth,baseAttack,attackSpeed,startMana,maxMana;
        public float[] adRatio,apRatio;
        public int shots, targets,attackRange;
        public float Attack(int star,float bonus){return DigimonCombatMath.Attack(baseAttack,star,bonus);}
        public float Damage(int star,float attack,float ap){int i=DigimonCombatMath.StarIndex(star);return DigimonCombatMath.Skill(attack,ap,adRatio[i],apRatio[i]);}
        public string ScalingRole { get {return adRatio[0]>0?(apRatio[0]>0?"혼합 딜러":"공격 딜러"):"마법 딜러";} }
        public string DamageLabel { get {return damageType=="physical"?"물리":"마법";} }
        public float Impact { get { return windup+travel; } }
        public float Duration { get { return Impact+(shots-1)*interval+recovery; } }
        public Color Tint { get { Color c;return ColorUtility.TryParseHtmlString(color,out c)?c:Color.white; } }
    }
    [Serializable] sealed class Document { public Entry[] skills=new Entry[0]; }
    static Dictionary<string,Entry> entries;
    public static IEnumerable<Entry> All { get {Find("koromon");return entries.Values;} }
    public static Entry Find(string id)
    {
        if(entries==null)
        {
            TextAsset asset=Resources.Load<TextAsset>("DigimonSkills");
#if DITTOCHES_PORTABLE_PREVIEW
            string json=asset!=null?asset.text:PortablePreview.SkillJson();
#else
            if(asset==null)throw new InvalidOperationException("Missing Resources/DigimonSkills.json");
            string json=asset.text;
#endif
            entries=new Dictionary<string,Entry>();
            foreach(Entry entry in JsonUtility.FromJson<Document>(json).skills)entries.Add(entry.id,entry);
        }
        Entry value;return id!=null&&entries.TryGetValue(id,out value)?value:null;
    }
}
