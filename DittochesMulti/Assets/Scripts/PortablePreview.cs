#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.IO;
using UnityEngine;

/// <summary>Local Mono-player preview only. Never included in a regular Unity build.</summary>
public static class PortablePreview
{
    public static bool HasArgument(string argument){return Array.IndexOf(Environment.GetCommandLineArgs(),argument)>=0;}
    public static bool TryBoot()
    {
        Debug.Log("PORTABLE PREVIEW: current scripts; existing player assets; substitute arena shader.");
        var display=new GameObject("Portable display camera");UnityEngine.Object.DontDestroyOnLoad(display);
        var displayCamera=display.AddComponent<Camera>();displayCamera.cullingMask=0;displayCamera.depth=-100;
        displayCamera.clearFlags=CameraClearFlags.SolidColor;displayCamera.backgroundColor=new Color(.012f,.022f,.04f);
        foreach(string name in new[]{"Sprites/Default","UI/Default","Unlit/Texture","Unlit/Color","Standard","Hidden/Internal-Colored"})
        {var shader=Shader.Find(name);Debug.Log("PREVIEW SHADER "+name+": "+(shader!=null?shader.isSupported.ToString():"missing"));}
        if(HasArgument("--model-gallery")){new GameObject("3D Model Preview").AddComponent<PortableModelGallery>();return true;}
        if(HasArgument("--arena-smoke")){new GameObject("Arena Runtime Validation").AddComponent<NativeGame>().BeginArenaSmoke();return true;}
        if(HasArgument("--online-smoke")){new GameObject("Online Runtime Validation").AddComponent<MultiLauncher>().BeginOnlineSmoke();return true;}
        if(!HasArgument("--skill-gallery"))return false;
        new GameObject("Canonical Skill Preview").AddComponent<PortableSkillGallery>();return true;
    }
    public static string SkillJson()
    {return File.ReadAllText(Path.Combine(Application.streamingAssetsPath,"DigimonSkills.json"));}
    public static Texture2D Texture(string resourcePath)
    {
        string file=Path.Combine(Application.streamingAssetsPath,"PreviewArt",resourcePath+".png");
        if(!File.Exists(file))return null;
        var texture=new Texture2D(2,2,TextureFormat.RGBA32,false){name=resourcePath,filterMode=FilterMode.Bilinear};
        if(ImageConversion.LoadImage(texture,File.ReadAllBytes(file)))return texture;
        UnityEngine.Object.Destroy(texture);return null;
    }
    public static Shader ArenaShader(bool textured)
    {
        if(!textured)
        {var solid=Shader.Find("Hidden/Internal-Colored");if(solid!=null&&solid.isSupported)return solid;}
        foreach(string name in new[]{"Sprites/Default","UI/Default","Unlit/Texture","Standard"})
        {var shader=Shader.Find(name);if(shader!=null&&shader.isSupported)return shader;}
        throw new InvalidOperationException("The existing player has no usable preview shader. A Unity rebuild is required.");
    }
    public static void ConfigureMaterial(Material material,bool textured)
    {
        if(textured)return;
        material.SetInt("_SrcBlend",(int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend",(int)UnityEngine.Rendering.BlendMode.Zero);
        material.SetInt("_ZWrite",1);material.SetInt("_ZTest",(int)UnityEngine.Rendering.CompareFunction.LessEqual);
        material.SetInt("_Cull",(int)UnityEngine.Rendering.CullMode.Off);material.renderQueue=2000;
    }
}

public static class PortablePreviewPrefs
{
    static string Prefix { get { return PortablePreview.HasArgument("--arena-smoke")||PortablePreview.HasArgument("--online-smoke")?"portable-validation.":"portable-preview."; } }
    public static bool HasKey(string key){return PlayerPrefs.HasKey(Prefix+key);}
    public static int GetInt(string key,int value=0){return PlayerPrefs.GetInt(Prefix+key,value);}
    public static string GetString(string key,string value=""){return PlayerPrefs.GetString(Prefix+key,value);}
    public static void SetInt(string key,int value){PlayerPrefs.SetInt(Prefix+key,value);}
    public static void SetString(string key,string value){PlayerPrefs.SetString(Prefix+key,value);}
    public static void Save(){PlayerPrefs.Save();}
}
#endif
