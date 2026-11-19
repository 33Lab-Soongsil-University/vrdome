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
                var driver=rig.head.GetComponent<TrackedPoseDriver>();
                if(rig.IsVR||driver.enabled)throw new Exception("No HMD: PC input must own the camera");
                Call("ApplyInputMode",true,false);
                if(!rig.IsVR||!driver.enabled)throw new Exception("VR transition must restore tracked camera");
                var before=rig.head.transform.localRotation;
                Call("HandleDesktopNavigation",new Event{type=EventType.MouseDrag,button=1,delta=new Vector2(80,20)});
                if(Quaternion.Angle(before,rig.head.transform.localRotation)>.01f)throw new Exception("Desktop input changed VR head pose");
                Call("ApplyInputMode",false,false);
                Call("HandleDesktopNavigation",new Event{type=EventType.MouseDrag,button=1,delta=new Vector2(80,20)});
                if(Quaternion.Angle(Quaternion.identity,rig.head.transform.localRotation)<1)throw new Exception("PC look failed");
                Call("HandleDesktopNavigation",new Event{type=EventType.KeyDown,keyCode=KeyCode.R});
                if(Quaternion.Angle(Quaternion.identity,rig.head.transform.localRotation)>.01f)throw new Exception("PC recenter failed");
                var buttons=rig.GetComponentsInChildren<Button>();if(buttons.Length!=8)throw new Exception("World menu button count");
                Physics.SyncTransforms();
                var point=rig.head.WorldToScreenPoint(buttons.First(b=>b.name=="Next seat").transform.position);
                Call("DesktopMenuInput",new Vector2(point.x,Screen.height-point.y),true);if(rig.seat!=1)throw new Exception("Seat action failed");
                Call("HandleDesktopNavigation",new Event{type=EventType.KeyDown,keyCode=KeyCode.Space});if(playback.State!=DomeVideoSetup.PlaybackState.Paused)throw new Exception("Pause failed");
                rig.SelectSeat(0);Capture();rig.TogglePlayback();if(playback.State!=DomeVideoSetup.PlaybackState.Playing)throw new Exception("Resume failed");
                Call("HandleDesktopNavigation",new Event{type=EventType.KeyDown,keyCode=KeyCode.RightArrow});phase=2;
            }
            else if(phase==2&&playback.State==DomeVideoSetup.PlaybackState.Playing&&playback.MainTime>.5)
            {
                if(rig.library.current!=1)throw new Exception("Clip action failed");
                rig.ShowMenu(false);if(rig.MenuVisible)throw new Exception("Hide menu failed");rig.ShowMenu(true);
                File.WriteAllText("/tmp/dome-quest-verification.txt","PASS: automatic desktop mode without HMD; simulated VR/PC transitions and camera ownership; right-drag look, R recenter, mouse hit-test seat selection, keyboard video switch and pause; 8 menu buttons, playback and rendering.\nHeadset tracking, controller input, stereo and Android video decoding require physical Quest 3 testing.");
                Finish();
            }
        }
        catch(Exception e){File.WriteAllText("/tmp/dome-quest-verification.txt","FAIL: "+e);Finish();}
    }
    static object Call(string name,params object[] args)
    {return typeof(DomeQuestViewer).GetMethod(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rig,args);}
    static void Capture()
    {
        var rt=new RenderTexture(1280,720,24);var prior=rig.head.targetTexture;var active=RenderTexture.active;
        rig.head.targetTexture=rt;rig.head.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("/tmp/dome-quest-preview.png",image.EncodeToPNG());rig.head.targetTexture=prior;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);
    }
    static void Finish(){SessionState.SetBool("DomeQuestVerify",false);phase=0;EditorApplication.isPlaying=false;}
}
