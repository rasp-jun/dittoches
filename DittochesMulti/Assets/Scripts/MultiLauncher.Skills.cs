using UnityEngine;

public sealed partial class MultiLauncher
{
    float CombatTime(Room room,float remaining)
    {return Mathf.Max(0,(room.battleDuration>0?room.battleDuration:8)-remaining);}
    float CombatProgress(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length<2)return 0;
        int last=room.frames.Length-1;
        // An older server has no timestamps or skill events.
        if(room.frames[last].time<=0)return Mathf.Clamp01(1-remaining/8f)*last;
        float time=CombatTime(room,remaining);
        for(int i=0;i<last;i++)if(time<room.frames[i+1].time)
            return i+Mathf.InverseLerp(room.frames[i].time,room.frames[i+1].time,time);
        return last;
    }
    float ActiveCastAge(Room room,int key,float time)
    {
        if(room.skillEvents==null)return -1;
        foreach(SkillEvent cast in room.skillEvents)
        {
            if(cast.caster!=key)continue;
            var s=DigimonSkillCatalog.Find(cast.id);float age=time-cast.started;
            if(s!=null&&age>=0&&age<s.Duration)return age;
        }
        return -1;
    }
    void DrawMatchSkills(Room room,float remaining)
    {
        if(room.skillEvents==null)return;
        float time=CombatTime(room,remaining);
        foreach(SkillEvent cast in room.skillEvents)
        {
            var s=DigimonSkillCatalog.Find(cast.id);float age=time-cast.started;
            if(s==null||age<0||age>=s.Duration)continue;
            float sx=cast.sx,sy=cast.sy,tx=cast.tx,ty=cast.ty;
            if(room.side==1){sx=6-sx;sy=7-sy;tx=6-tx;ty=7-ty;}
            arena.DrawSkill(s,TacticalArena.CellWorld(sx,sy),TacticalArena.CellWorld(tx,ty),age);
        }
    }
}
