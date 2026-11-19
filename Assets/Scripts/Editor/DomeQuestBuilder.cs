using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using Unity.XR.CoreUtils;

public static class DomeQuestBuilder
{
    public const string ScenePath="Assets/Scenes/quest3_dome.unity";
    [InitializeOnLoadMethod] static void Auto()
    {
        if(File.Exists(Path.Combine(Path.GetTempPath(),"dome-quest-setup.request"))){File.Delete(Path.Combine(Path.GetTempPath(),"dome-quest-setup.request"));EditorApplication.delayCall+=Setup;}
    }
    [MenuItem("Tools/Dome Study/PC VR/Configure Windows Link")]
    public static void Setup()
    {
        if(EditorApplication.isPlaying){Debug.LogError("Stop Play before Quest setup.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        ConfigureWindows();
        if(!File.Exists(ScenePath))
        {
            var original=EditorSceneManager.OpenScene("Assets/Scenes/nov11_video_suite.unity");EditorSceneManager.SaveScene(original,ScenePath,true);
        }
        var scene=EditorSceneManager.OpenScene(ScenePath);
        var view=UnityEngine.Object.FindAnyObjectByType<SphereTheaterView>();var library=UnityEngine.Object.FindAnyObjectByType<DomeClipLibrary>();
        var cam=Camera.main;var rig=UnityEngine.Object.FindAnyObjectByType<DomeQuestViewer>();
        if(rig==null)
        {
            var root=new GameObject("Quest 3 XR Origin");root.transform.SetPositionAndRotation(cam.transform.position,Quaternion.Euler(0,cam.transform.eulerAngles.y,0));
            var offset=new GameObject("Tracking space");offset.transform.SetParent(root.transform,false);
            cam.transform.SetParent(offset.transform,false);cam.transform.localPosition=Vector3.zero;cam.transform.localRotation=Quaternion.identity;
            var origin=root.AddComponent<XROrigin>();origin.Origin=root;origin.Camera=cam;origin.CameraFloorOffsetObject=offset;origin.CameraYOffset=0;origin.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Device;
            rig=root.AddComponent<DomeQuestViewer>();
        }
        rig.transform.rotation=Quaternion.Euler(0,rig.transform.eulerAngles.y,0);
        cam.transform.localPosition=Vector3.zero;cam.transform.localRotation=Quaternion.identity;
        var driver=cam.GetComponent<TrackedPoseDriver>();if(driver==null)driver=cam.gameObject.AddComponent<TrackedPoseDriver>();
        driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice,TrackedPoseDriver.TrackedPose.Center);
        driver.trackingType=TrackedPoseDriver.TrackingType.RotationAndPosition;driver.updateType=TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        cam.nearClipPlane=.05f;cam.farClipPlane=160;cam.allowHDR=false;cam.allowMSAA=true;cam.stereoTargetEye=StereoTargetEyeMask.Both;
        var urp=cam.GetUniversalAdditionalCameraData();urp.renderPostProcessing=false;urp.renderShadows=false;
        rig.head=cam;rig.library=library;rig.seats=view!=null?view.seats:rig.seats;rig.seatNames=view!=null?view.labels:rig.seatNames;rig.pointerShader=Shader.Find("Universal Render Pipeline/Unlit");
        if(view!=null)view.enabled=false;library.showInterface=false;library.playback.showControls=false;
        if(view!=null)EditorUtility.SetDirty(view);EditorUtility.SetDirty(library);EditorUtility.SetDirty(rig);
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>())
        {
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            if(renderer.GetComponent<DomePreview>()==null)GameObjectUtility.SetStaticEditorFlags(renderer.gameObject,GameObjectUtility.GetStaticEditorFlags(renderer.gameObject)|StaticEditorFlags.BatchingStatic);
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        File.WriteAllText(Path.Combine(Path.GetTempPath(),"dome-quest-setup.txt"),"Quest scene configured. Windows OpenXR + Touch profile, tracked camera, world-space menu.\n"+ScenePath);
        Debug.Log("[DomeQuest] VR scene configured: "+ScenePath);
    }
    public static void ConfigureWindows()
    {
        Directory.CreateDirectory("Assets/XR/Settings");
        if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey,out XRGeneralSettingsPerBuildTarget all))
        {all=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(all,"Assets/XR/Settings/DomeXRGeneralSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey,all,true);}
        var group=BuildTargetGroup.Standalone;
        if(!all.HasSettingsForBuildTarget(group))all.CreateDefaultSettingsForBuildTarget(group);
        if(!all.HasManagerSettingsForBuildTarget(group))all.CreateDefaultManagerSettingsForBuildTarget(group);
        var general=all.SettingsForBuildTarget(group);general.InitManagerOnStart=true;
        if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group))throw new Exception("PC OpenXR loader assignment failed");
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);
        var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
        if(settings==null)throw new Exception("PC OpenXR settings missing");
        var touch=settings.GetFeature<OculusTouchControllerProfile>();
        if(touch==null)throw new Exception("Oculus Touch profile missing");
        touch.enabled=true;settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        PlayerSettings.runInBackground=true;
        var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=player.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;player.ApplyModifiedPropertiesWithoutUndo();}
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);EditorUtility.SetDirty(all);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(touch);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Dome Study/PC VR/Build Windows EXE")]
    public static void BuildWindows()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play before building.");
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))throw new Exception("Install Windows Build Support in Unity Hub, or build on the Windows desktop.");
        Setup();Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/VRDome.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("[DomeQuest] Windows build succeeded. Copy the entire Builds/Windows folder to the presentation PC.");
    }
}
