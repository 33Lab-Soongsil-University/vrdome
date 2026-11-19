using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class SphereSeatPrefabMigration
{
    static Mesh cube, sphere;
    static Dictionary<string,List<CombineInstance>> batches = new Dictionary<string,List<CombineInstance>>();
    static string folder;
    [MenuItem("Tools/Dome Study/Convert Seats to Editable Prefabs")]
    public static void Convert()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogError("Exit Play Mode first."); return; }
        var view=Object.FindFirstObjectByType<SphereTheaterView>();
        if(view==null) return;
        if(view.transform.Find("Editable Seating")!=null) { Debug.Log("Editable seating already exists; no changes."); return; }
        var scene=view.gameObject.scene;
        if(scene.isDirty) { Debug.LogError("Save scene before conversion."); return; }
        string backup=AssetDatabase.GenerateUniqueAssetPath("Assets/Scenes/sphere_theater_before_prefabs.unity");
        if(!EditorSceneManager.SaveScene(scene,backup,true)) return;
        folder=AssetDatabase.GenerateUniqueAssetPath("Assets/SphereSeatPrefabs");
        AssetDatabase.CreateFolder("Assets",System.IO.Path.GetFileName(folder));
        cube=Primitive(PrimitiveType.Cube);sphere=Primitive(PrimitiveType.Sphere);
        Init();Seat(Vector3.zero,Quaternion.identity);var seatPrefab=SavePrefab("Theater Seat",view);
        Init();Human(Vector3.zero,Quaternion.identity);var personPrefab=SavePrefab("Seated Figure",view);
        var root=new GameObject("Editable Seating");root.transform.SetParent(view.transform,false);
        Undo.RegisterCreatedObjectUndo(root,"Convert seats to prefabs");
        int seatCount=0,people=0;
        for(int row=0;row<28;row++)
        {
            var rowRoot=new GameObject($"Row {row+1:00}");rowRoot.transform.SetParent(root.transform,false);
            float radius=17+row*1.22f,y=row*.64f+(row>=14?1.5f:0);
            int count=Mathf.FloorToInt(radius*116*Mathf.Deg2Rad/.83f);
            int number=0;
            for(int c=0;c<count;c++)
            {
                float degree=-57+(c+.5f)*114/count;
                bool aisle=false;
                foreach(float center in new[]{-56f,-28f,0f,28f,56f}) if(Mathf.Abs(degree-center)<1.3f) aisle=true;
                if(aisle)continue;
                var seat=(GameObject)PrefabUtility.InstantiatePrefab(seatPrefab,rowRoot.transform);
                seat.name=$"Seat {++number:000}";
                float a=degree*Mathf.Deg2Rad;
                seat.transform.localPosition=new Vector3(Mathf.Sin(a)*radius,y,20-Mathf.Cos(a)*radius);
                seat.transform.localRotation=Quaternion.Euler(0,-degree,0);
                seatCount++;
                if((c*7+row*13)%11<3 && row!=12)
                {
                    var person=(GameObject)PrefabUtility.InstantiatePrefab(personPrefab,seat.transform);
                    person.name="Occupant";people++;
                }
            }
        }
        var legacy=new GameObject("Original Combined Seating (disabled backup)");legacy.transform.SetParent(view.transform,false);
        Undo.RegisterCreatedObjectUndo(legacy,"Keep original seating backup");
        foreach(string name in new[]{"Seat fabric","Seat frames","People clothing","People skin"})
        {
            var child=view.transform.Find(name);
            if(child!=null) Undo.SetTransformParent(child,legacy.transform,"Archive combined seating");
        }
        legacy.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        Selection.activeGameObject=root;
        Debug.Log($"[SeatPrefabs] Saved {seatCount} seat prefab instances + {people} occupant prefab instances in 28 rows. Backup: {backup}");
    }
    static Mesh Primitive(PrimitiveType type) { var g=GameObject.CreatePrimitive(type);var m=g.GetComponent<MeshFilter>().sharedMesh;Object.DestroyImmediate(g);return m; }
    static void Init() { batches.Clear();foreach(string key in new[]{"Seat fabric","Seat frames","People clothing","People skin"})batches[key]=new List<CombineInstance>(); }
    static GameObject SavePrefab(string name,SphereTheaterView view)
    {
        var root=new GameObject(name);
        foreach(var pair in batches)
        {
            if(pair.Value.Count==0)continue;
            var mesh=new Mesh{name=name+" "+pair.Key,indexFormat=IndexFormat.UInt32};mesh.CombineMeshes(pair.Value.ToArray(),true,true);mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/"+name+" "+pair.Key+".asset");
            var child=new GameObject(pair.Key);child.transform.SetParent(root.transform,false);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;
            var original=view.transform.Find(pair.Key).GetComponent<MeshRenderer>().sharedMaterial;
            var material=new Material(original){name=name+" "+pair.Key,enableInstancing=true};
            AssetDatabase.CreateAsset(material,folder+"/"+name+" "+pair.Key+".mat");
            child.AddComponent<MeshRenderer>().sharedMaterial=material;
        }
        var result=PrefabUtility.SaveAsPrefabAsset(root,folder+"/"+name+".prefab");Object.DestroyImmediate(root);return result;
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
}
