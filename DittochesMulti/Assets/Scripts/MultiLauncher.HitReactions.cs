using System.Collections.Generic;
using UnityEngine;

public sealed partial class MultiLauncher
{
    Frame[] hitReactionFrames;
    SkillEvent[] hitReactionSkills;
    readonly Dictionary<int,CombatHitTrack> hitReactionTracks=new Dictionary<int,CombatHitTrack>();
    CombatHitTrack HitTrack(int key)
    {
        CombatHitTrack track;
        if(!hitReactionTracks.TryGetValue(key,out track)){track=new CombatHitTrack();hitReactionTracks.Add(key,track);}
        return track;
    }
    // Snapshots aggregate damage. When a skill can coincide with a basic, keep the
    // authoritative flash as well; do not attribute an uncertain skill/DoT to a projectile.
    static bool PossibleSkillHit(Room room,float stamp,int victim,out Vector3 direction)
    {
        direction=Vector3.zero;bool possible=false;
        if(room.skillEvents==null)return false;
        foreach(var cast in room.skillEvents)
        {
            var skill=DigimonSkillCatalog.Find(cast.id);if(skill==null)continue;
            for(int shot=0;shot<skill.shots;shot++)
            {
                float due=cast.started+skill.Impact+shot*skill.interval;
                if(stamp+.0001f<due||stamp-due>.051f)continue;
                possible=true;
                if(cast.target==victim)direction+=TacticalArena.CellWorld(cast.tx,cast.ty)-TacticalArena.CellWorld(cast.sx,cast.sy);
            }
        }
        return possible;
    }
    void EnsureHitReactions(Room room)
    {
        if(ReferenceEquals(hitReactionFrames,room.frames)&&ReferenceEquals(hitReactionSkills,room.skillEvents))return;
        hitReactionFrames=room.frames;hitReactionSkills=room.skillEvents;hitReactionTracks.Clear();
        if(room.frames==null)return;
        var attacks=new Dictionary<int,int>();var damage=new Dictionary<int,float>();
        foreach(var frame in room.frames)
        {
            if(frame==null||frame.units==null)continue;
            foreach(var source in frame.units)
            {
                int previous;attacks.TryGetValue(source.key,out previous);
                if(source.attacks<=previous||source.attackAt<0)continue;
                attacks[source.key]=source.attacks;
                var target=FindFighter(frame,source.target);
                if(target==null)continue;
                HitTrack(target.key).Add(source.attackAt+BasicAttackPresentation.Travel(source.id),
                    TacticalArena.CellWorld(target.x,target.y)-TacticalArena.CellWorld(source.x,source.y),frame.time,source.attackAt);
            }
            foreach(var victim in frame.units)
            {
                float previous;
                if(victim.hitAt<0||damage.TryGetValue(victim.key,out previous)&&victim.hitAt<=previous)continue;
                damage[victim.key]=victim.hitAt;
                bool basic=false;
                foreach(var source in frame.units)
                    if(source.attacks>0&&source.target==victim.key&&Mathf.Abs(source.attackAt-victim.hitAt)<.0001f){basic=true;break;}
                Vector3 direction;bool skill=PossibleSkillHit(room,victim.hitAt,victim.key,out direction);
                if(!basic||skill)HitTrack(victim.key).Add(victim.hitAt,direction,frame.time);
            }
        }
    }
    CombatHitPresentation OnlineHitReaction(Room room,Fighter fighter,float playhead)
    {
        EnsureHitReactions(room);
        CombatHitTrack track;
        if(fighter.hp<=0||!hitReactionTracks.TryGetValue(fighter.key,out track))return CombatHitPresentation.Empty;
        var hit=track.At(playhead,fighter.revived?fighter.revivedAt:-1);
        // These histories are shared between perspectives; mirror only the sampled vector.
        if(BattleSide(room)==1)hit.direction=-hit.direction;
        return hit;
    }
}
