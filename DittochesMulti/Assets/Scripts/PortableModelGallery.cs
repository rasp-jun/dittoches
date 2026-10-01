using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Inspect the same authored rig in the preserved player and a regular Unity build.</summary>
public sealed class PortableModelGallery : MonoBehaviour
{
    static readonly string[] Clips={"Idle","Walk","Run","Attack","PepperBreath","Hit","Defeat","Turn"};
    static readonly string[] Labels={"대기","걷기","달리기","공격","불 뿜기","피격","쓰러짐","방향 살피기","연결 동작"};
    static readonly float[] Rates={.25f,.5f,1};
    const float FrameStep=1f/30,SequenceDuration=7.24f;
    TacticalArena arena;
    int selected,rateIndex=2,modelIndex;
    bool showRoster;
    bool frameModel;
    string ModelId { get { return DigimonModelLibrary.Roster[modelIndex]; } }
    float clock,yaw=155;
    bool paused,capturing,repeat=true,resetPose=true;
    GUIStyle heading,small,centered;
    string phaseLabel="대기";
    readonly Vector3 origin=TacticalArena.CellWorld(3,5);
    static bool HasArgument(string value){return Array.IndexOf(Environment.GetCommandLineArgs(),value)>=0;}

    public static bool TryBoot()
    {
        if(!HasArgument("--model-gallery")&&!HasArgument("--model-smoke")&&!HasArgument("--roster-smoke"))return false;
        var display=new GameObject("Model gallery display");UnityEngine.Object.DontDestroyOnLoad(display);
        var camera=display.AddComponent<Camera>();camera.cullingMask=0;camera.depth=-100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.012f,.022f,.04f);
        display.AddComponent<PortableModelGallery>();return true;
    }
    void Awake()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        DigimonModelLibrary.PreviewEnabled=true;
        showRoster=HasArgument("--show-roster");
        arena=new TacticalArena(new Rect(0,0,1280,680));
        arena.UseModelStudio(origin);frameModel=true;
        arena.SetView(origin+new Vector3(2.65f,1.82f,-3.52f),origin+Vector3.up*.70f,30);
        capturing=HasArgument("--capture-model")||HasArgument("--model-smoke")||HasArgument("--roster-smoke");
        if(capturing)StartCoroutine(HasArgument("--roster-smoke")?CaptureRoster():Capture());
    }
    float Duration
    {
        get
        {
            if(selected==Clips.Length)return SequenceDuration;
            var clip=arena.ModelClip("agumon",Clips[selected]);return clip==null?1:clip.duration;
        }
    }
    void Select(int value)
    {selected=value;clock=0;resetPose=true;repeat=value!=6;}
    void SelectModel(int value)
    {
        modelIndex=(value+DigimonModelLibrary.Roster.Length)%DigimonModelLibrary.Roster.Length;
        // One arena per selection avoids retaining 34 meshes in an interactive session.
        arena.Dispose();arena=new TacticalArena(new Rect(0,0,1280,680));
        arena.UseModelStudio(origin);
        arena.SetView(origin+new Vector3(3.65f,2.32f,-4.82f),origin+Vector3.up*.80f,34);
        selected=0;clock=0;resetPose=true;showRoster=false;frameModel=true;
    }
    void Seek(float value)
    {clock=Mathf.Clamp(value,0,Duration);paused=true;resetPose=true;}
    void Advance(float delta)
    {
        clock+=delta;
        if(clock<=Duration)return;
        if(repeat){clock=Mathf.Repeat(clock,Duration);resetPose=true;}
        else {clock=Duration;paused=true;}
    }
    void Update()
    {
        if(capturing)return;
        if(Input.GetKeyDown(KeyCode.PageDown))SelectModel(modelIndex+1);
        if(Input.GetKeyDown(KeyCode.PageUp))SelectModel(modelIndex-1);
        if(Input.GetKeyDown(KeyCode.Space))paused=!paused;
        if(Input.GetKeyDown(KeyCode.LeftArrow))Seek(clock-FrameStep);
        if(Input.GetKeyDown(KeyCode.RightArrow))Seek(clock+FrameStep);
        if(Input.GetKeyDown(KeyCode.Home))Seek(0);
        if(!paused)Advance(Time.unscaledDeltaTime*Rates[rateIndex]);
    }
    // Uses the actual combat dispatcher, including its transitions and state priorities.
    void Sequence(out Vector3 position,out float speed,out float cast,out float attack,out float death)
    {
        position=origin;speed=0;cast=-1;attack=0;death=0;
        Vector3 forward=Quaternion.Euler(0,yaw,0)*Vector3.forward;
        if(clock<1)phaseLabel="대기";
        else if(clock<2.8f)
        {phaseLabel="걷기";speed=.5f;position+=forward*(clock-1)*.25f;}
        else
        {
            position+=forward*.45f;
            if(clock<3.3f)phaseLabel="멈추기";
            else if(clock<3.65f){phaseLabel="공격";attack=.35f-(clock-3.3f);}
            else if(clock<4.3f)phaseLabel="공격 후 대기";
            else if(clock<5.24f){phaseLabel=DigimonSkillCatalog.Find(ModelId).name;cast=clock-4.3f;}
            else if(clock<5.74f)phaseLabel="기술 후 대기";
            else {phaseLabel="쓰러짐";death=Mathf.Clamp01((clock-5.74f)/1.2f);}
        }
    }
    void Draw()
    {
        if(selected==Clips.Length&&resetPose)
        {
            // Rebuild stateful blending and gait when seeking through the combat sequence.
            float target=clock;clock=0;PoseFrame();
            int frames=Mathf.CeilToInt(target*60);
            for(int frame=1;frame<=frames;frame++)
            {clock=Mathf.Min(frame/60f,target);PoseFrame();}
            clock=target;
        }
        else PoseFrame();
        arena.Render();
    }
    void PoseFrame()
    {
        Vector3 position=origin;float speed=0,cast=-1,attack=0,death=0;
        bool sequence=selected==Clips.Length;
        if(sequence)Sequence(out position,out speed,out cast,out attack,out death);
        else {phaseLabel=selected==4?DigimonSkillCatalog.Find(ModelId).name:Labels[selected];if(selected==4)cast=clock;}
        DigimonModelLibrary.InspectionClip=sequence?null:Clips[selected];
        arena.BeginFrame(-1,-1,-1,false);
        arena.SetActor("agumon",position,null,new Color(.25f,.78f,.70f));
        if(resetPose){arena.ResetModelPose("agumon");resetPose=false;}
        arena.FaceActor("agumon",Quaternion.Euler(0,yaw,0)*Vector3.forward);
        arena.PoseDigimon("agumon",ModelId,speed,0,clock,cast,attack,death);
        Bounds bounds;
        if(frameModel&&arena.ModelBounds("agumon",out bounds))
        {
            float extent=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))*.66f;
            Vector3 focus=origin+Vector3.up*(bounds.center.y*.66f);
            arena.SetView(focus+new Vector3(.57f,.32f,-.76f).normalized*Mathf.Max(1.8f,extent*2.25f),focus,34);
            frameModel=false;
        }
        var skill=DigimonSkillCatalog.Find(ModelId);
        if(cast>=0&&cast<skill.Duration)
            arena.DrawSkill(skill,position,position+(Quaternion.Euler(0,yaw,0)*Vector3.forward)*2,cast);
    }
    void OnGUI()
    {
        if(arena==null)return;
        if(heading==null)
        {
            heading=new GUIStyle(GUI.skin.label){fontSize=24};small=new GUIStyle(GUI.skin.label){fontSize=16};
            centered=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
        }
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/900f);
        Matrix4x4 before=GUI.matrix;
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-900*scale)*.5f,0),Quaternion.identity,new Vector3(scale,scale,1));
        if(Event.current.type==EventType.Repaint)Draw();
        GUI.DrawTexture(new Rect(0,0,1280,680),arena.Texture,ScaleMode.StretchToFill,false);
        GUI.Label(new Rect(28,18,920,38),DigimonModelLibrary.RosterNames[modelIndex]+" · 3D 모션 확인",heading);
        if(GUI.Button(new Rect(970,20,280,38),"캐릭터 선택 · "+(modelIndex+1)+" / "+DigimonModelLibrary.Roster.Length))showRoster=!showRoster;
        GUI.Label(new Rect(28,58,960,30),(selected==4?"고유 기술":Labels[selected])+" / "+phaseLabel,small);
        for(int i=0;i<Labels.Length;i++)
        {
            Color color=GUI.backgroundColor;if(i==selected)GUI.backgroundColor=new Color(.35f,.85f,.78f);
            if(GUI.Button(new Rect(28+i*136,695,128,38),i==4?"고유 기술":Labels[i]))Select(i);
            GUI.backgroundColor=color;
        }
        GUI.Label(new Rect(28,747,138,28),"재생 위치",small);
        float seek=GUI.HorizontalSlider(new Rect(166,756,844,24),clock,0,Duration);
        if(Mathf.Abs(seek-clock)>.00001f)Seek(seek);
        GUI.Label(new Rect(1025,744,225,32),clock.ToString("0.00")+" / "+Duration.ToString("0.00")+" 초",centered);
        if(GUI.Button(new Rect(28,791,124,36),paused?"재생 (Space)":"정지 (Space)"))paused=!paused;
        if(GUI.Button(new Rect(162,791,104,36),"처음으로"))Seek(0);
        if(GUI.Button(new Rect(276,791,116,36),"◀ 이전 프레임"))Seek(clock-FrameStep);
        if(GUI.Button(new Rect(402,791,116,36),"다음 프레임 ▶"))Seek(clock+FrameStep);
        repeat=GUI.Toggle(new Rect(545,798,95,30),repeat,"반복 재생");
        GUI.Label(new Rect(674,797,92,30),"재생 속도",small);
        for(int i=0;i<Rates.Length;i++)
        {
            Color color=GUI.backgroundColor;if(i==rateIndex)GUI.backgroundColor=new Color(.35f,.85f,.78f);
            if(GUI.Button(new Rect(772+i*94,791,84,36),Rates[i].ToString("0.##")+"×"))rateIndex=i;
            GUI.backgroundColor=color;
        }
        GUI.Label(new Rect(28,852,770,28),"PgUp/PgDn 캐릭터 · ← → 프레임 이동 · Home 처음 · 회전 가능",small);
        GUI.Label(new Rect(803,852,72,28),"회전",small);
        float newYaw=GUI.HorizontalSlider(new Rect(872,861,368,24),yaw,0,360);
        if(!Mathf.Approximately(newYaw,yaw)){yaw=newYaw;if(paused&&selected==Clips.Length)resetPose=true;}
        if(showRoster)
        {
            GUI.Box(new Rect(650,70,602,352),"전체 디지몬 · 제작 검토 모델");
            for(int i=0;i<DigimonModelLibrary.Roster.Length;i++)
                if(GUI.Button(new Rect(662+(i%4)*146,104+(i/4)*34,140,30),DigimonModelLibrary.RosterNames[i]))SelectModel(i);
        }
        GUI.matrix=before;
    }
    bool Validate()
    {
        try
        {
            AuthoredModelValidation.Validate();
            Draw();
            for(int i=0;i<Clips.Length;i++)
                if(arena.ModelClip("agumon",Clips[i])==null)throw new InvalidOperationException("Missing authored clip: "+Clips[i]);
            Select(6);Seek(100);Advance(FrameStep);
            if(!paused||Mathf.Abs(clock-1.2f)>.001f)throw new InvalidOperationException("Defeat must hold its final pose.");
            Select(1);paused=false;Advance(Duration+.1f);
            if(Mathf.Abs(clock-.1f)>.001f)throw new InvalidOperationException("Loop must preserve overflow time.");
            Seek(-1);if(clock!=0||!paused)throw new InvalidOperationException("Seeking clamps and pauses.");
            Select(0);return true;
        }
        catch(Exception exception){Debug.LogException(exception);Application.Quit(1);return false;}
    }
    void SaveArena(string file)
    {SaveTexture(file,arena.Texture,arena.Texture.width,arena.Texture.height);}
    void SaveTexture(string file,RenderTexture source,int width,int height)
    {
        RenderTexture before=RenderTexture.active;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try
        {
            RenderTexture.active=source;image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();
            File.WriteAllBytes(file,image.EncodeToPNG());
        }
        finally{RenderTexture.active=before;Destroy(image);}
    }
    IEnumerator Capture()
    {
        yield return null;
        if(!Validate())yield break;
        string directory=Path.Combine(Application.dataPath,"../ModelCaptures");Directory.CreateDirectory(directory);
        int[] modes={0,1,2,3,4,5,6,7,0,0};
        float[] times={.3f,.2f,.15f,.31f,.40f,.10f,1.1f,.3f,.3f,.3f};
        for(int i=0;i<modes.Length;i++)
        {
            Select(modes[i]);Seek(times[i]);yaw=i==8?30:i==9?270:155;Draw();
            yield return new WaitForEndOfFrame();
            SaveArena(Path.Combine(directory,"agumon-"+i+".png"));
            Debug.Log("MODEL CAPTURE "+i+" "+Clips[selected]);
        }
        Select(8);yaw=155;Draw();
        int sample=0;float[] sequenceTimes={.5f,1.8f,3.15f,3.48f,4.15f,4.7f,5.5f,6.8f};
        for(int frame=1;frame<=435;frame++)
        {
            clock=frame/60f;Draw();
            if(sample<sequenceTimes.Length&&clock>=sequenceTimes[sample])
            {
                yield return new WaitForEndOfFrame();
                SaveArena(Path.Combine(directory,"sequence-"+sample+".png"));sample++;
            }
        }
        Debug.Log("MODEL CAPTURES COMPLETE: 8 clips, 2 viewpoints and 8 combat transition samples.");
        Application.Quit();
    }
    IEnumerator CaptureRoster()
    {
        yield return null;
        string directory=Path.Combine(Application.dataPath,"../RosterCaptures");Directory.CreateDirectory(directory);
        int checks=0;
        for(int character=0;character<DigimonModelLibrary.Roster.Length;character++)
        {
            SelectModel(character);yaw=155;Draw();
            string error=null;
            try { checks+=AuthoredRosterValidation.ValidateModel(ModelId); }
            catch(Exception exception){error=exception.ToString();}
            if(error!=null){Debug.LogError(error);Application.Quit(1);yield break;}
            foreach(int mode in new[]{0,1,3,4})
            {
                Select(mode);Seek(mode==3?.32f:mode==4?.40f:.23f);Draw();
                yield return new WaitForEndOfFrame();
                SaveArena(Path.Combine(directory,ModelId+"-"+Clips[mode]+".png"));
            }
            Debug.Log("ROSTER CAPTURE PASS: "+ModelId);
        }
        Debug.Log("ROSTER SMOKE PASS: "+DigimonModelLibrary.Roster.Length+" models; "+checks+" checks; 136 captures.");
        Application.Quit();
    }
    void OnDestroy()
    {if(arena!=null)arena.Dispose();DigimonModelLibrary.PreviewEnabled=false;DigimonModelLibrary.InspectionClip=null;}
}
