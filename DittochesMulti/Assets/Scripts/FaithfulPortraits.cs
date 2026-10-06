using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Static portraits rendered from the same reviewed meshes as the battlefield.</summary>
public static class FaithfulPortraits
{
    public const string Prefix="FaithfulPortraits/";
    static readonly Dictionary<string,Texture2D> cache=new Dictionary<string,Texture2D>();
    public static Texture2D Get(string id,bool bust=false)
    {
        if(string.IsNullOrEmpty(id)||Array.IndexOf(DigimonModelLibrary.Roster,id)<0)return null;
        string key=id+(bust?"-bust":"");Texture2D result;
        if(cache.TryGetValue(key,out result))return result;
        string path=Path.Combine(Application.streamingAssetsPath,"FaithfulPortraits",key+".png");
        result=null;
        if(File.Exists(path))
        {
            try
            {
                result=new Texture2D(2,2,TextureFormat.RGBA32,true){name=Prefix+key,filterMode=FilterMode.Trilinear,wrapMode=TextureWrapMode.Clamp};
                if(!result.LoadImage(File.ReadAllBytes(path),true))throw new IOException("Invalid portrait PNG: "+id);
            }
            catch(Exception error){if(result!=null)UnityEngine.Object.Destroy(result);result=null;Debug.LogWarning(error.Message);}
        }
        cache[key]=result;return result;
    }
    public static void Draw(Rect rect,string id,bool bust=false)
    {var image=Get(id,bust)??Get(id);if(image!=null)GUI.DrawTexture(rect,image,ScaleMode.ScaleToFit,true);}
}
