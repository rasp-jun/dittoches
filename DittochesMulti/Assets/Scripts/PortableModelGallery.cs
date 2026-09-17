#if DITTOCHES_PORTABLE_PREVIEW
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Actual runtime model inspection; no screenshot mockups or imported sprite stand-ins.</summary>
public sealed class PortableModelGallery : MonoBehaviour
{
    TacticalArena arena;
    string mode="Idle";
    float clock,yaw=155;
    bool paused,capturing;
    GUIStyle heading,small;
    readonly Vector3 origin=TacticalArena.CellWorld(3,5);
    void Awake()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        DigimonModelLibrary.PreviewEnabled=true;
        arena=new TacticalArena(new Rect(0,0,1280,780));
        arena.SetView(origin+new Vector3(2.65f,2.35f,-3.8f),origin+Vector3.up*.70f,32);
        capturing=PortablePreview.HasArgument("--capture-model");
        if(capturing)StartCoroutine(Capture());
    }
    void Update(){if(!paused&&!capturing)clock+=Time.deltaTime;}
    void Draw()
    {
        arena.BeginFrame(-1,-1,-1,false);
        arena.SetActor("agumon",origin,null,new Color(.25f,.78f,.70f));
        arena.FaceActor("agumon",Quaternion.Euler(0,yaw,0)*Vector3.forward);
        var skill=DigimonSkillCatalog.Find("agumon");
        float cast=mode=="Pepper Breath"?clock%(skill.Duration+1):-1;
        arena.PoseDigimon("agumon","agumon",mode=="Walk"?1:0,0,clock,cast,
            mode=="Attack"?Mathf.Max(0,.35f-clock%1.2f):0,mode=="Defeat"?Mathf.Clamp01(clock):0);
        if(cast>=0&&cast<skill.Duration)
            arena.DrawSkill(skill,origin,origin+(Quaternion.Euler(0,yaw,0)*Vector3.forward)*2,cast);
        arena.Render();
    }
    void OnGUI()
    {
        if(arena==null)return;
        if(heading==null)
        {heading=new GUIStyle(GUI.skin.label){fontSize=24};small=new GUIStyle(GUI.skin.label){fontSize=16};}
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/900f);
        Matrix4x4 before=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        if(Event.current.type==EventType.Repaint)Draw();
        GUI.DrawTexture(new Rect(0,0,1280,780),arena.Texture,ScaleMode.StretchToFill,false);
        GUI.Label(new Rect(28,18,960,38),"AGUMON / 직접 제작 3D 모델 · 관절 애니메이션 시험",heading);
        string[] modes={"Idle","Walk","Attack","Pepper Breath","Defeat"};
        for(int i=0;i<modes.Length;i++)if(GUI.Button(new Rect(28+i*160,800,150,40),modes[i])){mode=modes[i];clock=0;}
        if(GUI.Button(new Rect(838,800,120,40),paused?"재생":"일시 정지"))paused=!paused;
        yaw=GUI.HorizontalSlider(new Rect(995,815,245,24),yaw,0,360);
        GUI.Label(new Rect(28,853,1210,26),"첫 제작본 / "+mode+" / 드래그로 회전 / 방향 전환과 발 디딤 확인",small);
        GUI.matrix=before;
    }
    IEnumerator Capture()
    {
        yield return null;
        string directory=Path.Combine(Application.dataPath,"../ModelCaptures");Directory.CreateDirectory(directory);
        string[] modes={"Idle","Walk","Walk","Attack","Pepper Breath","Pepper Breath","Idle","Idle"};
        float[] times={.3f,.4f,.8f,.17f,.4f,.7f,.3f,.3f};
        for(int i=0;i<modes.Length;i++)
        {
            mode=modes[i];clock=0;yaw=i==6?30:i==7?270:155;
            // Advance at an actual frame cadence so blended locomotion reaches its sample pose.
            for(int frame=0;frame<60;frame++){clock+=times[i]/60;Draw();}
            yield return new WaitForEndOfFrame();
            RenderTexture before=RenderTexture.active;
            var image=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=arena.Texture;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
                File.WriteAllBytes(Path.Combine(directory,"agumon-"+i+".png"),image.EncodeToPNG());
            }
            finally{RenderTexture.active=before;Destroy(image);}
            Debug.Log("MODEL CAPTURE "+i+" "+mode);
        }
        Debug.Log("MODEL CAPTURES COMPLETE");Application.Quit();
    }
    void OnDestroy(){if(arena!=null)arena.Dispose();DigimonModelLibrary.PreviewEnabled=false;}
}
#endif
