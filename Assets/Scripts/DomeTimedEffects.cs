using System;
using System.Collections;
using UnityEngine;

// Effects occupy real 3D space inside the dome; seats therefore see parallax.
public class DomeTimedEffects : MonoBehaviour
{
    public enum EffectKind { Firework, Dust, Leaves }
    [Serializable]
    public class Cue
    {
        public string label = "Unverified timing";
        public bool enabled;
        public EffectKind kind;
        [Min(0)] public float startSeconds;
        [Min(.01f)] public float durationSeconds = 3;
        public Vector3 localPosition = new Vector3(0, 2, 7);
        public Color color = new Color(1, .6f, .15f, 1);
        [Range(1, 500)] public int count = 80;
        [Min(.01f)] public float size = .08f;
        [Min(0)] public float speed = 1.5f;
        public int seed = 42;
        [NonSerialized] public ParticleSystem particles;
        [NonSerialized] public float lastTime = -1;
    }
    public DomeVideoSetup playback;
    public Shader particleShader;
    public Cue[] cues = new Cue[0];
    private GameObject runtimeRoot;
    private Material material;
    private bool previewing;
    private void OnEnable()
    {
        if (playback == null) playback = GetComponent<DomeVideoSetup>();
        if (playback != null) playback.ClockSample += Evaluate;
    }
    private bool Build()
    {
        if (runtimeRoot != null) return true;
        var shader = particleShader != null ? particleShader : Shader.Find("DomeStudy/Particles");
        if (shader == null || !shader.isSupported) { Debug.LogError("Assign DomeParticles.shader.", this); return false; }
        material = new Material(shader);
        runtimeRoot = new GameObject("DomeEffects_Runtime");
        runtimeRoot.transform.SetParent(transform, false);
        foreach (var cue in cues)
        {
            if (cue == null) continue;
            var go = new GameObject(cue.label);
            go.transform.SetParent(runtimeRoot.transform, false);
            go.transform.localPosition = cue.localPosition;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = Mathf.Max(.01f, cue.durationSeconds);
            main.startLifetime = Mathf.Max(.01f, cue.durationSeconds);
            main.startSpeed = cue.speed;
            main.startSize = cue.size;
            main.startColor = cue.color;
            main.maxParticles = Mathf.Clamp(cue.count, 1, 500);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.gravityModifier = cue.kind == EffectKind.Firework ? .15f : cue.kind == EffectKind.Leaves ? .06f : -.005f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)Mathf.Clamp(cue.count, 1, 500)) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = cue.kind == EffectKind.Firework ? .01f : .4f;
            var colors = ps.colorOverLifetime;
            colors.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .08f), new GradientAlphaKey(0, 1) });
            colors.color = gradient;
            var rotation = ps.rotationOverLifetime;
            rotation.enabled = cue.kind == EffectKind.Leaves;
            rotation.z = 2;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)Mathf.Max(1, cue.seed);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var properties = new MaterialPropertyBlock();
            properties.SetFloat("_Leaf", cue.kind == EffectKind.Leaves ? 1 : 0);
            renderer.SetPropertyBlock(properties);
            cue.particles = ps;
            cue.lastTime = -1;
        }
        return true;
    }
    private void Evaluate(double time, bool playing)
    {
        if (previewing) return;
        bool valid = playback != null && (playback.State == DomeVideoSetup.PlaybackState.Playing || playback.State == DomeVideoSetup.PlaybackState.Paused || playback.State == DomeVideoSetup.PlaybackState.Ended);
        Sample(time, valid, false);
    }
    private void Sample(double time, bool valid, bool preview)
    {
        bool needed = false;
        foreach (var cue in cues) if (cue != null && (preview || cue.enabled)) needed = true;
        if (!needed || !Build()) return;
        foreach (var cue in cues)
        {
            if (cue == null || cue.particles == null) continue;
            float age = (float)time - (preview ? 0 : cue.startSeconds);
            bool active = valid && (preview || cue.enabled) && age >= 0 && age < cue.durationSeconds;
            var ps = cue.particles;
            if (!active)
            {
                if (cue.lastTime >= 0) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                cue.lastTime = -1;
                continue;
            }
            // Restart simulation on backwards jumps; incremental updates during ordinary playback.
            bool reset = cue.lastTime < 0 || age < cue.lastTime || age - cue.lastTime > .25f;
            float step = reset ? age : age - cue.lastTime;
            if (reset || step > 0) ps.Simulate(step, true, reset, false);
            ps.Pause(true);
            cue.lastTime = age;
        }
    }
    [ContextMenu("Preview All Effects (Play Mode, unsynchronized)")]
    public void PreviewAllEffects()
    {
        if (!Application.isPlaying || !isActiveAndEnabled) return;
        StopAllCoroutines();
        StartCoroutine(Preview());
    }
    private IEnumerator Preview()
    {
        previewing = true;
        float duration = 0;
        foreach (var cue in cues) if (cue != null) duration = Mathf.Max(duration, cue.durationSeconds);
        double start = Time.realtimeSinceStartupAsDouble;
        while (Time.realtimeSinceStartupAsDouble - start < duration)
        {
            Sample(Time.realtimeSinceStartupAsDouble - start, true, true);
            yield return null;
        }
        Sample(0, false, true);
        previewing = false;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        previewing = false;
        if (playback != null) playback.ClockSample -= Evaluate;
        if (runtimeRoot != null) { runtimeRoot.SetActive(false); Destroy(runtimeRoot); }
        if (material != null) Destroy(material);
        runtimeRoot = null;
        material = null;
        foreach (var cue in cues) if (cue != null) { cue.particles = null; cue.lastTime = -1; }
    }
}
