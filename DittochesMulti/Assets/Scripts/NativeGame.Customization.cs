using UnityEngine;

public sealed partial class NativeGame
{
    TamerLoadout tamerLoadout;
    LoadoutStudio tamerStudio;
    float finisherStarted=-10;
    bool finisherVictory;
    TamerFinisherScene nativeFinisher;
    static readonly Vector3 RivalTamerPosition=new Vector3(0,.23f,3.8f);
    bool NativeFinisherActive {get{return nativeFinisher!=null&&nativeFinisher.Active(Time.unscaledTime);}}
    public void ApplyEquippedLoadout(){tamerLoadout=TamerLoadout.Load(legend);if(artPack==0)legend=tamerLoadout.tamer;}
    void DrawNativeLoadout()
    {
        if(tamerLoadout==null)ApplyEquippedLoadout();
        if(tamerStudio==null)tamerStudio=new LoadoutStudio();
        if(tamerStudio.Draw(new Rect(565,140,1308,820),ref tamerLoadout)){legend=tamerLoadout.tamer;Save();}
    }
    void StartNativeFinisher()
    {
        if(tamerLoadout==null)ApplyEquippedLoadout();
        int rival=System.Array.IndexOf(RivalNames,currentOpponent);
        nativeFinisher=null;finisherStarted=-10;
        if(rival<0)return; // Creep rounds have no opposing tamer.
        finisherVictory=win;finisherStarted=Time.unscaledTime;
        nativeFinisher=new TamerFinisherScene(finisherStarted,win?0:1,win?tamerLoadout.finisher:rival%3,rival,
            legend,rival%TamerLoadout.Tamers.Length,LegacyWorld(legendPos),RivalTamerPosition,"나의 테이머",currentOpponent);
    }
    void DrawNativeTamers(float celebration)
    {
        if(NativeFinisherActive){nativeFinisher.Draw(arena,Time.unscaledTime,this,"solo:rival");return;}
        arena.SetTamer(this,legend,LegacyWorld(legendPos),LegacyWorld(legendTarget),legendVelocity.magnitude/550f,Time.unscaledTime,celebration);
        int rival=trialsMode?-1:battling?System.Array.IndexOf(RivalNames,currentOpponent):RoundType()=="크립"||RoundType()=="초밥집"?-1:NextRivalIndex;
        if(rival>=0)arena.SetTamer("solo:rival",rival%TamerLoadout.Tamers.Length,RivalTamerPosition,LegacyWorld(legendPos),0,Time.unscaledTime);
    }
}
