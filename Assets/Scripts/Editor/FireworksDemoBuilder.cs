using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

public static class FireworksDemoBuilder
{
    [InitializeOnLoadMethod]
    static void RequestedSetup()
    {
        if (!File.Exists("/tmp/dome-create-fireworks.request")) return;
        File.Delete("/tmp/dome-create-fireworks.request");
        EditorApplication.delayCall += Create;
    }
    const string ScenePath = "Assets/Scenes/fireworks_extension.unity";
    [MenuItem("Tools/Dome Study/Create Fireworks Extension Demo")]
    public static void Create()
    {
        if (EditorApplication.isPlaying) { EditorApplication.playModeStateChanged += AfterStop; EditorApplication.isPlaying = false; return; }
        
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!File.Exists(ScenePath)) {
            var original = EditorSceneManager.OpenScene("Assets/Scenes/sphere_theater.unity");
            EditorSceneManager.SaveScene(original, ScenePath, true);
        }
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var video = Object.FindAnyObjectByType<DomeVideoSetup>();
        video.mainClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/FireworksDemo/379763_medium.mp4");
        video.backgroundClip = AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/FireworksDemo/beach_fireworks_v2_bg_01.mp4");
        video.mainSourceCrop = new Rect(.08f,.05f,.84f,.64f);
        video.mainWidthDegrees = 86; video.mainHeightDegrees = 39;
        video.mainElevationDegrees = 7; video.mainAzimuthDegrees = 0;
        video.edgeBlend = .22f; video.backgroundBrightness = .55f; video.cleanBackgroundSky = true;
        video.autoPlay = true; video.showControls = false;
        var old = video.GetComponent<DomeTimedEffects>(); if (old != null) old.enabled = false;
        var effects = video.GetComponent<DomeFireworksExtension>();
        if (effects == null) effects = video.gameObject.AddComponent<DomeFireworksExtension>();
        effects.sparkSize = .38f; effects.trailSamples = 14;
        effects.sparkShader = Shader.Find("DomeStudy/FireworkSparks");
        effects.bursts = new[] {
            B("Opening left (already expanded in frame 0)",-.65f,.40f,.82f,new Color(1,.45f,.45f),2.9f,23,41),
            B("Opening right (already expanded in frame 0)",-.55f,.61f,.82f,new Color(.8f,.88f,1),2.7f,22,73),
            B("Left white/gold flash ~1.5s",1.50f,.40f,.76f,new Color(1,.86f,.55f),3,26,101),
            B("Right pink/gold burst ~2.3s",2.30f,.63f,.78f,new Color(1,.57f,.75f),2.8f,24,137),
            B("Right white burst ~4.4s",4.40f,.65f,.76f,new Color(.8f,.9f,1),2.8f,25,193)
        };
        EditorUtility.SetDirty(video); EditorUtility.SetDirty(effects);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = video.gameObject;
        Debug.Log("[FireworksDemo] Created scene with cropped main and video-clock extension. Background uses a clean sky strip from original beach footage pending clean regeneration.");
    }
    static void AfterStop(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= AfterStop;
        EditorApplication.delayCall += Create;
    }
    static DomeFireworksExtension.Burst B(string label,float t,float x,float y,Color c,float duration,float spread,int seed)
    { return new DomeFireworksExtension.Burst { label=label,startSeconds=t,sourceUV=new Vector2(x,y),color=c,duration=duration,spreadDegrees=spread,seed=seed }; }
}
