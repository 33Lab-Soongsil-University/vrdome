using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using Unity.XR.CoreUtils;

// Seated viewing only. Seat changes move the rig; head tracking always owns the camera.
public class DomeQuestViewer : MonoBehaviour
{
    public Camera head;
    public DomeClipLibrary library;
    public Transform[] seats;
    public string[] seatNames;
    public Shader pointerShader;
    public int seat;
    Transform panel; DomeDropdownMenu menu; LineRenderer laser; Material laserMaterial;
    readonly List<XRDisplaySubsystem> displays=new List<XRDisplaySubsystem>();
    TrackedPoseDriver poseDriver; float desktopYaw,desktopPitch; bool vrCentered;
    public bool IsVR { get; private set; }
    public bool DesktopInputEnabled => !IsVR;
    readonly Dictionary<Collider,Button> buttons=new Dictionary<Collider,Button>();
    bool triggerWasDown,menuWasDown,playWasDown,recenterWasDown;
    bool dwellClicked; float dwell,axisReady,reopenDwell; Button hover;
    public bool MenuVisible => panel!=null&&panel.gameObject.activeSelf;
    void Awake()
    {
        Application.runInBackground=true;Application.targetFrameRate=72;
        var desktop=FindAnyObjectByType<SphereTheaterView>();if(desktop!=null)desktop.enabled=false;
        library.showInterface=false;library.playback.showControls=false;
    }
    void Start()
    {
        poseDriver=head.GetComponent<TrackedPoseDriver>();
        BuildMenu();ApplyInputMode(HasVRDisplay(),true);ShowMenu(true);
    }
    bool HasVRDisplay()
    {
        // A running display defines VR, not momentary tracking or controller loss.
        displays.Clear();SubsystemManager.GetSubsystems(displays);
        foreach(var display in displays)if(display.running)return true;
        return false;
    }
    void ApplyInputMode(bool vr,bool force=false)
    {
        if(!force&&IsVR==vr)return;
        IsVR=vr;vrCentered=false;
        if(poseDriver!=null)poseDriver.enabled=vr;
        desktopYaw=desktopPitch=0;
        head.transform.localPosition=Vector3.zero;head.transform.localRotation=Quaternion.identity;
        triggerWasDown=menuWasDown=playWasDown=recenterWasDown=false;
        ClearHover();SelectSeat(seat);
    }
    void LateUpdate()
    {
        if(panel==null)return;
        if(!IsVR){if(MenuVisible)PlaceMenu();return;}
        if(vrCentered)return;
        var h=InputDevices.GetDeviceAtXRNode(XRNode.Head);
        if(h.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked)&&tracked){SelectSeat(seat);vrCentered=true;}
    }
    void ClearHover()
    {
        if(hover!=null)hover.image.color=Normal;
        hover=null;dwell=0;dwellClicked=false;reopenDwell=0;
    }
    public void SelectSeat(int index)
    {
        if(seats==null||seats.Length==0)return;
        seat=(index%seats.Length+seats.Length)%seats.Length;var anchor=seats[seat];if(anchor==null)return;
        // Keep the virtual floor level; only the tracked head may pitch or roll.
        transform.rotation=Quaternion.Euler(0,transform.eulerAngles.y,0);
        if(!IsVR){desktopYaw=desktopPitch=0;head.transform.localPosition=Vector3.zero;head.transform.localRotation=Quaternion.identity;}
        float delta=Mathf.DeltaAngle(head.transform.eulerAngles.y,anchor.eulerAngles.y);
        transform.RotateAround(head.transform.position,Vector3.up,delta);
        transform.position+=anchor.position-head.transform.position;
        if(MenuVisible)PlaceMenu();
    }
    public void SelectClip(int delta){if(library.Count==0)return;library.Select((library.current+delta+library.Count)%library.Count);}
    public void TogglePlayback(){if(library.playback.State==DomeVideoSetup.PlaybackState.Playing)library.playback.PausePlayback();else library.playback.ResumePlayback();}
    public void ShowMenu(bool show){panel.gameObject.SetActive(show);menu.Close();if(show)PlaceMenu();ClearHover();}
    void PlaceMenu()
    {
        if(!IsVR)
        {
            // Match a compact top-right overlay while keeping one raycastable Canvas for PC and VR.
            float heightAtDepth=4f*Mathf.Tan(head.fieldOfView*Mathf.Deg2Rad*.5f);
            panel.localScale=Vector3.one*(heightAtDepth*.88f/DomeDropdownMenu.MaxHeight);
            panel.position=head.ViewportToWorldPoint(new Vector3(.985f,.97f,2f));
            panel.rotation=head.transform.rotation;return;
        }
        panel.localScale=Vector3.one*.0014f;
        Vector3 f=Vector3.ProjectOnPlane(head.transform.forward,Vector3.up).normalized;if(f.sqrMagnitude<.1f)f=transform.forward;
        Vector3 right=Vector3.Cross(Vector3.up,f);
        // A world-anchored panel slightly right of the viewing direction, repositioned only on request.
        panel.position=head.transform.position+f*2f+right*.8f+Vector3.up*.48f;
        panel.rotation=Quaternion.LookRotation(f,Vector3.up);
    }
    void Update()
    {
        if(panel==null)return;
        ApplyInputMode(HasVRDisplay());
        if(!IsVR){laser.enabled=false;UpdateStatus();return;}
        var right=InputDevices.GetDeviceAtXRNode(XRNode.RightHand);var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        right.TryGetFeatureValue(CommonUsages.secondaryButton,out bool menuDown);
        right.TryGetFeatureValue(CommonUsages.primaryButton,out bool playDown);
        left.TryGetFeatureValue(CommonUsages.primaryButton,out bool recenterDown);
        if(menuDown&&!menuWasDown)ShowMenu(!MenuVisible);
        if(playDown&&!playWasDown)TogglePlayback();
        if(recenterDown&&!recenterWasDown)SelectSeat(seat);
        menuWasDown=menuDown;playWasDown=playDown;recenterWasDown=recenterDown;
        right.TryGetFeatureValue(CommonUsages.triggerButton,out bool trigger);
        bool tracked=right.TryGetFeatureValue(CommonUsages.isTracked,out bool t)&&t;
        if(!tracked){right=left;tracked=right.TryGetFeatureValue(CommonUsages.isTracked,out t)&&t;right.TryGetFeatureValue(CommonUsages.triggerButton,out trigger);}
        Vector3 pos=head.transform.position;Quaternion rot=head.transform.rotation;
        if(tracked&&right.TryGetFeatureValue(CommonUsages.devicePosition,out Vector3 p)&&right.TryGetFeatureValue(CommonUsages.deviceRotation,out Quaternion q))
        {pos=head.transform.parent.TransformPoint(p);rot=head.transform.parent.rotation*q;}
        if(MenuVisible)
        {
            UpdateStatus();
            Ray ray=new Ray(pos,rot*Vector3.forward);Button hitButton=null;float distance=3;
            // Only menu colliders are considered; theater geometry cannot steal UI input.
            foreach(var pair in buttons)if(pair.Key.gameObject.activeInHierarchy&&pair.Key.Raycast(ray,out RaycastHit hit,3)&&hit.distance<distance){distance=hit.distance;hitButton=pair.Value;}
            laser.enabled=true;laser.SetPosition(0,tracked?pos:pos+ray.direction*.15f);laser.SetPosition(1,ray.GetPoint(distance));
            if(hover!=hitButton){if(hover!=null)hover.image.color=Normal;hover=hitButton;dwell=0;dwellClicked=false;}
            if(hover!=null)
            {
                hover.image.color=DomeDropdownMenu.Highlight;
                if(tracked){if(trigger&&!triggerWasDown)hover.onClick.Invoke();}
                else {dwell+=Time.unscaledDeltaTime;if(dwell>1.5f&&!dwellClicked){var clicked=hover;dwellClicked=true;clicked.onClick.Invoke();}}
            }
            left.TryGetFeatureValue(CommonUsages.primary2DAxis,out Vector2 axis);
            if(Time.unscaledTime>axisReady&&Mathf.Abs(axis.x)>.75f){SelectClip(axis.x>0?1:-1);axisReady=Time.unscaledTime+.6f;}
        }
        else {laser.enabled=false;
            // Controller-free fallback: look down for two seconds to reopen the menu.
            if(!tracked&&Vector3.Dot(head.transform.forward,Vector3.down)>.75f){reopenDwell+=Time.unscaledDeltaTime;if(reopenDwell>2){ShowMenu(true);reopenDwell=0;}}else reopenDwell=0;
        }
        triggerWasDown=trigger;
    }
    void UpdateStatus(){menu.RefreshView();}
    void HandleDesktopNavigation(Event e)
    {
        if(IsVR)return;
        // IMGUI events are scoped to the Game view, including Input-System-only projects.
        if(e.type==EventType.KeyDown&&!e.alt&&!e.control&&!e.command)
        {
            bool handled=true;
            switch(e.keyCode)
            {
                case KeyCode.Tab:ShowMenu(!MenuVisible);break;
                case KeyCode.Escape:ShowMenu(false);break;
                case KeyCode.Space:TogglePlayback();break;
                case KeyCode.LeftArrow:SelectClip(-1);break;
                case KeyCode.RightArrow:SelectClip(1);break;
                case KeyCode.UpArrow:SelectSeat(seat+1);break;
                case KeyCode.DownArrow:SelectSeat(seat-1);break;
                case KeyCode.R:SelectSeat(seat);break;
                default:handled=false;break;
            }
            if(handled)e.Use();
        }
        if(e.type==EventType.MouseDrag&&e.button==1)
        {
            desktopYaw+=e.delta.x*.16f;desktopPitch=Mathf.Clamp(desktopPitch+e.delta.y*.16f,-80,80);
            head.transform.localRotation=Quaternion.Euler(desktopPitch,desktopYaw,0);e.Use();
        }
    }
    void OnGUI()
    {
        if(panel==null||IsVR)return;
        var e=Event.current;
        HandleDesktopNavigation(e);
        if(!MenuVisible)
        {
            if(GUI.Button(new Rect(Screen.width-150,16,134,34),"Menu [Tab]"))ShowMenu(true);
            return;
        }
        bool click=e.type==EventType.MouseDown&&e.button==0;
        if(DesktopMenuInput(e.mousePosition,click)&&click)e.Use();
        else if(click){menu.Close();ClearHover();}
    }
    bool DesktopMenuInput(Vector2 mousePosition,bool click)
    {
        if(IsVR||!MenuVisible)return false;
        Ray ray=head.ScreenPointToRay(new Vector3(mousePosition.x,Screen.height-mousePosition.y,0));
        Button target=null;float distance=100;
        foreach(var pair in buttons)if(pair.Key.gameObject.activeInHierarchy&&pair.Key.Raycast(ray,out RaycastHit hit,100)&&hit.distance<distance){distance=hit.distance;target=pair.Value;}
        if(hover!=target){ClearHover();hover=target;if(hover!=null)hover.image.color=DomeDropdownMenu.Highlight;}
        if(click&&hover!=null){var clicked=hover;clicked.onClick.Invoke();return true;}
        return false;
    }
    static readonly Color Normal=DomeDropdownMenu.Normal;
    void BuildMenu()
    {
        var go=new GameObject("Quest world menu",typeof(RectTransform),typeof(Canvas),typeof(DomeDropdownMenu));panel=go.transform;
        panel.SetParent(transform,false);
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=head;
        menu=go.GetComponent<DomeDropdownMenu>();menu.Initialize(this,(collider,button)=>buttons.Add(collider,button));
        var beam=new GameObject("Controller menu pointer");beam.transform.SetParent(transform,false);laser=beam.AddComponent<LineRenderer>();
        laserMaterial=new Material(pointerShader);laserMaterial.color=new Color(.3f,1,.8f);laserMaterial.SetColor("_BaseColor",new Color(.3f,1,.8f));
        laser.sharedMaterial=laserMaterial;laser.positionCount=2;laser.startWidth=.003f;laser.endWidth=.002f;laser.useWorldSpace=true;
    }
    void OnDestroy(){if(laserMaterial!=null)Destroy(laserMaterial);}
}
