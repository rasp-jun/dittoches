#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed partial class TacticalArena
{
    public void PromoResolution()
    {target.Release();target.width=1920;target.height=1080;target.antiAliasing=2;target.Create();camera.aspect=16f/9;}
    public void PromoStudio()
    {
        UseModelStudio(new Vector3(0,-.12f,0));camera.backgroundColor=new Color(.016f,.028f,.046f);
        var floor=root.transform.Find("Model review floor");if(floor!=null)floor.GetComponent<Renderer>().sharedMaterial.color=new Color(.055f,.09f,.12f);
    }
}

public sealed partial class MultiLauncher
{
    bool promoCapture;
    public void BeginPromoCapture(){promoCapture=true;token="";StartCoroutine(CapturePromo());}
    void PromoPiece(string key,string id,Vector3 position,Vector3 facing,float time,float cast=-1,float speed=0)
    {
        arena.SetActor(key,position,FaithfulPortraits.Get(id),ArenaInterface.Rarity((int)DigimonVisualScale.Stage(id)));
        arena.FaceActor(key,facing);arena.PoseDigimon(key,id,speed,0,time,cast);
    }
    void PromoFrame(int shot,float local,Room replay)
    {
        arena.BeginFrame(-1,-1,-1,false);
        if(shot==0||shot==5)
        {
            string[] ids=shot==0?new[]{"koromon","agumon","greymon","metalgreymon","wargreymon"}:new[]{"metalgarurumon","rosemon","wargreymon","seraphimon","herakle"};
            for(int i=0;i<ids.Length;i++)PromoPiece("hero"+i,ids[i],new Vector3((i-2)*2.05f,0,0),Vector3.back,local);
            arena.SetView(new Vector3(.65f-local*.12f,3.2f,-13+local*.15f),new Vector3(0,.85f,0),32);
        }
        else if(shot>=1&&shot<=3)
        {
            string id=shot==1?"greymon":shot==2?"metalgreymon":"wargreymon";
            var skill=DigimonSkillCatalog.Find(id);Vector3 source=new Vector3(-1.35f,0,0),destination=new Vector3(2.15f,0,.15f);
            float age=local-.9f;float cast=age>=0&&age<skill.Duration?age:-1;
            PromoPiece("caster",id,source,destination-source,local,cast);
            PromoPiece("target","devimon",destination,source-destination,local);
            if(age>=skill.Impact&&age<skill.Impact+.22f)arena.DecorateActor("target",false,0,1);
            if(cast>=0)arena.DrawSkill(skill,source,destination,cast,100+shot);
            arena.SetView(new Vector3(-.35f+local*.12f,2.6f,-7.8f),new Vector3(.15f,shot==3?1.25f:.95f,0),39);
        }
        else
        {
            // Exact production server replay and the normal online presentation functions.
            float time=local+.7f,remaining=replay.battleDuration-time,progress=CombatProgress(replay,remaining);
            int frame=Mathf.FloorToInt(progress),next=Mathf.Min(frame+1,replay.frames.Length-1);
            foreach(Fighter f in replay.frames[frame].units)RenderOnlineFighter(replay,f,frame,next,progress,remaining);
            DrawMatchSkills(replay,remaining);
            arena.SetView(new Vector3(-.7f+local*.22f,10.2f,-10.8f),new Vector3(0,.1f,0),39);
        }
        arena.Render();
    }
    IEnumerator CapturePromo()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;yield return null;artPack=0;
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"..","PromoCapture"));Directory.CreateDirectory(folder);
        string frames=Path.Combine(folder,"frames");Directory.CreateDirectory(frames);
        var replay=JsonUtility.FromJson<Room>(File.ReadAllText(Path.Combine(folder,"fixture.json")));
        state=new State{room=replay};selectedSlot=-1;
        bool storyboard=PortablePreview.HasArgument("--promo-storyboard");
        int shot=-1,count=0;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        for(int frame=0;frame<720;frame++)
        {
            float time=frame/30f;int nextShot=time<4?0:time<8?1:time<12?2:time<16?3:time<21?4:5;
            float local=time-(nextShot<4?nextShot*4:nextShot==4?16:21);
            if(nextShot!=shot)
            {
                if(arena!=null)arena.Dispose();arena=new TacticalArena(new Rect(0,0,1920,1080),true);arena.PromoResolution();
                if(nextShot!=4)arena.PromoStudio();shot=nextShot;
                yield return null;
            }
            PromoFrame(shot,local,replay);
            if(!storyboard||frame%30==0)
            {
                var old=RenderTexture.active;RenderTexture.active=arena.Texture;
                image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();RenderTexture.active=old;
                File.WriteAllBytes(Path.Combine(frames,frame.ToString("D5")+".jpg"),image.EncodeToJPG(94));count++;
            }
            if(frame%90==0)Debug.Log("PROMO FRAME "+frame+" / 720");
            yield return null;
        }
        File.WriteAllText(Path.Combine(folder,storyboard?"storyboard-report.json":"capture-report.json"),"{\"passed\":true,\"frames\":"+count+",\"fps\":30,\"width\":1920,\"height\":1080,\"seconds\":24}");
        Destroy(image);arena.Dispose();arena=null;Debug.Log("PROMO CAPTURE COMPLETE "+count);Application.Quit();
    }
}
#endif
