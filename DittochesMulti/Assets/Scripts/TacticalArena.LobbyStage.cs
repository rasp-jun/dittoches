using UnityEngine;

public sealed partial class TacticalArena
{
    GameObject lobbyStageRoot;
    Material lobbyMetal,lobbyStone,lobbyLight;
    readonly Transform[] lobbyCrystals=new Transform[12];
    readonly MeshRenderer[] lobbySeats=new MeshRenderer[8];
    public void DrawSeasonLobbyPortrait()
    {
        foreach(var surface in fieldSurfaces)surface.renderer.enabled=false;
        for(int i=0;i<fieldDecor.Length;i++)if(fieldDecor[i]!=null)fieldDecor[i].SetActive(false);
        if(lobbyStageRoot!=null)lobbyStageRoot.SetActive(false);
        if(atmosphere!=null)atmosphere.enabled=false;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
        camera.transform.localPosition=new Vector3(0,2.4f,-5.8f);
        camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.78f,0)));camera.fieldOfView=34;
    }
    public void SetSeasonLobbyTamer(int choice,float time,bool walking=false)
    {
        SetLobbyTamer(choice,time,walking);
        var actor=actors["lobby"];
        actor.halo.enabled=false;actor.contactShadow.enabled=false;actor.destination.enabled=false;
    }
    public void DrawLobbyStage(int theme,int participants,float time)
    {
        if(lobbyStageRoot==null)
        {
            foreach(var surface in fieldSurfaces)surface.renderer.enabled=false;
            for(int i=0;i<fieldDecor.Length;i++)if(fieldDecor[i]!=null)fieldDecor[i].SetActive(false);
            lobbyStageRoot=new GameObject("Celestial lobby pavilion");lobbyStageRoot.transform.SetParent(root.transform,false);
            var p=lobbyStageRoot.transform;
            lobbyStone=Material(new Color(.08f,.13f,.17f));lobbyMetal=Material(new Color(.65f,.51f,.27f));lobbyLight=Material(new Color(.31f,.76f,.85f),true);
            Shape("Floating dais",disc,lobbyStone,new Vector3(0,-.30f,0),new Vector3(3.5f,.42f,3.5f),p);
            Shape("Dais rim",ringMesh,lobbyMetal,new Vector3(0,.13f,0),Vector3.one*1.72f,p);
            Shape("Inner inscription",ringMesh,lobbyLight,new Vector3(0,.16f,0),Vector3.one*1.40f,p);
            Shape("Tamer pedestal",disc,lobbyStone,new Vector3(0,.14f,0),new Vector3(1.75f,.12f,1.75f),p);
            for(int i=0;i<8;i++)
            {
                float a=(i*45+22.5f)*Mathf.Deg2Rad;Vector3 point=new Vector3(Mathf.Sin(a)*2.9f,-.13f,Mathf.Cos(a)*2.5f);
                Shape("Satellite pedestal",hex,lobbyStone,point,new Vector3(.45f,.15f,.45f),p);
                lobbySeats[i]=Shape("Tamer constellation "+i,ringMesh,lobbyLight,point+Vector3.up*.16f,Vector3.one*.33f,p);
            }
            for(int side=-1;side<=1;side+=2)
            {
                Shape("Pavilion column",cube,lobbyStone,new Vector3(side*4,1.3f,3.8f),new Vector3(.25f,3.6f,.35f),p);
                Shape("Column inlay",cube,lobbyMetal,new Vector3(side*4,1.3f,3.61f),new Vector3(.025f,3.4f,.015f),p);
                Shape("Column crown",hex,lobbyMetal,new Vector3(side*4,3.15f,3.8f),new Vector3(.40f,.11f,.4f),p);
                for(int i=0;i<4;i++)
                {
                    var slab=Shape("Distant floating isle",hex,lobbyStone,new Vector3(side*(5+i*1.7f),-.8f-i*.3f,5+i*1.3f),new Vector3(1.4f,.5f,1.1f),p);
                    slab.transform.localRotation=Quaternion.Euler(8,i*18,side*6);
                }
            }
            for(int i=0;i<12;i++)lobbyCrystals[i]=Shape("Orbit crystal",hex,lobbyLight,Vector3.zero,new Vector3(.026f,.09f,.026f),p).transform;
        }
        SetAtmosphere(theme);
        Color accent=TamerLoadout.FieldColor(theme);
        lobbyStone.color=Color.Lerp(new Color(.036f,.055f,.085f),accent,.11f);lobbyLight.color=accent;
        camera.backgroundColor=Color.Lerp(new Color(.008f,.018f,.035f),accent,.025f);
        camera.transform.localPosition=new Vector3(0,2.75f,-6.7f);camera.transform.LookAt(root.transform.TransformPoint(new Vector3(0,.8f,.3f)));camera.fieldOfView=38;
        for(int i=0;i<8;i++)Tint(lobbySeats[i],i<participants?accent:new Color(.14f,.20f,.24f));
        for(int i=0;i<12;i++)
        {
            float a=i*Mathf.PI/6+time*.06f;var crystal=lobbyCrystals[i];
            crystal.localPosition=new Vector3(Mathf.Sin(a)*(2+i%3*.35f),.42f+Mathf.Sin(time*.8f+i)*.18f,Mathf.Cos(a)*1.9f);
            crystal.localRotation=Quaternion.Euler(15,time*8+i*30,25);
        }
    }
    public void SetLobbyTamer(int choice,float time,bool walking=false)
    {
        Vector3 point=new Vector3(walking?Mathf.Sin(time)*.7f:0,.28f,0);
        Vector3 direction=walking?Vector3.right*Mathf.Cos(time):Vector3.zero;
        SetTamer("lobby",choice,point,point+direction,walking?Mathf.Abs(Mathf.Cos(time)):0,time,walking?0:.12f);
        var actor=actors["lobby"];actor.root.transform.localScale=Vector3.one*(choice==0?1.45f:2.2f);
    }
#if DITTOCHES_PORTABLE_PREVIEW
    public void ValidateLobbyStage(System.Action<bool,string> require)
    {
        for(int choice=0;choice<TamerLoadout.Tamers.Length;choice++)
        {
            BeginFrame(-1,-1,-1,false);DrawLobbyStage(choice%3,1,0);SetLobbyTamer(choice,0);
            Vector3 idleScale=actors["lobby"].root.transform.localScale;
            foreach(float time in new[]{.4f,1.9f,3.7f,5.2f})
            {
                BeginFrame(-1,-1,-1,false);DrawLobbyStage(choice%3,8,time);SetLobbyTamer(choice,time,true);
                var actor=actors["lobby"];
                require((actor.root.transform.localScale-idleScale).sqrMagnitude<.0001f,"lobby walking preview keeps the equipped tamer presentation scale: "+choice);
                require(Mathf.Abs(actor.root.transform.localPosition.x)<1,"lobby walking stays on the central pedestal: "+choice);
            }
            SetLobbyTamer(choice,6);require(actors["lobby"].root.transform.localPosition.x==0,"stopping the lobby preview returns the tamer to center");
        }
    }
#endif
}
