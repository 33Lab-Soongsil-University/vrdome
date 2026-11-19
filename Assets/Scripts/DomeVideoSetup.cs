using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;

// Desktop preview: independent decoders, not frame-locked playback.
[RequireComponent(typeof(DomePreview))]
public class DomeVideoSetup : MonoBehaviour
{
    public VideoClip mainClip, backgroundClip;
    public Texture2D backgroundImage;
    public Vector4 backgroundUV = new Vector4(0,0,1,1);
    public Shader compositeShader;
    [Range(10, 150)] public float mainWidthDegrees = 70;
    [Range(10, 120)] public float mainHeightDegrees = 40;
    [Range(-50, 50)] public float mainElevationDegrees;
    [Range(0, 1)] public float backgroundBrightness = 1;
    [Tooltip("Use a clean left-hand sky strip above the horizon; temporary cleanup for this beach BG.")]
    public bool cleanBackgroundSky;
    public bool sourceEdgeBackground;
    [Header("Main source crop (normalized, origin: bottom left)")]
    public Rect mainSourceCrop = new Rect(.1f, .1f, .8f, .8f);
    [Range(-90, 90)] public float mainAzimuthDegrees;
    [Range(0, .49f)] public float edgeBlend = .08f;
    public bool autoPlay = true;
    public bool showControls = true;
    public enum PlaybackState { Stopped, Preparing, Playing, Paused, Ended, Failed }
    public PlaybackState State { get; private set; }
    public double MainTime => mainPlayer != null && mainPlayer.isPrepared ? mainPlayer.time : 0;
    public string LastError { get; private set; }
    // Effects should evaluate their state from this absolute clock (including backwards jumps).
    public event Action<double, bool> ClockSample;
    private VideoPlayer mainPlayer, bgPlayer;
    private RenderTexture mainRT, bgRT;
    private Material composite, originalMaterial;
    private GameObject players;

    private void OnEnable() { if (autoPlay) RestartPlayback(); }
    private void OnDisable() { StopPlayback(); }
    public void RestartPlayback()
    {
        if (!isActiveAndEnabled || !Application.isPlaying) return;
        StopPlayback();
        LastError = null;
        StartCoroutine(PreparePlayback());
    }
    public void StopPlayback()
    {
        StopAllCoroutines();
        Cleanup();
        State = PlaybackState.Stopped;
        ClockSample?.Invoke(0, false);
    }
    public void PausePlayback()
    {
        if (State != PlaybackState.Playing) return;
        mainPlayer.Pause();
        if (bgPlayer != null) bgPlayer.Pause();
        State = PlaybackState.Paused;
        ClockSample?.Invoke(MainTime, false);
    }
    public void ResumePlayback()
    {
        if (State == PlaybackState.Stopped || State == PlaybackState.Ended || State == PlaybackState.Failed)
        { RestartPlayback(); return; }
        if (State != PlaybackState.Paused) return;
        if (bgPlayer != null && bgPlayer.time < bgPlayer.length - 0.05) bgPlayer.Play();
        mainPlayer.Play();
        State = PlaybackState.Playing;
    }
    private IEnumerator PreparePlayback()
    {
        State = PlaybackState.Preparing;
        if (mainClip == null) { Fail("Assign Main Clip in Dome Video Setup."); yield break; }
        var shader = compositeShader != null ? compositeShader : Shader.Find("DomeStudy/VideoComposite");
        if (shader == null || !shader.isSupported) { Fail("Assign a supported DomeVideoComposite shader."); yield break; }
        GetComponent<DomePreview>().BuildDomePreview();
        originalMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        composite = new Material(shader);
        mainRT = CreateTarget(mainClip);
        composite.SetTexture("_MainVideo", mainRT);
        composite.SetTexture("_BGVideo", backgroundImage != null ? (Texture)backgroundImage :
            sourceEdgeBackground ? (Texture)mainRT : Texture2D.blackTexture);
        GetComponent<MeshRenderer>().sharedMaterial = composite;
        UpdateMaterial();
        players = new GameObject("VideoPlayers_Runtime");
        players.transform.SetParent(transform, false);
        mainPlayer = CreatePlayer(mainClip, mainRT);
        mainPlayer.loopPointReached += OnMainEnded;
        if (backgroundClip != null && backgroundImage == null)
        {
            bgRT = CreateTarget(backgroundClip);
            composite.SetTexture("_BGVideo", bgRT);
            bgPlayer = CreatePlayer(backgroundClip, bgRT);
            bgPlayer.loopPointReached += OnBackgroundEnded;
        }
        mainPlayer.Prepare();
        if (bgPlayer != null) bgPlayer.Prepare();
        float deadline = Time.realtimeSinceStartup + 60;
        while (State != PlaybackState.Failed && (!mainPlayer.isPrepared || (bgPlayer != null && !bgPlayer.isPrepared)))
        {
            if (Time.realtimeSinceStartup > deadline) { Fail("Video preparation timed out (60s). Check clip import/codec."); yield break; }
            yield return null;
        }
        if (State == PlaybackState.Failed) yield break;
        if (bgPlayer != null) bgPlayer.Play();
        mainPlayer.Play();
        State = PlaybackState.Playing;
    }
    private void Update()
    {
        if (composite != null) UpdateMaterial();
        if (State == PlaybackState.Failed && players != null) Cleanup();
        ClockSample?.Invoke(MainTime, State == PlaybackState.Playing);
    }
    private void UpdateMaterial()
    {
        var dome = GetComponent<DomePreview>();
        float w = Mathf.Clamp(mainWidthDegrees / Mathf.Max(1, dome.horizontalAngle), .01f, 1);
        float h = Mathf.Clamp(mainHeightDegrees / Mathf.Max(1, dome.verticalAngle), .01f, 1);
        float y = Mathf.Clamp(.5f + (mainElevationDegrees - dome.elevationCenterDegrees) / Mathf.Max(1, dome.verticalAngle) - h / 2, 0, 1 - h);
        float x = Mathf.Clamp(.5f + mainAzimuthDegrees / Mathf.Max(1, dome.horizontalAngle) - w / 2, 0, 1 - w);
        composite.SetVector("_MainRect", new Vector4(x, y, w, h));
        float cx = Mathf.Clamp(mainSourceCrop.x, 0, .999f);
        float cy = Mathf.Clamp(mainSourceCrop.y, 0, .999f);
        composite.SetVector("_SourceCrop", new Vector4(cx, cy,
            Mathf.Clamp(mainSourceCrop.width, .001f, 1 - cx),
            Mathf.Clamp(mainSourceCrop.height, .001f, 1 - cy)));
        composite.SetFloat("_EdgeBlend", Mathf.Clamp(edgeBlend, 0, .49f));
        composite.SetFloat("_BGStrength", backgroundBrightness);
        composite.SetVector("_BGRect", backgroundUV);
        composite.SetFloat("_CleanSky", cleanBackgroundSky ? 1 : 0);
        composite.SetFloat("_SourceEdges", sourceEdgeBackground ? 1 : 0);
    }
    private VideoPlayer CreatePlayer(VideoClip clip, RenderTexture target)
    {
        var p = players.AddComponent<VideoPlayer>();
        p.playOnAwake = false;
        p.source = VideoSource.VideoClip;
        p.clip = clip;
        p.renderMode = VideoRenderMode.RenderTexture;
        p.targetTexture = target;
        p.audioOutputMode = VideoAudioOutputMode.None;
        p.isLooping = false;
        p.waitForFirstFrame = true;
        p.errorReceived += OnError;
        return p;
    }
    private void OnError(VideoPlayer p, string message) { Fail($"{p.clip?.name}: {message}"); }
    private void Fail(string message)
    {
        LastError = message;
        State = PlaybackState.Failed;
        if (mainPlayer != null) mainPlayer.Pause();
        if (bgPlayer != null) bgPlayer.Pause();
        Debug.LogError("[DomeVideo] " + message, this);
    }
    private void OnMainEnded(VideoPlayer p)
    {
        p.Pause();
        if (bgPlayer != null) bgPlayer.Pause();
        State = PlaybackState.Ended;
    }
    // Shorter backgrounds hold their last decoded frame; longer ones stop with main.
    private void OnBackgroundEnded(VideoPlayer p) { p.Pause(); }
    private static RenderTexture CreateTarget(VideoClip clip)
    {
        float scale = Mathf.Min(1, 1920f / Mathf.Max(1, clip.width), 1920f / Mathf.Max(1, clip.height));
        var rt = new RenderTexture(Mathf.Max(2, Mathf.RoundToInt(clip.width * scale)),
            Mathf.Max(2, Mathf.RoundToInt(clip.height * scale)), 0, RenderTextureFormat.ARGB32);
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.Create();
        var previous = RenderTexture.active;
        RenderTexture.active = rt;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
        return rt;
    }
    private void Cleanup()
    {
        if (mainPlayer != null) { mainPlayer.errorReceived -= OnError; mainPlayer.loopPointReached -= OnMainEnded; mainPlayer.Stop(); }
        if (bgPlayer != null) { bgPlayer.errorReceived -= OnError; bgPlayer.loopPointReached -= OnBackgroundEnded; bgPlayer.Stop(); }
        if (players != null) { players.SetActive(false); Destroy(players); }
        var renderer = GetComponent<MeshRenderer>();
        if (composite != null && renderer.sharedMaterial == composite) renderer.sharedMaterial = originalMaterial;
        if (mainRT != null) { mainRT.Release(); Destroy(mainRT); }
        if (bgRT != null) { bgRT.Release(); Destroy(bgRT); }
        if (composite != null) Destroy(composite);
        mainPlayer = bgPlayer = null;
        mainRT = bgRT = null;
        players = null;
        composite = originalMaterial = null;
    }
    private void OnGUI()
    {
        if (!showControls) return;
        GUILayout.BeginArea(new Rect(12, 12, 460, 110), GUI.skin.box);
        GUILayout.Label($"{State} | Main {MainTime:F2}s | BG: hold last frame");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Play / Resume")) ResumePlayback();
        if (GUILayout.Button("Pause")) PausePlayback();
        if (GUILayout.Button("Stop")) StopPlayback();
        if (GUILayout.Button("Restart")) RestartPlayback();
        GUILayout.EndHorizontal();
        if (!string.IsNullOrEmpty(LastError)) GUILayout.Label(LastError);
        GUILayout.EndArea();
    }
}
