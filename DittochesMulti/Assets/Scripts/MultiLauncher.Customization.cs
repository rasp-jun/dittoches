using UnityEngine;

public sealed partial class MultiLauncher
{
    TamerLoadout lobbyLoadout;
    LoadoutStudio loadoutStudio;
    string cosmeticRoom="";
    int cosmeticReport,cosmeticWinner=-1,cosmeticFinisher;
    float cosmeticEffectStarted=-10;
    readonly Vector3[] tamerPositions={new Vector3(0,.23f,-3.8f),new Vector3(0,.23f,3.8f)};
    readonly Vector3[] tamerVelocities=new Vector3[2];
    void DrawLoadoutLobby()
    {
        if(loadoutStudio==null)loadoutStudio=new LoadoutStudio();
        loadoutStudio.Draw(new Rect(55,110,1490,790),ref lobbyLoadout,!busy&&(state==null||string.IsNullOrEmpty(state.queue)));
    }
    void DrawMatchTamers(Room room,Player me,Player enemy)
    {
        arena.SetField(me.field);
        if(cosmeticRoom!=room.id)
        {
            cosmeticRoom=room.id;cosmeticReport=room.reportRound;cosmeticEffectStarted=-10;
            tamerPositions[0]=TacticalArena.CellWorld(me.tamerX,me.tamerY);
            tamerPositions[1]=TacticalArena.CellWorld(6-enemy.tamerX,7-enemy.tamerY);
            tamerVelocities[0]=tamerVelocities[1]=Vector3.zero;
        }
        if(room.reportRound>cosmeticReport)
        {
            cosmeticReport=room.reportRound;
            if(room.phase!="battle"&&room.roundWinner>=0){cosmeticWinner=room.roundWinner==room.side?0:1;cosmeticFinisher=cosmeticWinner==0?me.finisher:enemy.finisher;cosmeticEffectStarted=Time.unscaledTime;}
        }
        for(int i=0;i<2;i++)
        {
            var player=i==0?me:enemy;Vector3 target=TacticalArena.CellWorld(i==0?player.tamerX:6-player.tamerX,i==0?player.tamerY:7-player.tamerY);
            tamerPositions[i]=Vector3.SmoothDamp(tamerPositions[i],target,ref tamerVelocities[i],.22f,4,Time.unscaledDeltaTime);
            arena.SetTamer("tamer:"+i,player.tamer,tamerPositions[i],target,Mathf.Clamp01(tamerVelocities[i].magnitude),Time.unscaledTime,i==cosmeticWinner&&Time.unscaledTime-cosmeticEffectStarted<2.2f?.8f:0);
        }
        if(cosmeticWinner>=0)arena.DrawFinisher(cosmeticFinisher,tamerPositions[cosmeticWinner],tamerPositions[1-cosmeticWinner],Time.unscaledTime-cosmeticEffectStarted);
    }
    void MoveOnlineTamer(int cell)
    {
        if(cell<28||cell>=56||busy)return;
        Send("/tamer-move",new Command{x=cell%7,y=cell/7});
    }
}
