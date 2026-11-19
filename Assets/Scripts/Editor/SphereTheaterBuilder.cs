using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SphereTheaterBuilder
{
    private static string folder;
    private static Mesh cube, sphere;
    private static readonly Dictionary<string,List<CombineInstance>> batches = new Dictionary<string,List<CombineInstance>>();
    private static readonly Dictionary<string,Material> mats = new Dictionary<string,Material>();
    private static GameObject architecture;

    [MenuItem("Tools/Dome Study/Build Sphere Theater Copy")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Exit Play before building theater."); return; }
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(scene.isDirty) { Debug.LogError("Save current scene before creating the theater copy."); return; }
        if(Object.FindFirstObjectByType<SphereTheaterView>()!=null) { Debug.LogWarning("Theater already exists; open the source preview to build a new copy."); return; }
        var dome=Object.FindFirstObjectByType<DomeVideoSetup>();
        if(dome==null) { Debug.LogError("Existing DomeRoot required."); return; }
        string scenePath=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/sphere_theater.unity");
        if(!EditorSceneManager.SaveScene(scene,scenePath,true)) return;
        EditorSceneManager.OpenScene(scenePath);
        dome=Object.FindFirstObjectByType<DomeVideoSetup>();
        folder=AssetDatabase.GenerateUniqueAssetPath("Assets/SphereTheater");
        AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
        batches.Clear(); mats.Clear();
        cube=PrimitiveMesh(PrimitiveType.Cube); sphere=PrimitiveMesh(PrimitiveType.Sphere);
        architecture=new GameObject("Sphere Theater / Architecture");
        Mat("Seat fabric",new Color(.065f,.052f,.085f),.15f);
        Mat("Seat frames",new Color(.035f,.039f,.05f),.5f);
        Mat("Terraces",new Color(.045f,.048f,.06f),.05f);
        Mat("Railings",new Color(.11f,.13f,.16f),.65f);
        Mat("Stage",new Color(.025f,.03f,.04f),.35f);
        Mat("People clothing",new Color(.09f,.12f,.15f),.05f);
        Mat("People skin",new Color(.23f,.17f,.13f),.1f);
        Mat("Aisle amber",new Color(.55f,.26f,.06f),.3f,new Color(1.2f,.4f,.07f));
        Mat("Balcony blue",new Color(.04f,.17f,.23f),.3f,new Color(.03f,.3f,.5f));
        // Parametric visual approximation, not measured venue dimensions.
        const int rows=28;
        int seats=0,figures=0;
        for(int row=0;row<rows;row++)
        {
            float radius=17+row*1.22f;
            float y=row*.64f + (row>=14?1.5f:0);
            // Continuous curved terrace assembled into a single mesh per material.
            for(int segment=0;segment<112;segment++)
            {
                float a=(-58+(segment+.5f)*116/112f)*Mathf.Deg2Rad;
                Vector3 pos=Arc(radius,a,y-.18f);
                Quaternion rot=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                Box("Terraces",pos,rot,new Vector3(radius*116*Mathf.Deg2Rad/112*1.02f,.36f,1.24f));
            }
            int count=Mathf.FloorToInt(radius*116*Mathf.Deg2Rad/.83f);
            for(int c=0;c<count;c++)
            {
                float degree=-57+(c+.5f)*114/count;
                if(IsAisle(degree)) continue;
                float a=degree*Mathf.Deg2Rad;
                var pos=Arc(radius,a,y);
                var rot=Quaternion.Euler(0,-degree,0);
                Seat(pos,rot); seats++;
                if((c*7+row*13)%11<3 && row!=12)
                {
                    Human(pos,rot); figures++;
                }
            }
            foreach(float degree in new[]{-56f,-28f,0f,28f,56f})
            {
                var rot=Quaternion.Euler(0,-degree,0);
                for(int half=0;half<2;half++)
                {
                    var pos=Arc(radius-.3f+half*.6f,degree*Mathf.Deg2Rad,y-.16f+half*.16f);
                    Box("Terraces",pos,rot,new Vector3(.95f,.18f,.61f));
                    Box("Aisle amber",pos+rot*new Vector3(0,.1f,.27f),rot,new Vector3(.75f,.025f,.045f));
                }
                if(row%3==0 && degree!=0)
                {
                    var pos=Arc(radius,degree*Mathf.Deg2Rad,y);
                    Box("Railings",pos+rot*new Vector3(.5f,.55f,0),rot,new Vector3(.045f,1.1f,.045f));
                    Box("Railings",pos+rot*new Vector3(.5f,1.1f,0),rot*Quaternion.Euler(-28,0,0),new Vector3(.06f,.06f,3.7f));
                }
            }
            if(row==13 || row==27)
            {
                for(int s=0;s<112;s++)
                {
                    float a=(-58+(s+.5f)*116/112f)*Mathf.Deg2Rad;
                    var rot=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
                    var p=Arc(radius+.65f,a,y+.35f);
                    Box("Railings",p,rot,new Vector3(radius*116*Mathf.Deg2Rad/112*1.05f,.7f,.15f));
                    Box("Balcony blue",p+Vector3.up*.38f,rot,new Vector3(radius*116*Mathf.Deg2Rad/112*1.05f,.035f,.17f));
                }
            }
        }
        Box("Stage",new Vector3(0,-.45f,20),Quaternion.identity,new Vector3(38,.8f,21));
        Box("Aisle amber",new Vector3(0,-.03f,9.6f),Quaternion.identity,new Vector3(38,.025f,.05f));
        Box("Stage",new Vector3(0,-1,0),Quaternion.identity,new Vector3(100,.5f,105));
        Flush();
        var preview=dome.GetComponent<DomePreview>();
        dome.transform.SetPositionAndRotation(new Vector3(0,12,0),Quaternion.identity);
        dome.transform.localScale=Vector3.one;
        preview.radius=48; preview.horizontalAngle=240; preview.verticalAngle=104; preview.elevationCenterDegrees=37.9f;
        preview.horizontalSegments=160; preview.verticalSegments=100;
        var standby=new Material(Shader.Find("DomeStudy/SphereStandby"));
        AssetDatabase.CreateAsset(standby,folder+"/Standby.mat");
        dome.GetComponent<MeshRenderer>().sharedMaterial=standby;
        preview.BuildDomePreview();
        dome.mainElevationDegrees=10; dome.mainWidthDegrees=85; dome.mainHeightDegrees=48;
        dome.showControls=false; dome.autoPlay=dome.mainClip!=null;
        var effects=dome.GetComponent<DomeTimedEffects>();
        if(effects!=null)
        {
            foreach(var cue in effects.cues) if(cue!=null) cue.enabled=false;
        }
        foreach(var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None)) light.enabled=false;
        AddLight("Soft screen spill",new Vector3(0,28,25),new Vector3(40,180,0),new Color(.53f,.66f,1),.65f,LightType.Directional);
        AddLight("Warm overhead",new Vector3(0,35,-12),new Vector3(60,20,0),new Color(1,.63f,.36f),.28f,LightType.Directional);
        RenderSettings.ambientMode=AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.18f,.2f,.27f);
        var cam=Camera.main;
        cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.003f,.005f,.009f);
        cam.fieldOfView=78;cam.nearClipPlane=.08f;cam.farClipPlane=220;
        var controller=architecture.AddComponent<SphereTheaterView>();
        controller.viewer=cam;controller.playback=dome;
        controller.seats=new Transform[5];
        int[] rowIndices={12,3,25,12,12}; float[] angles={2,2,2,-33,33};
        for(int i=0;i<5;i++)
        {
            int row=rowIndices[i];float r=17+row*1.22f;float y=row*.64f+(row>=14?1.5f:0);
            var point=new GameObject(controller.labels[i]+" / Eye position").transform;
            point.SetParent(architecture.transform);
            point.position=Arc(r,angles[i]*Mathf.Deg2Rad,y+1.22f);
            point.rotation=Quaternion.LookRotation(new Vector3(0,17,35)-point.position);
            controller.seats[i]=point;
        }
        controller.SetSeat(0);
        EditorUtility.SetDirty(dome);EditorUtility.SetDirty(preview);
        EditorSceneManager.MarkSceneDirty(dome.gameObject.scene);
        EditorSceneManager.SaveOpenScenes();AssetDatabase.SaveAssets();
        Selection.activeGameObject=architecture;
        if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.LookAt(new Vector3(0,15,0),Quaternion.Euler(20,0,0),80);
        Debug.Log($"[SphereTheater] Built {seats} seats, {figures} figures, 28 curved rows, 5 viewpoints. Saved {scenePath}. Approximation, not an architectural survey.");
    }
    static bool IsAisle(float a) { foreach(float center in new[]{-56f,-28f,0f,28f,56f}) if(Mathf.Abs(a-center)<1.3f) return true; return false; }
    static Vector3 Arc(float r,float a,float y) { return new Vector3(Mathf.Sin(a)*r,y,20-Mathf.Cos(a)*r); }
    static Mesh PrimitiveMesh(PrimitiveType type) { var g=GameObject.CreatePrimitive(type);var m=g.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(g);return m; }
    static void Mat(string name,Color color,float smooth,Color? emission=null)
    {
        var m=new Material(Shader.Find("Universal Render Pipeline/Lit"));m.name=name;m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
        if(emission.HasValue) { m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",emission.Value); }
        AssetDatabase.CreateAsset(m,folder+"/"+name+".mat");mats[name]=m;batches[name]=new List<CombineInstance>();
    }
    static void Box(string mat,Vector3 p,Quaternion r,Vector3 scale) { batches[mat].Add(new CombineInstance{mesh=cube,transform=Matrix4x4.TRS(p,r,scale)}); }
    static void Ellipsoid(string mat,Vector3 p,Quaternion r,Vector3 scale) { batches[mat].Add(new CombineInstance{mesh=sphere,transform=Matrix4x4.TRS(p,r,scale)}); }
    static void Seat(Vector3 p,Quaternion q)
    {
        Box("Seat fabric",p+q*new Vector3(0,.47f,0),q,new Vector3(.59f,.14f,.52f));
        Box("Seat fabric",p+q*new Vector3(0,.86f,-.23f),q*Quaternion.Euler(-8,0,0),new Vector3(.62f,.7f,.15f));
        Box("Seat frames",p+q*new Vector3(0,.23f,0),q,new Vector3(.12f,.45f,.3f));
        for(int s=-1;s<=1;s+=2) Box("Seat frames",p+q*new Vector3(s*.34f,.64f,0),q,new Vector3(.065f,.08f,.5f));
    }
    static void Human(Vector3 p,Quaternion q)
    {
        Ellipsoid("People clothing",p+q*new Vector3(0,.82f,-.02f),q,new Vector3(.4f,.59f,.28f));
        Ellipsoid("People skin",p+q*new Vector3(0,1.24f,-.015f),q,new Vector3(.22f,.27f,.23f));
        for(int s=-1;s<=1;s+=2)
        {
            Box("People clothing",p+q*new Vector3(s*.115f,.51f,.25f),q,new Vector3(.15f,.17f,.38f));
            Box("People clothing",p+q*new Vector3(s*.115f,.3f,.4f),q,new Vector3(.14f,.43f,.16f));
            Box("People clothing",p+q*new Vector3(s*.23f,.75f,.07f),q*Quaternion.Euler(-15,0,0),new Vector3(.11f,.37f,.13f));
        }
    }
    static void Flush()
    {
        foreach(var pair in batches)
        {
            var mesh=new Mesh{name=pair.Key,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/"+pair.Key+".asset");
            var go=new GameObject(pair.Key);go.transform.SetParent(architecture.transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mats[pair.Key];
        }
    }
    static void AddLight(string name,Vector3 pos,Vector3 angle,Color color,float intensity,LightType type)
    {
        var go=new GameObject(name);go.transform.SetParent(architecture.transform);go.transform.position=pos;go.transform.eulerAngles=angle;
        var light=go.AddComponent<Light>();light.type=type;light.color=color;light.intensity=intensity;light.shadows=LightShadows.None;
    }
}
