using UnityEngine;

public sealed partial class MultiLauncher
{
    float CombatTime(Room room,float remaining)
    {return room.players!=null&&room.players.Length>2?Mathf.Clamp(room.battleElapsed+Time.unscaledTime-receivedAt,0,room.battleDuration):Mathf.Max(0,(room.battleDuration>0?room.battleDuration:8)-remaining);}
    float CombatProgress(Room room,float remaining)
    {
        if(room.frames==null||room.frames.Length<2)return 0;
        int last=room.frames.Length-1;
        // An older server has no timestamps or skill events.
        if(room.frames[last].time<=0)return Mathf.Clamp01(1-remaining/8f)*last;
        float time=CombatTime(room,remaining);
        // Upper bound preserves exact timestamp boundaries, including repeated times.
        // No cursor is retained: corrected snapshots and backwards playback work immediately.
        int low=1,high=last+1;
        while(low<high)
        {
            int middle=low+(high-low)/2;
            if(time<room.frames[middle].time)high=middle;else low=middle+1;
        }
        if(low>last)return last;
        int frame=low-1;
        return frame+Mathf.InverseLerp(room.frames[frame].time,room.frames[low].time,time);
    }
    static Fighter FindFighter(Frame frame,int key,bool requireAlive=false)
    {
        if(frame==null||frame.units==null)return null;
        var units=frame.units;
        for(int i=0;i<units.Length;i++)
        {
            var fighter=units[i];
            if(fighter!=null&&fighter.key==key&&(!requireAlive||fighter.hp>0))return fighter;
        }
        return null;
    }
    static int FighterDeathFrame(Frame[] frames,int frame,int key)
    {
        while(frame>0)
        {
            var prior=FindFighter(frames[frame-1],key);
            if(prior==null||prior.hp>0)break;
            frame--;
        }
        return frame;
    }
    float ActiveCastAge(Room room,Fighter fighter,float time)
    {return ActiveCastAge(room,fighter.key,time,fighter.revived?fighter.revivedAt:-1);}
    float ActiveCastAge(Room room,int key,float time,float after=-1)
    {
        if(room.skillEvents==null)return -1;
        foreach(SkillEvent cast in room.skillEvents)
        {
            if(cast.caster!=key||cast.started<after)continue;
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
            if(BattleSide(room)==1){sx=6-sx;sy=7-sy;tx=6-tx;ty=7-ty;}
            // Every combat-start fighter keeps its own species/star even if the roster is upgraded or sold later.
            var target=room.frames!=null&&room.frames.Length>0?FindFighter(room.frames[0],cast.target):null;
            string id=target==null?null:target.id;int star=target==null?1:target.star;
            float surface=arena.ContactSurface("fighter:"+cast.target,CombatContactPresentation.Height(id,star),CombatContactPresentation.Surface(id,star));
            arena.DrawSkill(s,TacticalArena.CellWorld(sx,sy),TacticalArena.CellWorld(tx,ty),age,cast.serial,id,star,surface);
        }
    }
}
