using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

public static class Nov11SuiteBuilder
{
    [Serializable] class Manifest {public Entry[] entries;}
    [Serializable] class Entry {public string id,mainPath,backgroundPath,backgroundImagePath;public PresetData preset;}
    [Serializable] class Crop {public float x,y,width,height;}
    [Serializable] class PresetData {
        public string displayName,observation;public string[] sourceFiles;
        public Vector4 backgroundUV = new Vector4(0,0,1,1);
        public Crop crop; public float width,height,elevation,blend,backgroundBrightness;
        public bool cleanBeachSky,useSourceEdges;
        public DomeAmbientExtension.Cue[] cues;public DomeFireworksExtension.Burst[] fireworks;
    }
    const string Scene="Assets/Scenes/nov11_video_suite.unity";
    [InitializeOnLoadMethod] static void Request()
    {
        if(!File.Exists("/tmp/dome-nov11-suite.request"))return;
        File.Delete("/tmp/dome-nov11-suite.request");EditorApplication.delayCall+=Build;
    }
    [MenuItem("Tools/Dome Study/Build November 11 Video Suite")]
    public static void Build()
    {
        SessionState.SetBool("Nov11Verify",false);
        if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged+=AfterStop;EditorApplication.isPlaying=false;return;}
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if(!File.Exists(Scene)) {var source=EditorSceneManager.OpenScene("Assets/Scenes/sphere_theater.unity");EditorSceneManager.SaveScene(source,Scene,true);}
        var scene=EditorSceneManager.OpenScene(Scene);
        Directory.CreateDirectory("Assets/DomeClipPresets");
        var data=JsonUtility.FromJson<Manifest>(File.ReadAllText("Assets/DomeClipPresets/suite.json"));
        var presets=new DomeClipPreset[data.entries.Length];
        for(int i=0;i<data.entries.Length;i++)
        {
            var e=data.entries[i];string path="Assets/DomeClipPresets/"+e.id+".asset";
            var p=AssetDatabase.LoadAssetAtPath<DomeClipPreset>(path);
            if(p==null){p=ScriptableObject.CreateInstance<DomeClipPreset>();AssetDatabase.CreateAsset(p,path);}
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(e.preset),p);
            p.crop = new Rect(e.preset.crop.x,e.preset.crop.y,e.preset.crop.width,e.preset.crop.height);
            if(p.crop.width<=0||p.crop.height<=0)throw new Exception("Invalid crop "+e.id);
            p.mainClip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/November11/"+Path.GetFileName(e.mainPath));
            p.backgroundImage=null;
            if(!string.IsNullOrEmpty(e.backgroundImagePath))
            {
                string imagePath="Assets/Art/DomeBackgrounds/"+Path.GetFileName(e.backgroundImagePath);
                var importer=AssetImporter.GetAtPath(imagePath) as TextureImporter;
                if(importer!=null && (importer.wrapMode!=TextureWrapMode.Clamp || importer.maxTextureSize!=4096 || importer.textureCompression!=TextureImporterCompression.Uncompressed))
                {
                    importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;
                    importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=true;importer.SaveAndReimport();
                }
                p.backgroundImage=AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
                if(p.backgroundImage==null)throw new Exception("Missing background image "+imagePath);
            }
            p.backgroundClip=null;
            if(!string.IsNullOrEmpty(e.backgroundPath))p.backgroundClip=AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/Video/November11/"+Path.GetFileName(e.backgroundPath));
            if(p.mainClip==null)throw new Exception("Missing main "+e.mainPath);
            if(!p.useSourceEdges&&p.backgroundClip==null&&p.backgroundImage==null)throw new Exception("Missing background "+e.backgroundPath);
            EditorUtility.SetDirty(p);presets[i]=p;
        }
        var video=UnityEngine.Object.FindAnyObjectByType<DomeVideoSetup>();video.autoPlay=false;
        var old=video.GetComponent<DomeTimedEffects>();if(old!=null)old.enabled=false;
        var fire=video.GetComponent<DomeFireworksExtension>();if(fire==null)fire=video.gameObject.AddComponent<DomeFireworksExtension>();
        fire.sparkShader=Shader.Find("DomeStudy/FireworkSparks");fire.sparkSize=.38f;fire.trailSamples=14;fire.enabled=false;
        var ambient=video.GetComponent<DomeAmbientExtension>();if(ambient==null)ambient=video.gameObject.AddComponent<DomeAmbientExtension>();
        ambient.extensionShader=Shader.Find("DomeStudy/SceneParticles");ambient.enabled=false;
        var library=video.GetComponent<DomeClipLibrary>();if(library==null)library=video.gameObject.AddComponent<DomeClipLibrary>();
        library.playback=video;library.presets=presets;library.current=0;
        var view=UnityEngine.Object.FindAnyObjectByType<SphereTheaterView>();if(view!=null)view.currentSeat=0;
        EditorUtility.SetDirty(video);EditorUtility.SetDirty(library);EditorUtility.SetDirty(fire);EditorUtility.SetDirty(ambient);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText("/tmp/dome-suite-built.txt","Built "+presets.Length+" presets in "+Scene);
        Debug.Log("[DomeSuite] Built "+presets.Length+" presets; 16 source files including duplicate aliases.");
    }
    static void AfterStop(PlayModeStateChange s){if(s!=PlayModeStateChange.EnteredEditMode)return;EditorApplication.playModeStateChanged-=AfterStop;EditorApplication.delayCall+=Build;}
}
