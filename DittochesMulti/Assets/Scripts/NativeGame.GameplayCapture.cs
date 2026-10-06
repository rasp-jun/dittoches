#if DITTOCHES_PORTABLE_PREVIEW
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed partial class NativeGame
{
    [Serializable] sealed class GameplayFrame {public string file,section;public float time;public int repaints;}
    [Serializable] sealed class GameplayRecording {public bool passed;public int width,height;public float seconds;public string source="Actual solo game UI and combat, staged formation, isolated capture saves";public List<GameplayFrame> frames=new List<GameplayFrame>();}
    public void BeginGameplayCapture(){StartCoroutine(CaptureGameplay());}
    IEnumerator CaptureGameplay()
    {
        Application.runInBackground=true;Application.targetFrameRate=60;
        yield return null;
        UnityEngine.Random.InitState(61006);ResetGame();
        artPack=0;lobby=false;round=18;level=6;gold=40;hp=100;showCombatReport=false;showCarousel=false;
        selectedBoard=selectedBench=selectedItem=-1;traitFocus=null;showRecipeGuide=false;skillDetailUnit=null;
        Array.Clear(board,0,board.Length);Array.Clear(bench,0,bench.Length);inventory.Clear();lootOrbs.Clear();
        string[] team={"greymon","garurumon","wargreymon","metalgreymon","gabumon","lilimon"};
        int[] slots={2,3,4,16,17,18};
        for(int i=0;i<team.Length;i++)board[slots[i]]=new Unit(RosterById[team[i]]);
        board[2].items.Add(8);board[16].items.Add(11);board[4].items.Add(4);
        bench[0]=new Unit(RosterById["koromon"]);bench[3]=new Unit(RosterById["agumon"]);
        inventory.AddRange(new[]{0,1,1,2,2,3});RebuildPool();RollShop();
        tamerLoadout=new TamerLoadout{tamer=1,field=0,finisher=1};legend=1;
        EnsureArena();validationPointer=new Vector2(1900,1070);
        string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../GameplayCaptureSmooth-20261006"));
        Directory.CreateDirectory(Path.Combine(folder,"frames"));
        var report=new GameplayRecording();
        // Load guide portraits/layouts before recording so first-open IO is outside the take.
        OpenTraitGuide("courage");yield return new WaitForEndOfFrame();
        OpenTraitGuide("friendship");yield return new WaitForEndOfFrame();
        OpenEquipmentGuide(8);yield return new WaitForEndOfFrame();
        OpenEquipmentGuide(11);yield return new WaitForEndOfFrame();
        traitFocus=null;showRecipeGuide=false;
        yield return new WaitForSecondsRealtime(1);
        var endOfFrame=new WaitForEndOfFrame();float started=Time.realtimeSinceStartup,nextCapture=0;int phase=-1,count=0;
        System.Diagnostics.Process encoder=null;
        while(Time.realtimeSinceStartup-started<30)
        {
            float elapsed=Time.realtimeSinceStartup-started;
            int nextPhase=elapsed<3?0:elapsed<7?1:elapsed<11?2:elapsed<15?3:elapsed<19?4:5;
            if(nextPhase!=phase)
            {
                phase=nextPhase;
                if(phase==1)OpenTraitGuide("courage");
                else if(phase==2)OpenTraitGuide("friendship");
                else if(phase==3)OpenEquipmentGuide(8);
                else if(phase==4)OpenEquipmentGuide(11);
                else if(phase==5){traitFocus=null;showRecipeGuide=false;StartCoroutine(Battle());}
                Debug.Log("GAMEPLAY CAPTURE SECTION "+phase+" @ "+elapsed);
            }
            yield return endOfFrame;
            float timestamp=Time.realtimeSinceStartup-started;
            if(timestamp<nextCapture)continue;
            var frame=ScreenCapture.CaptureScreenshotAsTexture();
            string file="frames/"+(count+1).ToString("D5")+".jpg";
            if(encoder==null)
            {
                string format=frame.format==TextureFormat.RGB24?"rgb24":frame.format==TextureFormat.RGBA32?"rgba":null;
                if(format==null)throw new InvalidOperationException("Unsupported screen pixel format "+frame.format);
                string ffmpeg=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../tmp/media_tools/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe"));
                string arguments="-y -hide_banner -loglevel error -f rawvideo -pixel_format "+format+" -video_size "+frame.width+"x"+frame.height+
                    " -framerate 30 -i pipe:0 -vf vflip -q:v 2 \""+Path.Combine(folder,"frames/%05d.jpg")+"\"";
                encoder=System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ffmpeg,arguments){UseShellExecute=false,CreateNoWindow=true,RedirectStandardInput=true,RedirectStandardError=true});
                Debug.Log("GAMEPLAY RAW CAPTURE "+frame.format);
            }
            byte[] pixels=frame.GetRawTextureData();encoder.StandardInput.BaseStream.Write(pixels,0,pixels.Length);
            report.width=frame.width;report.height=frame.height;Destroy(frame);
            report.frames.Add(new GameplayFrame{file=file,time=timestamp,section=new[]{"prepare","synergy-courage","synergy-friendship","equipment-shield","equipment-weapon","combat"}[phase],repaints=validationRepaints});
            count++;nextCapture=timestamp+1f/30;
        }
        if(encoder!=null)
        {
            encoder.StandardInput.Close();string error=encoder.StandardError.ReadToEnd();encoder.WaitForExit();
            if(encoder.ExitCode!=0)throw new InvalidOperationException("Capture encoder failed: "+error);
            encoder.Dispose();
        }
        report.seconds=30;report.passed=count>=200&&validationRepaints>100&&phase==5;
        File.WriteAllText(Path.Combine(folder,"capture-report.json"),JsonUtility.ToJson(report,true));
        Debug.Log("GAMEPLAY CAPTURE COMPLETE "+count+" frames / "+report.seconds+" seconds / "+report.width+"x"+report.height);
        Application.Quit(report.passed?0:2);
    }
}
#endif
