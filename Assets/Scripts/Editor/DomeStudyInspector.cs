using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DomeVideoSetup))]
public class DomeVideoSetupInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Source Crop: normalized original-video coordinates (bottom left). Edge Blend: fraction of displayed main width/height. This angular mapping does not preserve perspective or automatically match background geometry.", MessageType.Info);
        if (GUILayout.Button("Reset source crop to full image"))
        {
            Undo.RecordObject(target, "Reset crop");
            ((DomeVideoSetup)target).mainSourceCrop = new Rect(0, 0, 1, 1);
            EditorUtility.SetDirty(target);
        }
    }
}
[CustomEditor(typeof(DomeTimedEffects))]
public class DomeTimedEffectsInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Enable cues only after checking their times against main footage. Positions are local 3D coordinates inside the dome. Change particle parameters outside Play Mode; enter Play again to rebuild.", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
            if (GUILayout.Button("Preview all effects (NOT synchronized)"))
                ((DomeTimedEffects)target).PreviewAllEffects();
    }
}
