using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Nov11SuiteVerification
{
    const string Root="/tmp/dome-suite-check";
    static int index,phase,count;static double deadline,pausedAt,videoTime;static bool failed;
    static readonly float[] samples={2.7f,4.4f,4.8f,7.5f,3.4f,3.4f,3.2f,3.2f,4.5f,5.0f,3.4f,3.4f,4.5f,3.4f};
    static Nov11SuiteVerification(){EditorApplication.update+=Tick;}
    [MenuItem("Tools/Dome Study/Verify November 11 Suite")]
    public static void Begin()
    {
        Directory.CreateDirectory(Root);File.WriteAllText(Root+"/result.txt","November 11 suite runtime verification\n");
        SessionState.SetBool("Nov11Verify",true);index=phase=0;failed=false;deadline=EditorApplication.timeSinceStartup+400;
        if(!EditorApplication.isPlaying)EditorApplication.isPlaying=true;
    }
    static int Particles(DomeClipLibrary lib)
    {var fire=lib.playback.GetComponent<DomeFireworksExtension>();var ambient=lib.playback.GetComponent<DomeAmbientExtension>();return fire.VisibleParticles+ambient.VisibleParticles;}
    static void Tick()
    {
        if(!SessionState.GetBool("Nov11Verify",false))return;
        if(deadline==0)deadline=EditorApplication.timeSinceStartup+400;
        if(EditorApplication.timeSinceStartup>deadline){Finish("FAIL timeout");return;}
        if(!EditorApplication.isPlaying)return;
        var lib=UnityEngine.Object.FindAnyObjectByType<DomeClipLibrary>();if(lib==null){Finish("FAIL suite missing");return;}
        var v=lib.playback;
        if(index>=lib.Count){lib.Select(0);Finish(failed?"FAIL see entries":"PASS all 14 clips prepared, emitted particles, paused stably and restarted");return;}
        if(phase==0){lib.Select(index);phase=1;return;}
        if(v.State==DomeVideoSetup.PlaybackState.Failed){failed=true;File.AppendAllText(Root+"/result.txt",$"FAIL {index}: {v.LastError}\n");index++;phase=0;return;}
        if(phase==1&&v.State==DomeVideoSetup.PlaybackState.Playing&&v.MainTime>=samples[index])
        {
            v.PausePlayback();videoTime=v.MainTime;pausedAt=EditorApplication.timeSinceStartup;count=Particles(lib);
            if(count==0)failed=true;
            Capture(index);File.AppendAllText(Root+"/result.txt",$"{index}: {lib.presets[index].displayName}, t={videoTime:F3}, particles={count}, bg={(lib.presets[index].backgroundImage!=null?"panoramic image":lib.presets[index].useSourceEdges?"source edge fallback":"separate clip")}\n");phase=2;
        }
        else if(phase==2&&EditorApplication.timeSinceStartup-pausedAt>.4)
        {
            bool stable=Math.Abs(v.MainTime-videoTime)<.05&&Particles(lib)==count;
            if(!stable)failed=true;File.AppendAllText(Root+"/result.txt",$"  Pause stable: {stable}\n");
            v.RestartPlayback();phase=3;
        }
        else if(phase==3&&v.State==DomeVideoSetup.PlaybackState.Playing&&v.MainTime>.15&&v.MainTime<2)
        {File.AppendAllText(Root+"/result.txt",$"  Restart: {v.MainTime:F3}s\n");index++;phase=0;}
    }
    static void Finish(string result){SessionState.SetBool("Nov11Verify",false);File.AppendAllText(Root+"/result.txt",result+"\n");Debug.Log("[DomeSuite] "+result);}
    static void Capture(int index)
    {
        var cam=Camera.main;if(cam==null)return;
        var rt=RenderTexture.GetTemporary(960,540,24);var previous=cam.targetTexture;var active=RenderTexture.active;
        var tex=new Texture2D(960,540,TextureFormat.RGB24,false);
        try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,960,540),0,0);tex.Apply();File.WriteAllBytes(Root+$"/{index:00}.png",tex.EncodeToPNG());}
        finally{cam.targetTexture=previous;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
    }
}
