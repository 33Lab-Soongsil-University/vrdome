using System;
using UnityEngine;

// These are designed extensions, not automatically detected original objects.
// Birth times and motion are evaluated from the main video clock, including pause/restart.
[RequireComponent(typeof(DomeVideoSetup),typeof(DomePreview))]
public class DomeAmbientExtension : MonoBehaviour
{
    public enum Kind { Dust, Leaves, Water, Confetti, Bubbles, Cards, Light, Mist, Fireflies }
    [Serializable] public class Cue
    {
        public string label;
        public Kind kind;
        public float start, end=5, lifetime=3;
        public Vector2 sourceUV = new Vector2(.5f,.5f);
        public Vector2 velocity = new Vector2(0,10);
        public Vector2 spread = new Vector2(20,12);
        public float gravity = -3, rate = 25, size = .4f, opacity = .7f;
        public Color color = Color.white;
        public int seed=42;
        [NonSerialized] public ParticleSystem system;
    }
    public Cue[] cues = new Cue[0];
    public Shader extensionShader;
    public int VisibleParticles { get; private set; }
    DomeVideoSetup video; DomePreview dome;
    GameObject root;
    Material[] materials;
    ParticleSystem.Particle[] buffer = new ParticleSystem.Particle[2500];
    void OnEnable() { video=GetComponent<DomeVideoSetup>(); dome=GetComponent<DomePreview>(); video.ClockSample+=Sample; }
    static float N(int i,int seed) { uint x=(uint)(i*374761393+seed*668265263); x=(x^(x>>13))*1274126177u; return (x&65535)/65535f; }
    bool Build()
    {
        if(root!=null)return true;
        var shader=extensionShader!=null?extensionShader:Shader.Find("DomeStudy/SceneParticles");
        if(shader==null)return false;
        root=new GameObject("Scene extension - video clock");root.transform.SetParent(transform,false);
        materials=new Material[cues.Length];
        for(int c=0;c<cues.Length;c++)
        {
            var cue=cues[c];if(cue==null)continue;
            var go=new GameObject(cue.label);go.transform.SetParent(root.transform,false);
            var ps=go.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var m=ps.main;m.playOnAwake=false;m.simulationSpace=ParticleSystemSimulationSpace.Local;m.maxParticles=buffer.Length;
            var em=ps.emission;em.enabled=false;var shape=ps.shape;shape.enabled=false;
            var material=new Material(shader);material.SetFloat("_Kind",(float)cue.kind);materials[c]=material;
            var r=go.GetComponent<ParticleSystemRenderer>();r.sharedMaterial=material;r.renderMode=ParticleSystemRenderMode.Billboard;
            cue.system=ps;
        }
        return true;
    }
    void Sample(double time,bool playing)
    {
        VisibleParticles=0;
        bool valid=video.State==DomeVideoSetup.PlaybackState.Playing||video.State==DomeVideoSetup.PlaybackState.Paused||video.State==DomeVideoSetup.PlaybackState.Ended;
        if(!valid) { foreach(var c in cues)if(c!=null&&c.system!=null)c.system.Clear(); return; }
        if(!Build())return;
        float w=Mathf.Min(video.mainWidthDegrees,dome.horizontalAngle),h=Mathf.Min(video.mainHeightDegrees,dome.verticalAngle);
        float cx=Mathf.Clamp(video.mainAzimuthDegrees,-(dome.horizontalAngle-w)/2,(dome.horizontalAngle-w)/2);
        float cy=Mathf.Clamp(video.mainElevationDegrees,dome.elevationCenterDegrees-(dome.verticalAngle-h)/2,dome.elevationCenterDegrees+(dome.verticalAngle-h)/2);
        Rect crop=video.mainSourceCrop;
        foreach(var c in cues)
        {
            if(c==null||c.system==null)continue;
            int count=0;
            float rate=Mathf.Clamp(c.rate,.1f,200),life=Mathf.Max(.05f,c.lifetime);
            int first=Mathf.Max(0,Mathf.FloorToInt(((float)time-life-c.start)*rate));
            int last=Mathf.Min(Mathf.FloorToInt(((float)time-c.start)*rate),Mathf.CeilToInt((c.end-c.start)*rate)-1);
            float ax=cx+((c.sourceUV.x-crop.x)/Mathf.Max(.001f,crop.width)-.5f)*w;
            float ay=cy+((c.sourceUV.y-crop.y)/Mathf.Max(.001f,crop.height)-.5f)*h;
            for(int i=first;i<=last&&count<buffer.Length;i++)
            {
                float age=(float)time-c.start-i/rate;
                if(age<0||age>life)continue;
                float vx=c.velocity.x+(N(i,c.seed)-.5f)*2*c.spread.x;
                float vy=c.velocity.y+(N(i+300,c.seed)-.5f)*2*c.spread.y;
                float x=ax+vx*age, y=ay+vy*age+.5f*c.gravity*age*age;
                if(c.kind==Kind.Leaves||c.kind==Kind.Confetti||c.kind==Kind.Cards)x+=Mathf.Sin(age*4+N(i+500,c.seed)*6)*age*2;
                if(c.kind==Kind.Fireflies)x+=Mathf.Sin(age*2+i)*3;
                if(y<dome.elevationCenterDegrees-dome.verticalAngle*.5f||y>88||Mathf.Abs(x)>dome.horizontalAngle*.5f)continue;
                float edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-5,2,Mathf.Max(Mathf.Abs(x-cx)-w/2,Mathf.Abs(y-cy)-h/2)));
                float fade=Mathf.Min(Mathf.Clamp01(age/.15f),Mathf.Clamp01((life-age)/.6f));
                float alpha=edge*fade*c.opacity;
                if(alpha<.005f)continue;
                var p=new ParticleSystem.Particle();float az=x*Mathf.Deg2Rad,el=y*Mathf.Deg2Rad;
                p.position=(dome.radius-1.2f)*new Vector3(Mathf.Cos(el)*Mathf.Sin(az),Mathf.Sin(el),Mathf.Cos(el)*Mathf.Cos(az));
                Color col=c.color;
                if(c.kind==Kind.Confetti)col=Color.Lerp(c.color,new Color(.95f,.6f,.8f),N(i+700,c.seed));
                if(c.kind==Kind.Leaves)col=Color.Lerp(c.color,new Color(.55f,.18f,.025f),N(i+700,c.seed));
                col.a=alpha;p.startColor=col;
                float size=c.size*Mathf.Lerp(.6f,1.4f,N(i+800,c.seed));
                if(c.kind==Kind.Dust||c.kind==Kind.Mist)size*=1+age*.6f;
                p.startSize=size;p.rotation=(c.kind==Kind.Water||c.kind==Kind.Bubbles)?0:N(i+900,c.seed)*360+age*110;
                p.startLifetime=p.remainingLifetime=10;buffer[count++]=p;
            }
            c.system.SetParticles(buffer,count);c.system.Pause();VisibleParticles+=count;
        }
    }
    void OnDisable()
    {
        if(video!=null)video.ClockSample-=Sample;
        if(root!=null)Destroy(root);root=null;
        if(materials!=null)foreach(var m in materials)if(m!=null)Destroy(m);
        foreach(var c in cues)if(c!=null)c.system=null;
        VisibleParticles=0;
    }
}
