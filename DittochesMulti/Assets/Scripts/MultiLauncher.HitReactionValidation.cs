#if DITTOCHES_PORTABLE_PREVIEW
using UnityEngine;

public sealed partial class MultiLauncher
{
    void ValidateOnlineHitReactions()
    {
        int before=onlineChecks;var oldArena=arena;
        using(var review=new TacticalArena(new Rect(0,0,640,360)))
        {
            try
            {
                arena=review;
                foreach(string id in DigimonModelLibrary.Roster)foreach(int side in new[]{0,1})
                {
                    var waiting=new Fighter{key=1,id=id,hp=100,x=1,y=3,hitAt=-10,attackAt=-10,target=2};
                    var source=waiting.Snapshot();source.attacks=1;source.attackAt=.1f;
                    var target=new Fighter{key=2,id="greymon",star=1,side=1,hp=500,x=4,y=3,attackAt=-10,hitAt=.1f,target=1};
                    var initial=target.Snapshot();initial.hitAt=-10;
                    var retargeted=source.Snapshot();retargeted.target=3;retargeted.x=6;
                    var room=new Room{id="hit-test",side=side,round=1,battleDuration=3,frames=new[]{new Frame{time=0,units=new[]{waiting,initial}},new Frame{time=.2f,units=new[]{source,target}},new Frame{time=.4f,units=new[]{retargeted,target}}}};
                    string saved=JsonUtility.ToJson(room);float impact=.1f+BasicAttackPresentation.Travel(id),shown=Mathf.Max(.2f,impact)+.04f;
                    OnlineRequire(OnlineHitReaction(room,target,Mathf.Max(.2f,impact)-.005f).time<0,"replay waits for impact and its snapshot "+id+"/"+side);
                    var hit=OnlineHitReaction(room,target,shown);
                    OnlineRequire(Mathf.Abs(hit.time-impact)<.0001f&&hit.direction.x*(side==0?1:-1)>.99f,"replay arrival and incoming direction mirror together "+id);
                    OnlineRequire(hitReactionTracks[2].Count==1,"repeated snapshots and retargeting cannot duplicate a hit "+id);
                    review.BeginFrame(-1,-1,-1,false);RenderOnlineFighter(room,target,1,2,1,3-shown);
                    review.RequireHitReaction("fighter:2",impact,hit.direction,hit.Flash(shown),OnlineRequire);
                    OnlineRequire(OnlineHitReaction(room,target,1).time<0&&OnlineHitReaction(room,target,shown).time==hit.time,"old hit does not restart after seek and rewind "+id);
                    OnlineRequire(JsonUtility.ToJson(room)==saved,"hit presentation does not mutate authoritative snapshots "+id);
                    target.hp=0;OnlineRequire(OnlineHitReaction(room,target,shown).time<0,"online death overrides pending reactions "+id);target.hp=500;
                    target.revived=true;target.revivedAt=.15f;OnlineRequire(OnlineHitReaction(room,target,shown).time<0,"pre-revival attack cannot flinch the new life "+id);target.revived=false;
                }
                // A direct status hit and an older projectile must both survive in arrival order.
                var s=new Fighter{key=1,id="metalgreymon",hp=100,x=1,y=3,attacks=1,attackAt=.1f,hitAt=-10,target=2};
                var t=new Fighter{key=2,id="greymon",hp=500,x=4,y=3,hitAt=.15f,attackAt=-10};
                var r=new Room{frames=new[]{new Frame{time=.2f,units=new[]{s,t}}}};
                OnlineRequire(OnlineHitReaction(r,t,.21f).time==.15f&&Mathf.Abs(OnlineHitReaction(r,t,.36f).time-.32f)<.0001f,"a later raw damage stamp cannot hide an earlier projectile arrival");
                var skill=DigimonSkillCatalog.Find("agumon");float stamp=1;
                s.attackAt=stamp;t.hitAt=stamp;r.frames=new[]{new Frame{time=stamp,units=new[]{s,t}}};r.skillEvents=new[]{new SkillEvent{id="agumon",target=2,started=stamp-skill.Impact,sx=6,sy=3,tx=4,ty=3}};
                var simultaneous=OnlineHitReaction(r,t,stamp+.04f);
                OnlineRequire(simultaneous.time==stamp&&simultaneous.direction.x<-.99f,"coincident skill keeps authoritative hit and known skill direction");
                OnlineRequire(Mathf.Abs(OnlineHitReaction(r,t,stamp+.26f).time-(stamp+.22f))<.0001f,"coincident basic still reacts on arrival");
                r.frames=new[]{new Frame{time=0,units=new Fighter[0]}};r.skillEvents=null;
                OnlineRequire(OnlineHitReaction(r,t,stamp+.04f).time<0&&hitReactionTracks.Count==0,"replaced replay clears old cached hits");
                r.frames=null;OnlineRequire(OnlineHitReaction(r,t,1).time<0,"missing replay is safe");
            }
            finally{arena=oldArena;hitReactionFrames=null;hitReactionSkills=null;hitReactionTracks.Clear();}
        }
        Debug.Log("HIT REACTIONS ONLINE COMPLETE: "+(onlineChecks-before)+" checks / 47 species, both perspectives, actual renderer, seeks, skills, snapshot replacement and revival");
    }
}
#endif
