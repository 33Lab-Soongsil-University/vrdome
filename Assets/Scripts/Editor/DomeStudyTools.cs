using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class DomeStudyTools
{
    [MenuItem("Tools/Dome Study/Preview Effects in Play Mode")]
    public static void PreviewEffects()
    {
        var effects = Object.FindFirstObjectByType<DomeTimedEffects>();
        if (!Application.isPlaying || effects == null) { Debug.LogWarning("Enter Play Mode with effect presets configured."); return; }
        effects.PreviewAllEffects();
    }

    [MenuItem("Tools/Dome Study/Configure Crop and Effect Presets")]
    public static void ConfigureEffects()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var dome = Object.FindFirstObjectByType<DomeVideoSetup>();
        if (dome == null) { Debug.LogError("Select a scene with DomeVideoSetup."); return; }
        Undo.RecordObject(dome, "Configure crop");
        dome.compositeShader = Shader.Find("DomeStudy/VideoComposite");
        var effects = dome.GetComponent<DomeTimedEffects>();
        if (effects == null) effects = Undo.AddComponent<DomeTimedEffects>(dome.gameObject);
        Undo.RecordObject(effects, "Configure effects");
        effects.playback = dome;
        effects.particleShader = Shader.Find("DomeStudy/Particles");
        if (effects.cues == null || effects.cues.Length == 0)
            effects.cues = new[] {
                new DomeTimedEffects.Cue { label = "Firework - timing unverified", kind = DomeTimedEffects.EffectKind.Firework, localPosition = new Vector3(-3,2,7), count = 120, speed = 1.5f },
                new DomeTimedEffects.Cue { label = "Dust - timing unverified", kind = DomeTimedEffects.EffectKind.Dust, localPosition = new Vector3(0,-2,6), color = new Color(.55f,.42f,.25f,.35f), count = 40, size = .4f, speed = .2f },
                new DomeTimedEffects.Cue { label = "Leaves - timing unverified", kind = DomeTimedEffects.EffectKind.Leaves, localPosition = new Vector3(3,-1,7), color = new Color(.9f,.3f,.06f,1), count = 20, size = .12f, speed = .4f }
            };
        EditorUtility.SetDirty(dome);
        EditorUtility.SetDirty(effects);
        EditorSceneManager.MarkSceneDirty(dome.gameObject.scene);
        Selection.activeGameObject = dome.gameObject;
    }

    [MenuItem("Tools/Dome Study/Create Front Preview Copy")]
    public static void CreatePreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.isDirty) { Debug.LogError("Save the current scene before creating a preview copy."); return; }
        var dome = Object.FindFirstObjectByType<DomeVideoSetup>();
        if (dome == null) { Debug.LogError("Existing DomeVideoSetup not found; no objects created."); return; }
        string path = AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/dome_front_preview.unity");
        if (!EditorSceneManager.SaveScene(scene, path, true)) return;
        EditorSceneManager.OpenScene(path);
        dome = Object.FindFirstObjectByType<DomeVideoSetup>();
        Undo.RecordObject(dome.transform, "Front dome preview");
        dome.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        dome.transform.localScale = Vector3.one;
        dome.compositeShader = Shader.Find("DomeStudy/VideoComposite");
        dome.GetComponent<DomePreview>().BuildDomePreview();
        var camera = Camera.main;
        if (camera != null)
        {
            Undo.RecordObjects(new Object[] { camera, camera.transform }, "Preview camera");
            camera.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            camera.fieldOfView = 75;
            camera.farClipPlane = Mathf.Max(100, dome.GetComponent<DomePreview>().radius * 3);
        }
        foreach (var volume in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!volume.isGlobal) continue;
            Undo.RecordObject(volume, "Disable preview grading");
            volume.enabled = false;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Selection.activeGameObject = dome.gameObject;
        Debug.Log("[DomeStudy] Created " + path + "; original scene preserved.");
    }
}
