using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName="Dome/Video extension preset")]
public class DomeClipPreset : ScriptableObject
{
    public string displayName;
    [TextArea] public string observation;
    public string[] sourceFiles;
    public VideoClip mainClip, backgroundClip;
    public Texture2D backgroundImage;
    [Tooltip("Background image crop: x, y, width, height (bottom-left origin)")]
    public Vector4 backgroundUV = new Vector4(0,0,1,1);
    public Rect crop = new Rect(.08f,.08f,.84f,.84f);
    public float width = 86, height = 44, elevation = 10, blend = .18f, backgroundBrightness = .7f;
    public bool cleanBeachSky;
    public bool useSourceEdges;
    public DomeAmbientExtension.Cue[] cues = new DomeAmbientExtension.Cue[0];
    public DomeFireworksExtension.Burst[] fireworks = new DomeFireworksExtension.Burst[0];
}
