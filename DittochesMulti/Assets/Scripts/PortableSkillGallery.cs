#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed class PortableSkillGallery : MonoBehaviour
{
    [Serializable] sealed class SkillDocument { public DigimonSkillCatalog.Entry[] skills=new DigimonSkillCatalog.Entry[0]; }
    TacticalArena arena;
    DigimonSkillCatalog.Entry[] skills;
    MultiLauncher.Catalog roster;
    int index;
    float age;
    bool paused,capturing;
    GUIStyle title,label;
    readonly System.Collections.Generic.Dictionary<string,Texture2D> portraits=new System.Collections.Generic.Dictionary<string,Texture2D>();
    readonly Vector3 source=TacticalArena.CellWorld(3,5),target=TacticalArena.CellWorld(3,2);
    void Awake()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        skills=JsonUtility.FromJson<SkillDocument>(PortablePreview.SkillJson()).skills;
        roster=JsonUtility.FromJson<MultiLauncher.Catalog>(Resources.Load<TextAsset>("MultiRoster").text);
        arena=new TacticalArena(TacticalArena.SoloViewport);
        capturing=PortablePreview.HasArgument("--capture-all");
        if(capturing)StartCoroutine(CaptureAll());
    }
    Texture2D Portrait(string id)
    {
        Texture2D cached;if(portraits.TryGetValue(id,out cached))return cached;
        string current="ArtVariants/LicensedFanArt/"+char.ToUpper(id[0])+id.Substring(1)+"-unit-v2";
        cached=Resources.Load<Texture2D>(current)??PortablePreview.Texture(current);
        var definition=Array.Find(roster.units,u=>u.id==id);
        string sprite=definition!=null?definition.sprite:char.ToUpper(id[0])+id.Substring(1);
        if(cached==null)cached=Resources.Load<Texture2D>("Sprites/"+sprite);
        portraits[id]=cached;return cached;
    }
    void Update(){if(!paused&&!capturing)age=(age+Time.deltaTime)%(skills[index].Duration+.7f);}
    void DrawArena()
    {
        var s=skills[index];float showAge=age<s.Duration?age:-1;
        arena.BeginFrame(-1,-1,-1,false);
        arena.SetActor("caster",source,Portrait(s.id),new Color(.25f,1,.62f));
        arena.PoseDigimon("caster",s.id,0,0,age,showAge);
        arena.SetActor("target",target,Portrait("greymon"),new Color(1,.35f,.28f));
        arena.PoseDigimon("target","greymon",0,0,age);
        arena.DecorateActor("target",false,0,age>=s.Impact&&age<s.Impact+.2f?1:0);
        if(showAge>=0)arena.DrawSkill(s,source,target,age);
        arena.Render();
    }
    void OnGUI()
    {
        if(arena==null)return;
        if(title==null)
        {title=new GUIStyle(GUI.skin.label){fontSize=25,alignment=TextAnchor.MiddleCenter};label=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};}
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/940f);
        Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        var skill=skills[index];
        if(Event.current.type==EventType.Repaint)DrawArena();
        GUI.Label(new Rect(100,12,1080,42),skill.id+"  /  "+skill.name+"  ("+skill.technique+")",title);
        GUI.Label(new Rect(180,55,920,32),"스킬 시험판 · 기존 이미지 + 동작/이펙트 · 대체 셰이더 사용",label);
        GUI.DrawTexture(new Rect(140,94,1000,664),arena.Texture,ScaleMode.ScaleToFit,false);
        GUI.Label(new Rect(140,765,1000,50),skill.description,label);
        if(GUI.Button(new Rect(140,820,160,40),"이전 유닛")){index=(index+skills.Length-1)%skills.Length;age=0;}
        if(GUI.Button(new Rect(320,820,160,40),paused?"재생":"일시 정지"))paused=!paused;
        if(GUI.Button(new Rect(500,820,160,40),"다음 유닛")){index=(index+1)%skills.Length;age=0;}
        age=GUI.HorizontalSlider(new Rect(690,835,450,24),age,0,skill.Duration+.69f);
        GUI.Label(new Rect(140,878,1000,35),(index+1)+" / "+skills.Length+"   ·   "+age.ToString("0.00")+" s",label);
        GUI.matrix=old;
    }
    IEnumerator CaptureAll()
    {
        yield return null;
        string directory=Path.Combine(Application.dataPath,"../Captures");Directory.CreateDirectory(directory);
        for(index=0;index<skills.Length;index++)
        {
            var s=skills[index];float[] samples={s.windup*.65f,s.windup+s.travel*.55f,s.Impact+.04f};
            for(int phase=0;phase<samples.Length;phase++)
            {
                age=samples[phase];DrawArena();yield return new WaitForEndOfFrame();
                var previous=RenderTexture.active;
                var texture=new Texture2D(arena.Texture.width,arena.Texture.height,TextureFormat.RGB24,false);
                try
                {
                    RenderTexture.active=arena.Texture;
                    texture.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);texture.Apply();
                    File.WriteAllBytes(Path.Combine(directory,s.id+"-"+phase+".png"),texture.EncodeToPNG());
                    Debug.Log("PREVIEW CAPTURE "+s.id+" phase="+phase+" effects="+arena.EffectCount);
                }
                finally{RenderTexture.active=previous;Destroy(texture);}
            }
        }
        Debug.Log("PREVIEW CAPTURES COMPLETE: "+skills.Length*3);Application.Quit();
        index=0;
    }
    void OnDestroy(){if(arena!=null)arena.Dispose();}
}
#endif
