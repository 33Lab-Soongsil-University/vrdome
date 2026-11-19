using UnityEngine;

public class DomeClipLibrary : MonoBehaviour
{
    public DomeClipPreset[] presets;
    public DomeVideoSetup playback;
    public int current;
    public bool showInterface=true;
    public int Count => presets==null?0:presets.Length;
    bool open; Vector2 scroll; Texture2D rounded; GUIStyle button,panel;
    void Start(){Select(current);}
    public void Select(int index)
    {
        if(index<0||index>=Count||presets[index]==null||playback==null)return;
        var p=presets[index];current=index;
        playback.StopPlayback();
        var fire=playback.GetComponent<DomeFireworksExtension>();
        var ambient=playback.GetComponent<DomeAmbientExtension>();
        fire.enabled=false;ambient.enabled=false;
        playback.mainClip=p.mainClip;playback.backgroundClip=p.backgroundClip;
        playback.backgroundImage=p.backgroundImage;playback.backgroundUV=p.backgroundUV;
        playback.mainSourceCrop=p.crop;playback.mainWidthDegrees=p.width;playback.mainHeightDegrees=p.height;
        playback.mainElevationDegrees=p.elevation;playback.mainAzimuthDegrees=0;playback.edgeBlend=p.blend;
        playback.backgroundBrightness=p.backgroundBrightness;playback.cleanBackgroundSky=p.cleanBeachSky;
        playback.sourceEdgeBackground=p.useSourceEdges;
        fire.bursts=p.fireworks;ambient.cues=p.cues;
        fire.enabled=p.fireworks!=null&&p.fireworks.Length>0;ambient.enabled=p.cues!=null&&p.cues.Length>0;
        playback.RestartPlayback();
    }
    void Styles()
    {
        if(rounded!=null)return;
        rounded=new Texture2D(32,32,TextureFormat.RGBA32,false);
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float dx=Mathf.Max(8-x,x-23),dy=Mathf.Max(8-y,y-23);
            float distance=new Vector2(Mathf.Max(0,dx),Mathf.Max(0,dy)).magnitude;
            rounded.SetPixel(x,y,new Color(.07f,.095f,.13f,Mathf.Clamp01(9-distance)*.96f));
        }
        rounded.Apply();button=new GUIStyle(GUI.skin.button){fontSize=13,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(13,10,4,4),border=new RectOffset(10,10,10,10)};
        button.normal.background=rounded;button.hover.background=rounded;button.active.background=rounded;
        button.normal.textColor=new Color(.87f,.95f,1);button.hover.textColor=new Color(.5f,1,.85f);
        panel=new GUIStyle(GUI.skin.box){border=new RectOffset(10,10,10,10)};panel.normal.background=rounded;
    }
    void OnGUI()
    {
        if(!showInterface||Count==0)return;Styles();
        float scale=Mathf.Clamp(Mathf.Min(Screen.width/760f,Screen.height/460f),.35f,1.5f);
        var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        var trigger=new Rect(20,20,220,34);var popup=new Rect(20,60,260,Mathf.Min(Count*34+12,Screen.height/scale-135));
        if(open&&Event.current.type==EventType.MouseDown&&!trigger.Contains(Event.current.mousePosition)&&!popup.Contains(Event.current.mousePosition))open=false;
        if(GUI.Button(trigger,presets[current].displayName+"  ▾",button))open=!open;
        if(open)
        {
            GUI.Box(popup,GUIContent.none,panel);
            scroll=GUI.BeginScrollView(new Rect(popup.x+5,popup.y+5,popup.width-10,popup.height-10),scroll,new Rect(0,0,225,Count*34));
            for(int i=0;i<Count;i++)if(GUI.Button(new Rect(0,i*34,225,31),presets[i].displayName,button)){Select(i);open=false;}
            GUI.EndScrollView();
        }
        GUI.matrix=old;
    }
    void OnDisable(){if(rounded!=null)Destroy(rounded);rounded=null;}
}
