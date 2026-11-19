using System;
using UnityEditor;
using UnityEngine;

public static class DomeCompositeValidation
{
    [MenuItem("Tools/Dome Study/Validate Crop and Blend")]
    public static void Validate()
    {
        var shader = Shader.Find("DomeStudy/VideoComposite");
        if (shader == null || !shader.isSupported) throw new Exception("Composite shader unavailable.");
        var material = new Material(shader);
        var source = new Texture2D(256, 256, TextureFormat.RGBAFloat, false, true);
        var pixels = new Color[256*256];
        for (int y=0;y<256;y++) for (int x=0;x<256;x++) pixels[y*256+x] = new Color(x/255f,y/255f,0,1);
        source.SetPixels(pixels); source.Apply(); source.wrapMode = TextureWrapMode.Clamp;
        var background = new Texture2D(1,1,TextureFormat.RGBAFloat,false,true);
        background.SetPixel(0,0,Color.blue); background.Apply();
        var rt = new RenderTexture(256,256,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
        rt.Create();
        var readback = new Texture2D(256,256,TextureFormat.RGBAFloat,false,true);
        var previous = RenderTexture.active;
        try
        {
            material.SetTexture("_MainVideo",source); material.SetTexture("_BGVideo",background);
            material.SetVector("_MainRect",new Vector4(.2f,.2f,.6f,.6f));
            material.SetVector("_SourceCrop",new Vector4(.2f,.3f,.4f,.2f));
            material.SetFloat("_BGStrength",1); material.SetFloat("_EdgeBlend",.2f);
            Graphics.Blit(source,rt,material);
            RenderTexture.active = rt; readback.ReadPixels(new Rect(0,0,256,256),0,0); readback.Apply();
            var center=readback.GetPixel(128,128); var outside=readback.GetPixel(10,128); var edge=readback.GetPixel(60,128);
            if (Mathf.Abs(center.r-.4f)>.03f || Mathf.Abs(center.g-.4f)>.03f || center.b>.03f) throw new Exception("Crop center mismatch: "+center);
            if (outside.b<.97f || outside.r>.03f) throw new Exception("Background mismatch: "+outside);
            if (edge.b<.1f || edge.b>.99f || edge.r<=0) throw new Exception("Feather not blended: "+edge);
            material.SetFloat("_EdgeBlend",0); Graphics.Blit(source,rt,material);
            RenderTexture.active=rt; readback.ReadPixels(new Rect(0,0,256,256),0,0); readback.Apply();
            if (readback.GetPixel(60,128).b>.03f) throw new Exception("Zero feather did not hard cut.");
            Debug.Log("[DomeStudy] PASS GPU crop center, outside BG, blended edge, zero feather. Synthetic textures; actual video alignment not tested.");
        }
        finally
        {
            RenderTexture.active=previous;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(source);
            UnityEngine.Object.DestroyImmediate(background); UnityEngine.Object.DestroyImmediate(readback);
            UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
