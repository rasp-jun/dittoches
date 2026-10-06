#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.IO;
using UnityEngine;

public sealed partial class MultiLauncher
{
    bool portraitReview,portraitCards;int portraitRepaints;
    public void BeginPortraitReview(){portraitReview=true;token="";StartCoroutine(PortraitReview());}
    void DrawPortraitReview()
    {
        if(Event.current.type==EventType.Repaint)portraitRepaints++;
        if(Event.current.isMouse||Event.current.isKey)Event.current.Use();
        Styles();var old=GUI.matrix;float scale=Mathf.Min(Screen.width/1600f,Screen.height/1000f);
        GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);Panel(new Rect(0,0,1600,1000),navy);
        if(!portraitCards){DrawCodexLobby();GUI.matrix=old;return;}
        GUI.Label(new Rect(310,310,970,60),"모집 카드 · 3D 초상화",title);
        var me=new Player{gold=50,board=new Unit[0],bench=new Unit[0]};
        string[] ids={"agumon","greymon","metalgreymon","wargreymon","rosemon"};
        for(int i=0;i<ids.Length;i++)DrawOnlineRecruitCard(new Rect(310+i*191,420,181,108),ids[i],i,me,false);
        GUI.matrix=old;
    }
    IEnumerator PortraitReview()
    {
        yield return null;artPack=0;
        int count=0;
        foreach(string id in DigimonModelLibrary.Roster)
        {
            var full=FaithfulPortraits.Get(id);var bust=FaithfulPortraits.Get(id,true);
            if(full==null||bust==null||full.width!=768||bust.width!=768||LobbyPortrait(id)!=full){Application.Quit(2);throw new InvalidOperationException("Portrait load failed: "+id);}
            count++;
        }
        string output=Path.GetFullPath(Path.Combine(Application.dataPath,"..","PortraitValidation"));Directory.CreateDirectory(output);
        for(int page=0;page<Mathf.CeilToInt(catalog.units.Length/15f)+1;page++)
        {
            portraitCards=page==Mathf.CeilToInt(catalog.units.Length/15f);codexPage=page;yield return new WaitForSeconds(.3f);yield return new WaitForEndOfFrame();
            var capture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(output,"codex-"+page+".png"),capture.EncodeToPNG());Destroy(capture);
        }
        bool passed=count==34&&portraitRepaints>0;
        File.WriteAllText(Path.Combine(output,"ui-report.json"),"{\"passed\":"+passed.ToString().ToLowerInvariant()+",\"portraits\":"+count+",\"repaints\":"+portraitRepaints+"}");
        Debug.Log("PORTRAIT UI RESULT "+passed);Application.Quit(passed?0:1);
    }
}
#endif
