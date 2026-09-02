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
public static class PuebloSombrasGrassInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string TextureRoot = Root + "/Textures/GrassGround";
    private const string MaterialPath = Root + "/M_PuebloSombras_Grass_Ground.mat";
    private const string Prefix = TextureRoot + "/PuebloSombras_Grass_Ground";
    private const string BaseColorPath = Prefix + "_BaseColor_2048.png";
    private const string NormalPath = Prefix + "_Normal_2048.png";
    private const string AoPath = Prefix + "_AO_2048.png";
    private const string HeightPath = Prefix + "_Height_2048.png";
    private const string RoughnessPath = Prefix + "_Roughness_2048.png";
    private const string PackedPath = Prefix + "_MetallicSmoothness_2048.png";

    static PuebloSombrasGrassInstaller()
    {
        if (!File.Exists(GetMarkerPath()))
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

            Material material = CreateOrUpdateMaterial();
            ApplyToGrassAreas(material);
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
            material = new Material(shader) { name = "M_PuebloSombras_Grass_Ground" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", LoadTexture(BaseColorPath));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", LoadTexture(NormalPath));
        material.SetFloat("_BumpScale", 0.30f);
        material.SetTexture("_MetallicGlossMap", LoadTexture(PackedPath));
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 1.0f);
        material.SetFloat("_SmoothnessTextureChannel", 0.0f);
        material.SetTexture("_OcclusionMap", LoadTexture(AoPath));
        material.SetFloat("_OcclusionStrength", 0.32f);

        Vector2 tiling = new Vector2(0.05f, 0.05f);
        foreach (string property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
        {
            if (material.HasProperty(property))
            {
                material.SetTextureScale(property, tiling);
                material.SetTextureOffset(property, Vector2.zero);
            }
        }

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

    private static void ApplyToGrassAreas(Material generatedMaterial)
    {
        string markerPath = GetMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada.");

        Renderer[] namedAreas = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Suelo Areas Verdes", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Renderer[] targets = namedAreas
            .Where(renderer => renderer.sharedMaterials.Any(item => item != null && item.name == "Material.003"))
            .ToArray();

        if (targets.Length == 0)
            throw new InvalidOperationException("No se encontraron áreas verdes con Material.003.");

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Aplicar pasto Pueblo de Sombras");
        List<string> appliedPaths = new List<string>();
        foreach (Renderer renderer in targets)
        {
            Undo.RecordObject(renderer, "Aplicar pasto estilizado");
            Material[] assigned = renderer.sharedMaterials;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (assigned[index] != null && assigned[index].name == "Material.003")
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
            "Named green areas: " + namedAreas.Length,
            "Applied: " + appliedPaths.Count,
            "Tiling: 0.05 x 0.05",
            "Coastal sidewalk sharing Material.003: intentionally untouched",
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "APPLIED",
        };
        report.AddRange(appliedPaths);
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = generatedMaterial;
        EditorGUIUtility.PingObject(generatedMaterial);
        Debug.Log($"[Pueblo de Sombras] Pasto aplicado a {appliedPaths.Count} áreas verdes. La vereda costera quedó sin cambios. La escena quedó sin guardar.");
    }

    private static string GetMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_GrassGround_Applied.txt"));
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
