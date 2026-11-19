using UnityEngine;
using UnityEngine.Rendering;

// Geometry preview only: no video mapping or experimental dimensions are implied.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[ExecuteAlways]
public class DomePreview : MonoBehaviour
{
    [Min(1f)] public float radius = 10f;
    [Range(30f, 300f)] public float horizontalAngle = 180f;
    [Range(30f, 180f)] public float verticalAngle = 180f;
    [Range(16, 160)] public int horizontalSegments = 96;
    [Range(8, 100)] public int verticalSegments = 48;

    [Range(-75f, 75f)] public float elevationCenterDegrees;

    private Mesh generatedMesh;
    private Material generatedMaterial;
    private Texture2D generatedTexture;

    [ContextMenu("Build Dome Preview")]
    public void BuildDomePreview()
    {
        var currentMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        bool preserveMaterial = currentMaterial != null && currentMaterial != generatedMaterial;
        horizontalSegments = Mathf.Clamp(horizontalSegments, 16, 160);
        verticalSegments = Mathf.Clamp(verticalSegments, 8, 100);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Texture");
        if (shader == null)
        {
            Debug.LogError("No compatible Unlit shader found. Check the render pipeline.", this);
            return;
        }
        if (preserveMaterial) Dispose(generatedMesh);
        else ReleaseResources();
        int columns = horizontalSegments + 1;
        var vertices = new Vector3[columns * (verticalSegments + 1)];
        var uv = new Vector2[vertices.Length];
        var triangles = new int[horizontalSegments * verticalSegments * 6];
        for (int y = 0; y <= verticalSegments; y++)
        {
            float v = (float)y / verticalSegments;
            float elevation = Mathf.Clamp(elevationCenterDegrees + (v - 0.5f) * verticalAngle, -89.9f, 89.9f) * Mathf.Deg2Rad;
            for (int x = 0; x <= horizontalSegments; x++)
            {
                float u = (float)x / horizontalSegments;
                float azimuth = (u - 0.5f) * horizontalAngle * Mathf.Deg2Rad;
                int index = y * columns + x;
                vertices[index] = radius * new Vector3(
                    Mathf.Cos(elevation) * Mathf.Sin(azimuth),
                    Mathf.Sin(elevation),
                    Mathf.Cos(elevation) * Mathf.Cos(azimuth));
                uv[index] = new Vector2(u, v);
            }
        }
        int t = 0;
        for (int y = 0; y < verticalSegments; y++)
        for (int x = 0; x < horizontalSegments; x++)
        {
            int a = y * columns + x;
            // Inward-facing triangles, visible from the sphere center.
            triangles[t++] = a;
            triangles[t++] = a + columns;
            triangles[t++] = a + 1;
            triangles[t++] = a + 1;
            triangles[t++] = a + columns;
            triangles[t++] = a + columns + 1;
        }
        generatedMesh = new Mesh { name = "DomePreviewMesh", hideFlags = HideFlags.DontSave };
        generatedMesh.vertices = vertices;
        generatedMesh.uv = uv;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = generatedMesh;

        // Preserve both the video material and the grid it will restore on stop.
        if (preserveMaterial) return;

        const int size = 512;
        generatedTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        generatedTexture.name = "DomePreviewGrid";
        generatedTexture.hideFlags = HideFlags.DontSave;
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            bool axis = Mathf.Abs(x - size / 2) < 2 || Mathf.Abs(y - size / 2) < 2;
            bool line = x % 32 < 2 || y % 32 < 2;
            pixels[y * size + x] = axis ? new Color(1f, 0.65f, 0.1f)
                : line ? new Color(0.3f, 0.65f, 0.8f) : new Color(0.035f, 0.075f, 0.12f);
        }
        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply();
        generatedMaterial = new Material(shader) { name = "DomePreviewUnlit", hideFlags = HideFlags.DontSave };
        if (generatedMaterial.HasProperty("_BaseMap")) generatedMaterial.SetTexture("_BaseMap", generatedTexture);
        else generatedMaterial.mainTexture = generatedTexture;
        if (generatedMaterial.HasProperty("_BaseColor")) generatedMaterial.SetColor("_BaseColor", Color.white);
        var renderer = GetComponent<MeshRenderer>();
        renderer.sharedMaterial = preserveMaterial ? currentMaterial : generatedMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void OnEnable() { BuildDomePreview(); }
    private void OnDestroy() { ReleaseResources(); }
    private void ReleaseResources()
    {
        Dispose(generatedMesh);
        Dispose(generatedMaterial);
        Dispose(generatedTexture);
    }
    private static void Dispose(Object item)
    {
        if (item == null) return;
        if (Application.isPlaying) Destroy(item);
        else DestroyImmediate(item);
    }
}
