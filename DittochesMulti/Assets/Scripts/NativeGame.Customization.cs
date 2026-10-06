using UnityEngine;

public sealed partial class NativeGame
{
    TamerLoadout tamerLoadout;
    LoadoutStudio tamerStudio;
    float finisherStarted=-10;
    bool finisherVictory;
    public void ApplyEquippedLoadout(){tamerLoadout=TamerLoadout.Load(legend);if(artPack==0)legend=tamerLoadout.tamer;}
    void DrawNativeLoadout()
    {
        if(tamerLoadout==null)ApplyEquippedLoadout();
        if(tamerStudio==null)tamerStudio=new LoadoutStudio();
        if(tamerStudio.Draw(new Rect(565,140,1308,820),ref tamerLoadout)){legend=tamerLoadout.tamer;Save();}
    }
    void DrawNativeFinisher()
    {
        if(tamerLoadout==null)ApplyEquippedLoadout();
        arena.DrawFinisher(finisherVictory?tamerLoadout.finisher:0,
            finisherVictory?LegacyWorld(legendPos):new Vector3(0,.23f,3.8f),
            finisherVictory?new Vector3(0,.23f,3.8f):LegacyWorld(legendPos),Time.unscaledTime-finisherStarted);
    }
}
