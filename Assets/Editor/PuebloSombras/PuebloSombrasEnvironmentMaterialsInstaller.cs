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
public static class PuebloSombrasEnvironmentMaterialsInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string TextureRoot = Root + "/Textures";

    private sealed class MaterialSpec
    {
        public string Label;
        public string Name;
        public string Folder;
        public string HierarchyKeyword;
        public string OriginalMaterial;
        public float Tiling;
        public float NormalScale;
        public float OcclusionStrength;

        public string MaterialPath => Root + "/M_PuebloSombras_" + Name + ".mat";
        public string TexturePrefix => TextureRoot + "/" + Folder + "/PuebloSombras_" + Name;
        public string BaseColorPath => TexturePrefix + "_BaseColor_2048.png";
        public string NormalPath => TexturePrefix + "_Normal_2048.png";
        public string AoPath => TexturePrefix + "_AO_2048.png";
        public string HeightPath => TexturePrefix + "_Height_2048.png";
        public string RoughnessPath => TexturePrefix + "_Roughness_2048.png";
        public string PackedPath => TexturePrefix + "_MetallicSmoothness_2048.png";
    }

    private sealed class ApplyResult
    {
        public MaterialSpec Spec;
        public int Named;
        public readonly List<string> AppliedPaths = new List<string>();
    }

    private static readonly MaterialSpec[] Specs =
    {
        new MaterialSpec
        {
            Label = "Calles",
            Name = "Road_Asphalt",
            Folder = "RoadAsphalt",
            HierarchyKeyword = "Suelo Calles",
            OriginalMaterial = "Material.001",
            Tiling = 0.05f,
            NormalScale = 0.22f,
            OcclusionStrength = 0.42f,
        },
        new MaterialSpec
        {
            Label = "Cordones",
            Name = "Curb_Concrete",
            Folder = "CurbConcrete",
            HierarchyKeyword = "Suelo Cordones",
            OriginalMaterial = "Material.007",
            Tiling = 0.12f,
            NormalScale = 0.28f,
            OcclusionStrength = 0.38f,
        },
        new MaterialSpec
        {
            Label = "Fachadas",
            Name = "House_Stucco",
            Folder = "HouseStucco",
            HierarchyKeyword = "Muro básico Viviendas",
            OriginalMaterial = "Material.004",
            Tiling = 0.08f,
            NormalScale = 0.18f,
            OcclusionStrength = 0.34f,
        },
        new MaterialSpec
        {
            Label = "Techos",
            Name = "Roof_Tiles",
            Folder = "RoofTiles",
            HierarchyKeyword = "Cubierta básica",
            OriginalMaterial = "Material.005",
            Tiling = 0.35f,
            NormalScale = 0.42f,
            OcclusionStrength = 0.55f,
        },
    };

    static PuebloSombrasEnvironmentMaterialsInstaller()
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
            Dictionary<MaterialSpec, Material> materials = new Dictionary<MaterialSpec, Material>();
            foreach (MaterialSpec spec in Specs)
            {
                ConfigureTexture(spec.BaseColorPath, TextureImporterType.Default, true, false);
                ConfigureTexture(spec.NormalPath, TextureImporterType.NormalMap, false, false);
                ConfigureTexture(spec.AoPath, TextureImporterType.Default, false, false);
                ConfigureTexture(spec.HeightPath, TextureImporterType.Default, false, false);
                ConfigureTexture(spec.RoughnessPath, TextureImporterType.Default, false, false);
                ConfigureTexture(spec.PackedPath, TextureImporterType.Default, false, true);
                materials.Add(spec, CreateOrUpdateMaterial(spec));
            }

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
                throw new InvalidOperationException("No hay una escena activa cargada.");

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Aplicar materiales de entorno Pueblo de Sombras");
            List<ApplyResult> results = Specs.Select(spec => ApplyCategory(scene, spec, materials[spec])).ToList();
            Undo.CollapseUndoOperations(undoGroup);

            if (results.Any(result => result.AppliedPaths.Count == 0))
                throw new InvalidOperationException("Una categoría no encontró superficies compatibles; se requiere revisión.");

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            WriteReport(scene, results);

            Selection.activeObject = materials[Specs[0]];
            EditorGUIUtility.PingObject(materials[Specs[0]]);
            Debug.Log(
                "[Pueblo de Sombras] Materiales aplicados: " +
                string.Join(", ", results.Select(result => result.Spec.Label + " " + result.AppliedPaths.Count)) +
                ". La escena quedó sin guardar para revisión visual.");
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

    private static Material CreateOrUpdateMaterial(MaterialSpec spec)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró Universal Render Pipeline/Lit.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(spec.MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_PuebloSombras_" + spec.Name };
            AssetDatabase.CreateAsset(material, spec.MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", LoadTexture(spec.BaseColorPath));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", LoadTexture(spec.NormalPath));
        material.SetFloat("_BumpScale", spec.NormalScale);
        material.SetTexture("_MetallicGlossMap", LoadTexture(spec.PackedPath));
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 1.0f);
        material.SetFloat("_SmoothnessTextureChannel", 0.0f);
        material.SetTexture("_OcclusionMap", LoadTexture(spec.AoPath));
        material.SetFloat("_OcclusionStrength", spec.OcclusionStrength);

        Vector2 tiling = new Vector2(spec.Tiling, spec.Tiling);
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

    private static ApplyResult ApplyCategory(Scene scene, MaterialSpec spec, Material material)
    {
        Renderer[] named = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf(spec.HierarchyKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Renderer[] targets = named
            .Where(renderer => renderer.sharedMaterials.Any(item => item != null && item.name == spec.OriginalMaterial))
            .ToArray();

        ApplyResult result = new ApplyResult { Spec = spec, Named = named.Length };
        foreach (Renderer renderer in targets)
        {
            Undo.RecordObject(renderer, "Aplicar " + spec.Label);
            Material[] assigned = renderer.sharedMaterials;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (assigned[index] != null && assigned[index].name == spec.OriginalMaterial)
                    assigned[index] = material;
            }
            renderer.sharedMaterials = assigned;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            result.AppliedPaths.Add(GetHierarchyPath(renderer.transform));
        }
        return result;
    }

    private static void WriteReport(Scene scene, IEnumerable<ApplyResult> results)
    {
        List<string> lines = new List<string>
        {
            "Scene: " + scene.path,
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
        };

        foreach (ApplyResult result in results)
        {
            lines.Add(result.Spec.Label + ":");
            lines.Add("  Material: " + result.Spec.MaterialPath);
            lines.Add("  Named: " + result.Named);
            lines.Add("  Applied: " + result.AppliedPaths.Count);
            lines.Add("  Tiling: " + result.Spec.Tiling);
            lines.AddRange(result.AppliedPaths.Select(path => "  " + path));
            lines.Add(string.Empty);
        }
        File.WriteAllLines(GetMarkerPath(), lines);
    }

    private static string GetMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_EnvironmentMaterials_Applied.txt"));
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
