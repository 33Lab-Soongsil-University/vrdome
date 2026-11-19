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
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
using Unity.XR.CoreUtils;

public static class DomeQuestBuilder
{
    public const string ScenePath="Assets/Scenes/quest3_dome.unity";
    [InitializeOnLoadMethod] static void Auto()
    {
        if(File.Exists("/tmp/dome-quest-setup.request")){File.Delete("/tmp/dome-quest-setup.request");EditorApplication.delayCall+=Setup;}
    }
    [MenuItem("Tools/Dome Study/Quest 3/Create or Update VR Scene")]
    public static void Setup()
    {
        if(EditorApplication.isPlaying){Debug.LogError("Stop Play before Quest setup.");return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        ConfigureAndroid();
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
        File.WriteAllText("/tmp/dome-quest-setup.txt","Quest scene configured. Android OpenXR + Touch profile, tracked camera, world-space menu.\n"+ScenePath);
        Debug.Log("[DomeQuest] VR scene configured: "+ScenePath);
    }
    public static void ConfigureAndroid()
    {
        Directory.CreateDirectory("Assets/XR/Settings");
        if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey,out XRGeneralSettingsPerBuildTarget all))
        {all=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(all,"Assets/XR/Settings/DomeXRGeneralSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey,all,true);}
        if(!all.HasSettingsForBuildTarget(BuildTargetGroup.Android))all.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if(!all.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))all.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var general=all.SettingsForBuildTarget(BuildTargetGroup.Android);general.InitManagerOnStart=true;
        if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Android))throw new Exception("OpenXR loader assignment failed");
        UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
        var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if(settings==null)throw new Exception("OpenXR settings missing");
        var quest=settings.GetFeature<MetaQuestFeature>();var touch=settings.GetFeature<OculusTouchControllerProfile>();
        if(quest==null||touch==null)throw new Exception("OpenXR feature list not ready; run setup again after package import");
        quest.enabled=true;touch.enabled=true;settings.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.vrdome.queststudy");
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.Vulkan});
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;
        // OpenXR uses Input System; IMGUI desktop controls continue to use Event.current.
        var player=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=player.FindProperty("activeInputHandler");if(input!=null){input.intValue=1;player.ApplyModifiedPropertiesWithoutUndo();}
        EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);EditorUtility.SetDirty(all);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(quest);EditorUtility.SetDirty(touch);
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/Dome Study/Quest 3/Build APK")]
    public static void BuildAPK()
    {
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new Exception("Android Build Support is not installed or editor restart is required.");
        Setup();Directory.CreateDirectory("Builds/Quest3");
        var rig=UnityEngine.Object.FindAnyObjectByType<DomeQuestViewer>();
        if(rig==null||Vector3.Angle(rig.transform.up,Vector3.up)>.01f)throw new Exception("XR origin must be upright");
        var issues=new System.Collections.Generic.List<UnityEngine.XR.OpenXR.Features.OpenXRFeature.ValidationRule>();
        UnityEditor.XR.OpenXR.OpenXRProjectValidation.GetCurrentValidationIssues(issues,BuildTargetGroup.Android);
        var validation=new System.Text.StringBuilder("XR origin upright; tracked camera and Quest scene saved.\n");
        foreach(var issue in issues){validation.AppendLine((issue.error?"ERROR: ":"WARN: ")+issue.message);if(issue.error)throw new Exception(issue.message);}
        File.WriteAllText("/tmp/dome-quest-validation.txt",validation.ToString());
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Quest3/VRDome-Quest3.apk",target=BuildTarget.Android,options=BuildOptions.None});
        File.WriteAllText("/tmp/dome-quest-build.txt",report.summary.result+"\n"+report.summary.totalErrors+" errors\n"+report.summary.outputPath);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Quest APK build failed: "+report.summary.result);
    }
}
