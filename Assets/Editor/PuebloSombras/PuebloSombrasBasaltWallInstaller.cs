#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasBasaltWallInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string TextureRoot = Root + "/Textures/VegetatedBasaltWall";
    private const string MaterialPath = Root + "/M_PuebloSombras_VegetatedBasaltWall.mat";
    private const string BaseColorPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_BaseColor_2048.png";
    private const string NormalPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_Normal_2048.png";
    private const string AoPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_AO_2048.png";
    private const string HeightPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_Height_2048.png";
    private const string RoughnessPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_Roughness_2048.png";
    private const string PackedPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_MetallicSmoothness_2048.png";
    private const string VegetationMaskPath = TextureRoot + "/PuebloSombras_VegetatedBasaltWall_VegetationMask_2048.png";

    static PuebloSombrasBasaltWallInstaller()
    {
        if (!File.Exists(GetAppliedMarkerPath()))
            EditorApplication.delayCall += InstallAndApply;
    }

    private static void InstallAndApply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InstallAndApply;
            return;
        }

        try
        {
            ConfigureTexture(BaseColorPath, TextureImporterType.Default, true, false);
            ConfigureTexture(NormalPath, TextureImporterType.NormalMap, false, false);
            ConfigureTexture(AoPath, TextureImporterType.Default, false, false);
            ConfigureTexture(HeightPath, TextureImporterType.Default, false, false);
            ConfigureTexture(RoughnessPath, TextureImporterType.Default, false, false);
            ConfigureTexture(PackedPath, TextureImporterType.Default, false, true);
            ConfigureTexture(VegetationMaskPath, TextureImporterType.Default, false, false);

            Material material = CreateOrUpdateMaterial();
            ApplyToBasaltWalls(material);
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureTexture(string path, TextureImporterType type, bool srgb, bool preserveAlpha)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            throw new InvalidOperationException("No se pudo importar la textura: " + path);

        importer.textureType = type;
        importer.sRGBTexture = srgb;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.alphaIsTransparency = false;
        if (preserveAlpha)
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró Universal Render Pipeline/Lit.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_PuebloSombras_VegetatedBasaltWall" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", LoadTexture(BaseColorPath));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", LoadTexture(NormalPath));
        material.SetFloat("_BumpScale", 0.42f);
        material.SetTexture("_MetallicGlossMap", LoadTexture(PackedPath));
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 1.0f);
        material.SetFloat("_SmoothnessTextureChannel", 0.0f);
        material.SetTexture("_OcclusionMap", LoadTexture(AoPath));
        material.SetFloat("_OcclusionStrength", 0.58f);
        material.SetTextureScale("_BaseMap", Vector2.one);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_OCCLUSIONMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException("No se pudo cargar la textura: " + path);
        return texture;
    }

    private static void ApplyToBasaltWalls(Material generatedMaterial)
    {
        string markerPath = GetAppliedMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada.");

        Renderer[] namedWalls = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Muro básico Muro 50", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Renderer[] targets = namedWalls
            .Where(renderer => renderer.sharedMaterials.Any(item => item != null && item.name == "Material.002"))
            .ToArray();

        if (targets.Length == 0)
            throw new InvalidOperationException("No se encontraron muros con Material.002; no se modificó la escena.");

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Aplicar basalto con vegetación a muros");
        List<string> appliedPaths = new List<string>();
        foreach (Renderer renderer in targets)
        {
            Undo.RecordObject(renderer, "Aplicar basalto con vegetación");
            Material[] assigned = renderer.sharedMaterials;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (assigned[index] != null && assigned[index].name == "Material.002")
                    assigned[index] = generatedMaterial;
            }
            renderer.sharedMaterials = assigned;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            appliedPaths.Add(GetHierarchyPath(renderer.transform));
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        List<string> report = new List<string>
        {
            "Scene: " + scene.path,
            "Material: " + MaterialPath,
            "Named walls: " + namedWalls.Length,
            "Applied: " + appliedPaths.Count,
            "Skipped: " + (namedWalls.Length - appliedPaths.Count),
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "APPLIED",
        };
        report.AddRange(appliedPaths);
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = generatedMaterial;
        EditorGUIUtility.PingObject(generatedMaterial);
        Debug.Log($"[Pueblo de Sombras] Basalto con vegetación aplicado a {appliedPaths.Count} muros. La escena quedó sin guardar para revisión visual.");
    }

    private static string GetAppliedMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_VegetatedBasaltWall_Applied.txt"));
    }

    private static string GetHierarchyPath(Transform transform)
    {
        List<string> parts = new List<string>();
        Transform current = transform;
        while (current != null)
        {
            parts.Add(current.name);
            current = current.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }
}
#endif
