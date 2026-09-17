using UnityEngine;

public sealed partial class NativeGame
{
    private readonly CombatSeparation separation=new CombatSeparation(64);
    private readonly CombatSeparation.Body[] separationBodies=new CombatSeparation.Body[64];
    private void SeparateCombatants(float dt)
    {
        for(int i=0;i<fighters.Count;i++)
        {
            Fighter fighter=fighters[i];
            separationBodies[i]=new CombatSeparation.Body(fighter.pos.x,fighter.pos.y,!fighter.dead,fighter.stun<=0);
        }
        separation.Step(separationBodies,fighters.Count,dt);
        for(int i=0;i<fighters.Count;i++)fighters[i].pos=new Vector2(separationBodies[i].x,separationBodies[i].y);
    }
    private void TickCombatPresentation(float dt)
    {
        for(int i=combatPopups.Count-1;i>=0;i--)
        {
            combatPopups[i].life-=dt;
            if(combatPopups[i].life<=0)combatPopups.RemoveAt(i);
        }
        foreach(Fighter f in fighters)
        {
            f.renderPos=Vector2.SmoothDamp(f.renderPos,f.pos,ref f.renderVelocity,.12f,7f,dt);
            f.hitFlash=Mathf.Max(0,f.hitFlash-dt);f.healFlash=Mathf.Max(0,f.healFlash-dt);
            f.shieldFlash=Mathf.Max(0,f.shieldFlash-dt);f.attackFlash=Mathf.Max(0,f.attackFlash-dt);
            f.skillFlash=Mathf.Max(0,f.skillFlash-dt);
        }
    }
}
