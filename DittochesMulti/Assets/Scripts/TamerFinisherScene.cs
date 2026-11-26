using UnityEngine;

// The presentation owns a snapshot of the completed duel. The next pairing and
// movement updates may follow the recipient, but never change its identity.
public sealed class TamerFinisherScene
{
    public readonly float Started;
    public readonly int Winner,Finisher,OpponentSide;
    public readonly int OwnTamer,RivalTamer;
    public readonly Vector3 OwnPosition,RivalPosition;
    public readonly string OwnName,RivalName;
    Vector3 liveTarget;
    bool following;
    public TamerFinisherScene(float started,int winner,int finisher,int opponentSide,
        int ownTamer,int rivalTamer,Vector3 ownPosition,Vector3 rivalPosition,string ownName,string rivalName)
    {
        Started=started;Winner=winner;Finisher=finisher;OpponentSide=opponentSide;
        OwnTamer=ownTamer;RivalTamer=rivalTamer;OwnPosition=ownPosition;RivalPosition=rivalPosition;
        OwnName=ownName;RivalName=rivalName;
    }
    public bool Active(float now){return now>=Started&&now-Started<TacticalArena.FinisherDuration;}
    public Vector3 Source {get{return Winner==0?OwnPosition:RivalPosition;}}
    public Vector3 Target {get{return Winner==0?RivalPosition:OwnPosition;}}
    public void Draw(TacticalArena arena,float now,object ownKey,object rivalKey)
    {
        if(!Active(now))return;
        arena.SetTamer(ownKey,OwnTamer,OwnPosition,RivalPosition,0,now,Winner==0?.8f:0);
        arena.SetTamer(rivalKey,RivalTamer,RivalPosition,OwnPosition,0,now,Winner==1?.8f:0);
        arena.FaceActor(ownKey,RivalPosition-OwnPosition);arena.FaceActor(rivalKey,OwnPosition-RivalPosition);
        arena.DrawFinisher(Finisher,Source,Target,now-Started);
    }
    public void DrawMovingEffect(TacticalArena arena,float now,Vector3 own,Vector3 rival)
    {
        if(!Active(now))return;
        following=true;liveTarget=Winner==0?rival:own;
        arena.DrawFinisher(Finisher,Winner==0?own:rival,liveTarget,now-Started,true);
    }
    public void Labels(TacticalArena arena,float now,Rect viewport)
    {
        if(!Active(now))return;
        Vector2 p=arena.Project(following?liveTarget:Target);
        string name=Winner==0?RivalName:OwnName;
        float width=Mathf.Clamp(44+name.Length*13,110,228);
        Rect label=new Rect(Mathf.Clamp(p.x-width*.5f,viewport.x+8,viewport.xMax-width-8),Mathf.Clamp(p.y+10,viewport.y+8,viewport.yMax-32),width,24);
        BattleHudTheme.Badge(label,name+(now-Started<TacticalArena.FinisherImpactTime(Finisher)?" · 대상":" · 피격"),TamerLoadout.FinishColor(Finisher),12);
    }
}
