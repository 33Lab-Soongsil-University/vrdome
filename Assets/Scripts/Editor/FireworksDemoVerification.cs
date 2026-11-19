using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FireworksDemoVerification
{
    static int phase;
    static double deadline, pausedAt, pausedTime;
    static int pausedCount;
    const string Root = "/tmp/dome-fireworks-check";
    static FireworksDemoVerification() { EditorApplication.update += Tick; }
    [MenuItem("Tools/Dome Study/Verify Fireworks Playback")]
    public static void Begin()
    {
        Directory.CreateDirectory(Root);
        File.WriteAllText(Root+"/result.txt", "Verification started\n");
        SessionState.SetBool("FireworksVerify", true);
        phase = 0; deadline = EditorApplication.timeSinceStartup + 90;
        if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        else { var v = UnityEngine.Object.FindAnyObjectByType<DomeVideoSetup>(); v.RestartPlayback(); }
    }
    static void Tick()
    {
        if (!SessionState.GetBool("FireworksVerify",false)) return;
        if (deadline == 0) deadline = EditorApplication.timeSinceStartup + 90;
        if (EditorApplication.timeSinceStartup > deadline) { End("FAIL timeout"); return; }
        if (!EditorApplication.isPlaying) return;
        var v = UnityEngine.Object.FindAnyObjectByType<DomeVideoSetup>();
        var fx = UnityEngine.Object.FindAnyObjectByType<DomeFireworksExtension>();
        if (v == null || fx == null) { End("FAIL missing demo components"); return; }
        if (v.State == DomeVideoSetup.PlaybackState.Failed) { End("FAIL "+v.LastError); return; }
        if (phase == 0 && v.MainTime > .7)
        {
            Capture("opening"); Record("opening",v,fx); phase=1;
        }
        else if (phase == 1 && v.MainTime > 2.7)
        {
            v.PausePlayback(); pausedAt=EditorApplication.timeSinceStartup;
            pausedTime=v.MainTime; pausedCount=fx.VisibleParticles;
            Capture("extension"); Record("pause",v,fx); phase=2;
        }
        else if (phase == 2 && EditorApplication.timeSinceStartup-pausedAt>1)
        {
            bool stable=Math.Abs(v.MainTime-pausedTime)<.05 && fx.VisibleParticles==pausedCount;
            File.AppendAllText(Root+"/result.txt", "Pause stable: "+stable+"\n");
            v.ResumePlayback(); phase=3;
        }
        else if (phase == 3 && v.MainTime>4.7)
        {
            Capture("late"); Record("late",v,fx); v.RestartPlayback(); phase=4;
        }
        else if (phase == 4 && v.State==DomeVideoSetup.PlaybackState.Playing && v.MainTime>.2 && v.MainTime<1.5)
        {
            Record("restart",v,fx); v.PausePlayback(); End("PASS playback / effects / restart; see pause stability and captures");
        }
    }
    static void Record(string stage,DomeVideoSetup v,DomeFireworksExtension fx)
    { File.AppendAllText(Root+"/result.txt",$"{stage}: {v.State}, t={v.MainTime:F3}, sparks={fx.VisibleParticles}\n"); }
    static void End(string result)
    { SessionState.SetBool("FireworksVerify",false); File.AppendAllText(Root+"/result.txt",result+"\n"); Debug.Log("[FireworksDemo] "+result); }
    static void Capture(string name)
    {
        var cam=Camera.main; if(cam==null) return;
        var rt=RenderTexture.GetTemporary(1280,720,24);
        var previous=cam.targetTexture; var active=RenderTexture.active;
        var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);
        try { cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt; tex.ReadPixels(new Rect(0,0,1280,720),0,0); tex.Apply(); File.WriteAllBytes(Root+"/"+name+".png",tex.EncodeToPNG()); }
        finally { cam.targetTexture=previous; RenderTexture.active=active; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(tex); }
    }
}
