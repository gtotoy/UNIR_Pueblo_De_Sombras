using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasAWAInstaller
{
    private const string TextureFolder = "Assets/Escenario/Materials/PuebloSombras/Textures/AWAWater";
    private const string MaterialFolder = "Assets/Escenario/Materials/PuebloSombras";
    private const string MaterialPath = MaterialFolder + "/M_PuebloSombras_AWA_Water.mat";
    private const string ShaderName = "Pueblo de Sombras/Stylized River Water";
    private const string MarkerName = "PuebloSombras_AWA_Water_Applied.txt";

    private static string MarkerPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", MarkerName);

    static PuebloSombrasAWAInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath))
        {
            Apply(false);
        }
    }

    [MenuItem("Pueblo de Sombras/Aplicar agua AWA")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_BaseColor_2048.png", false, true);
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_Normal_2048.png", true, false);
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_Height_2048.png", false, false);
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_Roughness_2048.png", false, false);
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_AO_2048.png", false, false);
            ImportTexture(TextureFolder + "/PuebloSombras_AWA_Water_MetallicSmoothness_2048.png", false, false);

            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[PuebloSombras AWA] El shader todavía no está disponible; se reintentará tras la importación.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            EnsureFolder(MaterialFolder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_PuebloSombras_AWA_Water" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/PuebloSombras_AWA_Water_BaseColor_2048.png"));
            material.SetTexture("_NormalMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureFolder + "/PuebloSombras_AWA_Water_Normal_2048.png"));
            material.SetColor("_DeepColor", new Color(0.20f, 0.31f, 0.34f, 1f));
            material.SetColor("_ShallowColor", new Color(0.46f, 0.58f, 0.59f, 1f));
            material.SetColor("_FresnelColor", new Color(0.68f, 0.77f, 0.78f, 1f));
            material.SetFloat("_Opacity", 0.92f);
            material.SetFloat("_Tiling", 5.5f);
            material.SetFloat("_NormalStrength", 0.42f);
            material.SetFloat("_Smoothness", 0.72f);
            material.SetVector("_SpeedA", new Vector4(0.006f, 0.002f, 0f, 0f));
            material.SetVector("_SpeedB", new Vector4(-0.003f, 0.005f, 0f, 0f));
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            GameObject awa = FindExactObject(SceneManager.GetActiveScene(), "AWA");
            if (awa == null)
            {
                Debug.LogError("[PuebloSombras AWA] No se encontró un GameObject llamado exactamente AWA en la escena activa.");
                return;
            }

            Renderer[] renderers = awa.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("[PuebloSombras AWA] AWA no contiene ningún Renderer.");
                return;
            }

            var report = new List<string>
            {
                "Pueblo de Sombras - aplicación de agua AWA",
                "Escena: " + SceneManager.GetActiveScene().path,
                "Objeto: " + GetPath(awa.transform),
                "Material: " + MaterialPath,
                "Shader: " + ShaderName,
                "Tiling de ondas: 5.5",
                "Opacidad: 0.92",
                "Escena guardada automáticamente: no",
                "Renderers modificados: " + renderers.Length
            };

            foreach (Renderer renderer in renderers)
            {
                string oldMaterials = string.Join(", ", renderer.sharedMaterials.Select(m => m != null ? m.name : "<null>"));
                Undo.RecordObject(renderer, "Aplicar agua estilizada a AWA");
                renderer.sharedMaterials = new[] { material };
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                EditorUtility.SetDirty(renderer);
                report.Add($"- {GetPath(renderer.transform)} | antes: {oldMaterials} | bounds: {renderer.bounds.size}");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = awa;
            EditorGUIUtility.PingObject(material);
            Debug.Log("[PuebloSombras AWA] Agua aplicada a AWA. La escena quedó sin guardar para revisión.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ImportTexture(string path, bool normalMap, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        TextureImporterType desiredType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (importer.textureType != desiredType) { importer.textureType = desiredType; changed = true; }
        if (importer.sRGBTexture != sRgb) { importer.sRGBTexture = sRgb; changed = true; }
        if (importer.wrapMode != TextureWrapMode.Repeat) { importer.wrapMode = TextureWrapMode.Repeat; changed = true; }
        if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; changed = true; }
        if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
        if (importer.maxTextureSize != 2048) { importer.maxTextureSize = 2048; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
        {
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static GameObject FindExactObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName) return candidate.gameObject;
            }
        }
        return null;
    }

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
