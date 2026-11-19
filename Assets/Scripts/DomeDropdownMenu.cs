using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Presentation only: every action delegates to the existing viewer/library.
public sealed class DomeDropdownMenu : MonoBehaviour
{
    public const float Width=300, MaxHeight=670;
    public static readonly Color Normal=new Color(.36f,.40f,.47f,.97f);
    public static readonly Color Highlight=new Color(.49f,.56f,.65f,1);
    DomeQuestViewer viewer; Action<Collider,Button> register;
    Font font; Sprite rounded; Texture2D texture;
    Button seatHeader,videoHeader,controlHeader,playButton;
    RectTransform seatList,videoList,controlList;
    Button[] videoRows; Text pager,status; int opened=-1,page;
    const int PageSize=6; const float Row=56;
    public int OpenSection => opened;

    public void Initialize(DomeQuestViewer owner,Action<Collider,Button> registerButton)
    {
        viewer=owner;register=registerButton;font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildRoundedSprite();
        var rect=(RectTransform)transform;rect.pivot=new Vector2(1,1);rect.sizeDelta=new Vector2(Width,MaxHeight);
        seatHeader=ButtonAt(transform,"Seat dropdown","Seat",()=>Toggle(0));
        videoHeader=ButtonAt(transform,"Video dropdown","Video",()=>Toggle(1));
        controlHeader=ButtonAt(transform,"Controls dropdown","Controls",()=>Toggle(2));
        seatList=List("Seat options");
        for(int i=0;i<viewer.seats.Length;i++)
        {
            int index=i;
            var b=ButtonAt(seatList,"Seat option "+viewer.seatNames[i],SeatName(i),()=>{viewer.SelectSeat(index);Close();});
            Position(b.transform,-Width/2,-12-Row*i-24,276,48);
        }
        seatList.sizeDelta=new Vector2(Width,24+Row*viewer.seats.Length);
        videoList=List("Video options");videoRows=new Button[PageSize];
        for(int i=0;i<PageSize;i++)
        {
            int slot=i;videoRows[i]=ButtonAt(videoList,"Video option "+i,"",()=>{viewer.library.Select(page*PageSize+slot);Close();});
            Position(videoRows[i].transform,-Width/2,-12-Row*i-24,276,48);
        }
        var previous=ButtonAt(videoList,"Previous page","<",()=>{page=Mathf.Max(0,page-1);RefreshRows();});
        var next=ButtonAt(videoList,"Next page",">",()=>{page=Mathf.Min((viewer.library.Count-1)/PageSize,page+1);RefreshRows();});
        Position(previous.transform,-244,-390,80,44);Position(next.transform,-56,-390,80,44);
        pager=Label(videoList,"Page",18);Position(pager.transform,-150,-390,100,44);
        videoList.sizeDelta=new Vector2(Width,426);
        controlList=List("Playback options");
        playButton=Control("Play / Pause",0,viewer.TogglePlayback);
        Control("Restart",1,()=>viewer.library.playback.RestartPlayback());
        Control("Recenter",2,()=>viewer.SelectSeat(viewer.seat));
        Control("Hide menu",3,()=>viewer.ShowMenu(false));
        status=Label(controlList,"Playback status",17);Position(status.transform,-150,-258,276,38);
        controlList.sizeDelta=new Vector2(Width,290);
        Layout();RefreshView();
    }
    string SeatName(int i)
    {
        string s=viewer.seatNames[i];return s.Length==0?s:char.ToUpper(s[0])+s.Substring(1).ToLowerInvariant();
    }
    public void Toggle(int section)
    {
        opened=opened==section?-1:section;
        if(opened==1){page=viewer.library.current/PageSize;RefreshRows();}
        Layout();RefreshView();
    }
    public void Close(){opened=-1;Layout();RefreshView();}
    public void RefreshView()
    {
        if(viewer==null)return;
        SetLabel(seatHeader,SeatName(viewer.seat)+(opened==0?"  ▴":"  ▾"));
        string clip=viewer.library.Count>0?viewer.library.presets[viewer.library.current].displayName:"Video";
        SetLabel(videoHeader,clip+(opened==1?"  ▴":"  ▾"));
        SetLabel(controlHeader,"Controls"+(opened==2?"  ▴":"  ▾"));
        SetLabel(playButton,viewer.library.playback.State==DomeVideoSetup.PlaybackState.Playing?"Pause":"Play");
        status.text=$"{(viewer.IsVR?"VR":"PC")}  ·  {viewer.library.playback.State}";
    }
    void RefreshRows()
    {
        for(int i=0;i<PageSize;i++)
        {
            int index=page*PageSize+i;bool valid=index<viewer.library.Count;
            videoRows[i].gameObject.SetActive(valid);
            if(valid)SetLabel(videoRows[i],viewer.library.presets[index].displayName);
        }
        pager.text=$"{page+1} / {Mathf.Max(1,Mathf.CeilToInt(viewer.library.Count/(float)PageSize))}";
    }
    void Layout()
    {
        float y=0;
        Button[] headers={seatHeader,videoHeader,controlHeader};RectTransform[] lists={seatList,videoList,controlList};
        for(int i=0;i<3;i++)
        {
            Position(headers[i].transform,-Width/2,-y-26,Width,52);y+=62;
            lists[i].gameObject.SetActive(opened==i);
            if(opened==i){lists[i].anchoredPosition=new Vector2(0,-y);y+=lists[i].sizeDelta.y+10;}
        }
    }
    RectTransform List(string name)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
        var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(1,1);
        var image=go.GetComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=new Color(.14f,.19f,.24f,.96f);image.raycastTarget=false;return r;
    }
    Button Control(string label,int row,UnityAction action)
    {var b=ButtonAt(controlList,label,label,action);Position(b.transform,-Width/2,-36-Row*row,276,48);return b;}
    Button ButtonAt(Transform parent,string name,string label,UnityAction action)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button),typeof(BoxCollider));go.transform.SetParent(parent,false);
        var image=go.GetComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=Normal;
        var b=go.GetComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;b.onClick.AddListener(action);
        var c=go.GetComponent<BoxCollider>();c.size=new Vector3(276,48,6);register(c,b);
        var t=Label(go.transform,label,22);var rect=(RectTransform)t.transform;
        rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(14,2);rect.offsetMax=new Vector2(-14,-2);
        return b;
    }
    Text Label(Transform parent,string value,int size)
    {
        var go=new GameObject(value,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        var t=go.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=size;t.color=new Color(.96f,.97f,1);
        t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=15;t.resizeTextMaxSize=size;return t;
    }
    static void SetLabel(Button b,string value){b.GetComponentInChildren<Text>().text=value;}
    static void Position(Transform transform,float x,float y,float width,float height)
    {
        var r=(RectTransform)transform;r.anchorMin=r.anchorMax=new Vector2(1,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);
        var c=transform.GetComponent<BoxCollider>();if(c!=null)c.size=new Vector3(width,height,6);
    }
    void BuildRoundedSprite()
    {
        const int n=64;texture=new Texture2D(n,n,TextureFormat.RGBA32,false);texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
        for(int y=0;y<n;y++)for(int x=0;x<n;x++)
        {float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-d)));}
        texture.Apply();rounded=Sprite.Create(texture,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(31,31,31,31));
    }
    void OnDestroy(){if(rounded!=null)Destroy(rounded);if(texture!=null)Destroy(texture);}
}
