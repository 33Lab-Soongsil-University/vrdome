using System;
using UnityEngine;

// Original-video UVs locate each event. Sparks extend onto the surrounding dome.
// Evaluated from absolute video time: pause, restart and frame skips stay deterministic.
[RequireComponent(typeof(DomeVideoSetup), typeof(DomePreview))]
public class DomeFireworksExtension : MonoBehaviour
{
    [Serializable] public class Burst
    {
        public string label;
        public float startSeconds;
        public Vector2 sourceUV;
        public Color color = Color.yellow;
        public float duration = 2.8f;
        public float spreadDegrees = 23;
        public int seed = 42;
    }
    public Burst[] bursts = new Burst[0];
    [Range(16,120)] public int rays = 64;
    [Range(3,16)] public int trailSamples = 10;
    [Range(.02f,.5f)] public float sparkSize = .18f;
    [Range(0,1)] public float intensity = .9f;
    public Shader sparkShader;
    public int VisibleParticles { get; private set; }
    DomeVideoSetup video;
    DomePreview dome;
    ParticleSystem sparks;
    Material material;
    ParticleSystem.Particle[] particles;
    void OnEnable()
    {
        video = GetComponent<DomeVideoSetup>(); dome = GetComponent<DomePreview>();
        video.ClockSample += Sample;
    }
    bool Build()
    {
        if (sparks != null) return true;
        var shader = sparkShader != null ? sparkShader : Shader.Find("DomeStudy/FireworkSparks");
        if (shader == null) return false;
        var go = new GameObject("Firework extension - video clock");
        go.transform.SetParent(transform, false);
        sparks = go.AddComponent<ParticleSystem>();
        sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = sparks.main; main.playOnAwake = false; main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 20000;
        var emission = sparks.emission; emission.enabled = false;
        var shape = sparks.shape; shape.enabled = false;
        material = new Material(shader);
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        particles = new ParticleSystem.Particle[20000];
        return true;
    }
    static float Noise(int i, int seed)
    {
        uint n = (uint)(i * 374761393 + seed * 668265263);
        n = (n ^ (n >> 13)) * 1274126177u;
        return (n & 65535) / 65535f;
    }
    void Sample(double time, bool playing)
    {
        bool valid = video.State == DomeVideoSetup.PlaybackState.Playing || video.State == DomeVideoSetup.PlaybackState.Paused || video.State == DomeVideoSetup.PlaybackState.Ended;
        if (!valid) { if (sparks != null) sparks.Clear(); VisibleParticles = 0; return; }
        if (!Build()) return;
        int count = 0;
        Rect crop = video.mainSourceCrop;
        float w = Mathf.Clamp(video.mainWidthDegrees, .01f, dome.horizontalAngle);
        float h = Mathf.Clamp(video.mainHeightDegrees, .01f, dome.verticalAngle);
        float cx = Mathf.Clamp(video.mainAzimuthDegrees, -(dome.horizontalAngle-w)/2, (dome.horizontalAngle-w)/2);
        float cy = Mathf.Clamp(video.mainElevationDegrees, dome.elevationCenterDegrees-(dome.verticalAngle-h)/2, dome.elevationCenterDegrees+(dome.verticalAngle-h)/2);
        foreach (var burst in bursts)
        {
            if (burst == null) continue;
            float age = (float)time - burst.startSeconds;
            if (age < 0 || age > burst.duration) continue;
            float az = cx + ((burst.sourceUV.x-crop.x)/Mathf.Max(.001f,crop.width)-.5f)*w;
            float el = cy + ((burst.sourceUV.y-crop.y)/Mathf.Max(.001f,crop.height)-.5f)*h;
            for (int ray = 0; ray < rays; ray++)
            {
                float angle = (ray + Noise(ray,burst.seed)*.8f) / rays * Mathf.PI*2;
                float speed = Mathf.Lerp(.65f,1,Noise(ray+200,burst.seed));
                for (int tail = 0; tail < trailSamples; tail++)
                {
                    float t = age - tail*.025f;
                    if (t < 0 || count >= particles.Length) continue;
                    float radius = burst.spreadDegrees * speed * (1-Mathf.Exp(-t*1.15f));
                    float x = az + Mathf.Cos(angle)*radius;
                    float y = el + Mathf.Sin(angle)*radius - 1.4f*t*t;
                    // Preserve the original event inside main; reveal its continuation at the edge.
                    float outside = Mathf.Max(Mathf.Abs(x-cx)-w*.5f, Mathf.Abs(y-cy)-h*.5f);
                    float edge = Mathf.SmoothStep(0,1,Mathf.InverseLerp(-3,2,outside));
                    float alpha = Mathf.Clamp01((burst.duration-age)/.9f) * edge * intensity * Mathf.Pow(1-(float)tail/trailSamples,1.5f);
                    if (alpha < .008f || y > 88 || y < dome.elevationCenterDegrees-dome.verticalAngle*.5f || Mathf.Abs(x)>dome.horizontalAngle*.5f) continue;
                    float a=x*Mathf.Deg2Rad, e=y*Mathf.Deg2Rad;
                    var p = new ParticleSystem.Particle();
                    p.position = (dome.radius-.6f)*new Vector3(Mathf.Cos(e)*Mathf.Sin(a),Mathf.Sin(e),Mathf.Cos(e)*Mathf.Cos(a));
                    Color color = Color.Lerp(burst.color, new Color(1,.3f,.07f), Mathf.Clamp01(age/burst.duration)*.45f);
                    color.a = alpha; p.startColor = color;
                    p.startSize = sparkSize*(tail==0 ? 1.4f : .75f);
                    p.startLifetime = p.remainingLifetime = 10;
                    particles[count++] = p;
                }
            }
        }
        sparks.SetParticles(particles,count); sparks.Pause(); VisibleParticles = count;
    }
    void OnDisable()
    {
        if (video != null) video.ClockSample -= Sample;
        if (sparks != null) Destroy(sparks.gameObject);
        if (material != null) Destroy(material);
        sparks = null; material = null; VisibleParticles = 0;
    }
}
