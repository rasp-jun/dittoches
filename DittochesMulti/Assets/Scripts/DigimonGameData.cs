using System;
using System.IO;
using UnityEngine;

/// <summary>Loads the same reviewed catalog in Editor and the shipped preview player.</summary>
public static class DigimonGameData
{
    public static string Read(string name)
    {
#if DITTOCHES_PORTABLE_PREVIEW
        string path=Path.Combine(Application.streamingAssetsPath,name+".json");
        if(File.Exists(path))return File.ReadAllText(path);
#endif
        var asset=Resources.Load<TextAsset>(name);
        if(asset==null)throw new InvalidOperationException("Missing game catalog: "+name);
        return asset.text;
    }
}
