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
public static class PuebloSombrasBlockCentersInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string TextureRoot = Root + "/Textures/BlockCenters";
    private const string Prefix = TextureRoot + "/PuebloSombras_Block_Centers";
    private const string MaterialPrefix = "M_PuebloSombras_Block_Centers";
    private const string BaseColorPath = Prefix + "_BaseColor_2048.png";
    private const string NormalPath = Prefix + "_Normal_2048.png";
    private const string AoPath = Prefix + "_AO_2048.png";
    private const string HeightPath = Prefix + "_Height_2048.png";
    private const string RoughnessPath = Prefix + "_Roughness_2048.png";
    private const string PackedPath = Prefix + "_MetallicSmoothness_2048.png";

    private static readonly Vector2[] Offsets =
    {
        new Vector2(0.00f, 0.00f),
        new Vector2(0.31f, 0.17f),
        new Vector2(0.63f, 0.47f),
        new Vector2(0.19f, 0.71f),
    };

    static PuebloSombrasBlockCentersInstaller()
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

            Material[] variants = Enumerable.Range(0, Offsets.Length)
                .Select(CreateOrUpdateMaterial)
                .ToArray();
            ApplyToBlockCenters(variants);
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

    private static Material CreateOrUpdateMaterial(int index)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró Universal Render Pipeline/Lit.");

        char suffix = (char)('A' + index);
        string materialName = MaterialPrefix + "_" + suffix;
        string materialPath = Root + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", LoadTexture(BaseColorPath));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", LoadTexture(NormalPath));
        material.SetFloat("_BumpScale", 0.34f);
        material.SetTexture("_MetallicGlossMap", LoadTexture(PackedPath));
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 1.0f);
        material.SetFloat("_SmoothnessTextureChannel", 0.0f);
        material.SetTexture("_OcclusionMap", LoadTexture(AoPath));
        material.SetFloat("_OcclusionStrength", 0.38f);

        Vector2 tiling = new Vector2(0.035f, 0.035f);
        foreach (string property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
        {
            if (!material.HasProperty(property))
                continue;
            material.SetTextureScale(property, tiling);
            material.SetTextureOffset(property, Offsets[index]);
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

    private static void ApplyToBlockCenters(Material[] variants)
    {
        string markerPath = GetMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada.");

        Renderer[] centers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Suelo Centro de Manzanas", StringComparison.OrdinalIgnoreCase) >= 0)
            .OrderBy(renderer => GetHierarchyPath(renderer.transform), StringComparer.Ordinal)
            .ToArray();

        if (centers.Length == 0)
            throw new InvalidOperationException("No se encontraron centros de manzana.");

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Aplicar centros de manzana Pueblo de Sombras");
        List<string> reportLines = new List<string>();
        int applied = 0;

        for (int rendererIndex = 0; rendererIndex < centers.Length; rendererIndex++)
        {
            Renderer renderer = centers[rendererIndex];
            Material replacement = variants[rendererIndex % variants.Length];
            Material[] assigned = renderer.sharedMaterials;
            bool changed = false;
            for (int materialIndex = 0; materialIndex < assigned.Length; materialIndex++)
            {
                Material current = assigned[materialIndex];
                if (current != null && (current.name == "Material.008" || current.name.StartsWith(MaterialPrefix, StringComparison.Ordinal)))
                {
                    assigned[materialIndex] = replacement;
                    changed = true;
                }
            }

            if (!changed)
                continue;

            Undo.RecordObject(renderer, "Aplicar patio interior estilizado");
            renderer.sharedMaterials = assigned;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            applied++;
            reportLines.Add(GetHierarchyPath(renderer.transform) + " -> " + replacement.name);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        List<string> report = new List<string>
        {
            "Scene: " + scene.path,
            "Detected block centers: " + centers.Length,
            "Applied: " + applied,
            "Variants: " + variants.Length,
            "Tiling: 0.035 x 0.035",
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "APPLIED",
        };
        report.AddRange(reportLines);
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = variants[0];
        EditorGUIUtility.PingObject(variants[0]);
        Debug.Log($"[Pueblo de Sombras] Centros de manzana aplicados: {applied}, con {variants.Length} variantes de distribución. La escena quedó sin guardar.");
    }

    private static string GetMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_BlockCenters_Applied.txt"));
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
