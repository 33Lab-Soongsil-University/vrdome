using UnityEngine;

public class SphereTheaterView : MonoBehaviour
{
    public Camera viewer;
    public DomeVideoSetup playback;
    public Transform[] seats;
    public string[] labels = { "CENTER", "FRONT", "UPPER", "LEFT", "RIGHT" };
    public int currentSeat;
    public bool showInterface = true;
    private float yaw, pitch;
    private bool seatMenuOpen, seatChosen;
    private Vector2 seatScroll;
    private Texture2D panelTexture, buttonTexture, hoverTexture, selectedTexture;
    private GUIStyle panel, button, selected, title, subtitle, caption, badge;
    private void Start() { SetSeat(currentSeat); }
    public void SetSeat(int index)
    {
        if(viewer==null || seats==null || index<0 || index>=seats.Length || seats[index]==null)return;
        currentSeat=index;
        viewer.transform.SetPositionAndRotation(seats[index].position,seats[index].rotation);
        yaw=0;pitch=0;
    }
    private string SeatLabel(int index)
    {
        if(labels==null || index<0 || index>=labels.Length)return "SEAT";
        return labels[index];
    }
    private string DisplayLabel(int index)
    {
        string value=SeatLabel(index);
        return string.IsNullOrEmpty(value)?"Seat":value.Substring(0,1)+value.Substring(1).ToLowerInvariant();
    }
    private void OnGUI()
    {
        if(!showInterface || viewer==null)return;
        var e=Event.current;
        if(e.type==EventType.MouseDrag && e.button==1 && seats!=null && currentSeat>=0 && currentSeat<seats.Length && seats[currentSeat]!=null)
        {
            yaw=Mathf.Clamp(yaw+e.delta.x*.16f,-100,100);
            pitch=Mathf.Clamp(pitch+e.delta.y*.16f,-65,65);
            viewer.transform.rotation=seats[currentSeat].rotation*Quaternion.Euler(pitch,yaw,0);e.Use();
        }
        EnsureStyles();
        var previous=GUI.matrix;
        float scale=Mathf.Clamp(Mathf.Min(Screen.width/760f,Screen.height/460f),.35f,1.5f);
        GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
        float w=Screen.width/scale,h=Screen.height/scale;
        const float menuWidth=152;
        var trigger=new Rect(w-menuWidth-20,20,menuWidth,34);
        int count=seats==null?0:seats.Length;
        float listHeight=Mathf.Min(count*34+12,Mathf.Max(50,h-80));
        var popup=new Rect(trigger.x,trigger.yMax+6,menuWidth,listHeight);
        if(seatMenuOpen && e.type==EventType.KeyDown && e.keyCode==KeyCode.Escape)
        { seatMenuOpen=false;e.Use(); }
        if(seatMenuOpen && e.type==EventType.MouseDown && !trigger.Contains(e.mousePosition) && !popup.Contains(e.mousePosition))
            seatMenuOpen=false;
        string chosen=seatChosen?DisplayLabel(currentSeat):"Seat";
        if(GUI.Button(trigger,chosen+(seatMenuOpen?"  ▴":"  ▾"),button))seatMenuOpen=!seatMenuOpen;
        if(seatMenuOpen)
        {
            GUI.Box(popup,GUIContent.none,panel);
            var viewport=new Rect(popup.x+6,popup.y+6,popup.width-12,popup.height-12);
            bool scroll=count*34>viewport.height;
            seatScroll=GUI.BeginScrollView(viewport,seatScroll,new Rect(0,0,viewport.width-(scroll?16:0),count*34),false,scroll);
            for(int n=0;n<count;n++)
            {
                bool oldEnabled=GUI.enabled;
                GUI.enabled=seats[n]!=null;
                if(GUI.Button(new Rect(0,n*34,viewport.width-(scroll?18:2),30),DisplayLabel(n),seatChosen && n==currentSeat?selected:button))
                { SetSeat(n);seatChosen=true;seatMenuOpen=false; }
                GUI.enabled=oldEnabled;
            }
            GUI.EndScrollView();
        }
        bool video=playback!=null && playback.mainClip!=null;
        if(video)
        {
            float controlsLeft=(w-416)/2;
            float controlsY=h-56;
            if(GUI.Button(new Rect(controlsLeft,controlsY,98,36),"Play",button))playback.ResumePlayback();
            if(GUI.Button(new Rect(controlsLeft+106,controlsY,98,36),"Pause",button))playback.PausePlayback();
            if(GUI.Button(new Rect(controlsLeft+212,controlsY,98,36),"Restart",button))playback.RestartPlayback();
            if(GUI.Button(new Rect(controlsLeft+318,controlsY,98,36),"Standby",button))playback.StopPlayback();
        }
        GUI.matrix=previous;
    }
    private void EnsureStyles()
    {
        if(panelTexture!=null)return;
        panelTexture=Rounded(new Color(.035f,.045f,.068f,.94f));
        buttonTexture=Rounded(new Color(.12f,.15f,.20f,.93f));
        hoverTexture=Rounded(new Color(.20f,.25f,.32f,1));
        selectedTexture=Rounded(new Color(.52f,.90f,.84f,1));
        panel=new GUIStyle(GUI.skin.box){border=new RectOffset(20,20,20,20)};
        panel.normal.background=panelTexture;
        button=new GUIStyle(GUI.skin.button){fontSize=12,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,border=new RectOffset(20,20,20,20),padding=new RectOffset(8,8,0,0)};
        button.normal.background=buttonTexture;button.normal.textColor=new Color(.82f,.87f,.94f);
        button.hover.background=hoverTexture;button.hover.textColor=Color.white;
        button.active.background=selectedTexture;button.active.textColor=new Color(.03f,.12f,.14f);
        button.focused.background=hoverTexture;
        selected=new GUIStyle(button);selected.normal.background=selectedTexture;selected.normal.textColor=new Color(.035f,.15f,.16f);
        selected.hover.background=selectedTexture;selected.hover.textColor=selected.normal.textColor;
        title=new GUIStyle(GUI.skin.label){fontSize=26,fontStyle=FontStyle.Bold};title.normal.textColor=new Color(.94f,.97f,1);
        subtitle=new GUIStyle(GUI.skin.label){fontSize=11};subtitle.normal.textColor=new Color(.62f,.72f,.80f);
        caption=new GUIStyle(subtitle){fontSize=10,fontStyle=FontStyle.Bold};caption.normal.textColor=new Color(.78f,.86f,.92f);
        badge=new GUIStyle(button){fontSize=11};badge.normal.background=panelTexture;badge.normal.textColor=new Color(.52f,.90f,.84f);
    }
    private static Texture2D Rounded(Color color)
    {
        const int size=48; const float radius=20;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.HideAndDontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        var pixels=new Color[size*size];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float dx=Mathf.Max(Mathf.Abs(x+.5f-size*.5f)-(size*.5f-radius),0);
            float dy=Mathf.Max(Mathf.Abs(y+.5f-size*.5f)-(size*.5f-radius),0);
            float alpha=Mathf.Clamp01(radius-Mathf.Sqrt(dx*dx+dy*dy)+.5f);
            pixels[y*size+x]=new Color(color.r,color.g,color.b,color.a*alpha);
        }
        texture.SetPixels(pixels);texture.Apply();return texture;
    }
    private void OnDisable()
    {
        foreach(var t in new[]{panelTexture,buttonTexture,hoverTexture,selectedTexture})if(t!=null)Destroy(t);
        panelTexture=buttonTexture=hoverTexture=selectedTexture=null;
    }
}
