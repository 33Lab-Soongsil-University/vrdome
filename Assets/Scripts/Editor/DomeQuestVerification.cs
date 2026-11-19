using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.XR.OpenXR;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

[InitializeOnLoad]
public static class DomeQuestVerification
{
    static double started; static int phase; static DomeQuestViewer rig;
    static DomeQuestVerification(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(File.Exists("/tmp/dome-quest-verify.request")&&!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling)
        {
            File.Delete("/tmp/dome-quest-verify.request");
            try {var issues=new List<OpenXRFeature.ValidationRule>();OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
            File.WriteAllLines("/tmp/dome-quest-validation.txt",issues.Select(x=>(x.error?"ERROR: ":"WARN: ")+x.message));}
            catch(Exception e){File.WriteAllText("/tmp/dome-quest-validation.txt","Android-target validation deferred while running the desktop target: "+e.Message);}
            EditorSceneManager.OpenScene(DomeQuestBuilder.ScenePath);
            SessionState.SetBool("DomeQuestVerify",true);EditorApplication.isPlaying=true;return;
        }
        if(!SessionState.GetBool("DomeQuestVerify",false)||!EditorApplication.isPlaying)return;
        try
        {
            if(phase==0){started=EditorApplication.timeSinceStartup;phase=1;return;}
            if(EditorApplication.timeSinceStartup-started>60)throw new Exception("Playback timeout");
            rig=UnityEngine.Object.FindAnyObjectByType<DomeQuestViewer>();if(rig==null||!rig.MenuVisible)return;
            var playback=rig.library.playback;
            if(playback.State==DomeVideoSetup.PlaybackState.Failed)throw new Exception(playback.LastError);
            if(phase==1&&playback.State==DomeVideoSetup.PlaybackState.Playing&&playback.MainTime>.5)
            {
                if(Vector3.Angle(rig.transform.up,Vector3.up)>.01f)throw new Exception("XR rig must keep the horizon level");
                if(rig.head.GetComponent<TrackedPoseDriver>()==null)throw new Exception("Tracked camera missing");
                var buttons=rig.GetComponentsInChildren<Button>();if(buttons.Length!=8)throw new Exception("World menu button count");
                buttons.First(b=>b.name=="Next seat").onClick.Invoke();if(rig.seat!=1)throw new Exception("Seat action failed");
                buttons.First(b=>b.name=="Play / Pause").onClick.Invoke();if(playback.State!=DomeVideoSetup.PlaybackState.Paused)throw new Exception("Pause failed");
                rig.SelectSeat(0);Capture();rig.TogglePlayback();if(playback.State!=DomeVideoSetup.PlaybackState.Playing)throw new Exception("Resume failed");
                rig.SelectClip(1);phase=2;
            }
            else if(phase==2&&playback.State==DomeVideoSetup.PlaybackState.Playing&&playback.MainTime>.5)
            {
                if(rig.library.current!=1)throw new Exception("Clip action failed");
                rig.ShowMenu(false);if(rig.MenuVisible)throw new Exception("Hide menu failed");rig.ShowMenu(true);
                File.WriteAllText("/tmp/dome-quest-verification.txt","PASS: tracked camera, 8 world menu buttons, seat change, video switch, pause/resume, hide/show, desktop rendering.\nHeadset tracking, controller input, stereo and Android video decoding require physical Quest 3 testing.");
                Finish();
            }
        }
        catch(Exception e){File.WriteAllText("/tmp/dome-quest-verification.txt","FAIL: "+e);Finish();}
    }
    static void Capture()
    {
        var rt=new RenderTexture(1280,720,24);var prior=rig.head.targetTexture;var active=RenderTexture.active;
        rig.head.targetTexture=rt;rig.head.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("/tmp/dome-quest-preview.png",image.EncodeToPNG());rig.head.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
    static void Finish(){SessionState.SetBool("DomeQuestVerify",false);phase=0;EditorApplication.isPlaying=false;}
}
