using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One source of truth for solo, server simulation and arena presentation.</summary>
public static class DigimonSkillCatalog
{
    [Serializable] public sealed class Entry
    {
        public string id, name, technique, description, visual, motion, shape, color, source, anime;
        public float windup, travel, recovery, interval, radius, reach, multiplier, stun, size, hover;
        public int shots, targets;
        public float Impact { get { return windup+travel; } }
        public float Duration { get { return Impact+(shots-1)*interval+recovery; } }
        public Color Tint { get { Color c;return ColorUtility.TryParseHtmlString(color,out c)?c:Color.white; } }
    }
    [Serializable] sealed class Document { public Entry[] skills=new Entry[0]; }
    static Dictionary<string,Entry> entries;
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
