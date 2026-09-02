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
public static class PuebloSombrasSidewalkInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string TextureRoot = Root + "/Textures";
    private const string MaterialPath = Root + "/M_PuebloSombras_Sidewalk.mat";
    private const string BaseColorPath = TextureRoot + "/PuebloSombras_Sidewalk_BaseColor_2048.png";
    private const string NormalPath = TextureRoot + "/PuebloSombras_Sidewalk_Normal_2048.png";
    private const string RoughnessPath = TextureRoot + "/PuebloSombras_Sidewalk_Roughness_2048.png";
    private const string PackedPath = TextureRoot + "/PuebloSombras_Sidewalk_MetallicSmoothness_2048.png";

    [Serializable]
    private sealed class ScanReport
    {
        public string scene;
        public string generatedMaterial;
        public string generatedAtUtc;
        public RendererInfo[] renderers;
    }

    [Serializable]
    private sealed class RendererInfo
    {
        public int score;
        public string hierarchyPath;
        public string rendererType;
        public string mesh;
        public bool active;
        public float centerX;
        public float centerY;
        public float centerZ;
        public float sizeX;
        public float sizeY;
        public float sizeZ;
        public MaterialInfo[] materials;
    }

    [Serializable]
    private sealed class MaterialInfo
    {
        public string name;
        public string assetPath;
        public string shader;
        public float colorR;
        public float colorG;
        public float colorB;
        public float colorA;
    }

    static PuebloSombrasSidewalkInstaller()
    {
        if (!File.Exists(GetAppliedMarkerPath()))
            EditorApplication.delayCall += InstallAndScan;
    }

    private static void InstallAndScan()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += InstallAndScan;
            return;
        }

        try
        {
            ConfigureTexture(BaseColorPath, TextureImporterType.Default, true);
            ConfigureTexture(NormalPath, TextureImporterType.NormalMap, false);
            ConfigureTexture(RoughnessPath, TextureImporterType.Default, false);
            ConfigureTexture(PackedPath, TextureImporterType.Default, false);

            Material material = CreateOrUpdateMaterial();
            WriteRendererReport(material);
            ApplyDetectedSidewalksOnce(material);
            AssetDatabase.SaveAssets();
            Debug.Log("[Pueblo de Sombras] Material de vereda instalado y superficies analizadas.");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureTexture(string path, TextureImporterType type, bool srgb)
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
        if (path == PackedPath)
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.SaveAndReimport();
    }

    private static Material CreateOrUpdateMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró el shader Universal Render Pipeline/Lit.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_PuebloSombras_Sidewalk" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        Texture2D packed = AssetDatabase.LoadAssetAtPath<Texture2D>(PackedPath);

        material.SetTexture("_BaseMap", baseColor);
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 0.24f);
        material.SetTexture("_MetallicGlossMap", packed);
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 1.0f);
        material.SetFloat("_SmoothnessTextureChannel", 0.0f);
        material.SetTextureScale("_BaseMap", Vector2.one);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void WriteRendererReport(Material generatedMaterial)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada para analizar.");

        RendererInfo[] renderers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Select(CreateRendererInfo)
            .OrderByDescending(info => info.score)
            .ThenByDescending(info => info.sizeX * info.sizeZ)
            .ToArray();

        ScanReport report = new ScanReport
        {
            scene = scene.path,
            generatedMaterial = AssetDatabase.GetAssetPath(generatedMaterial),
            generatedAtUtc = DateTime.UtcNow.ToString("O"),
            renderers = renderers,
        };

        string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_Sidewalk_Renderers.json"));
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
    }

    private static void ApplyDetectedSidewalksOnce(Material generatedMaterial)
    {
        string markerPath = GetAppliedMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada para aplicar la vereda.");

        string[] acceptedOriginalMaterials = { "Material.006", "Material.008" };
        Renderer[] namedSidewalks = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Suelo Veredas General", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        Renderer[] targets = namedSidewalks
            .Where(renderer => renderer.sharedMaterials.Any(item => item != null && acceptedOriginalMaterials.Contains(item.name)))
            .ToArray();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Aplicar material Pueblo de Sombras a veredas");
        List<string> appliedPaths = new List<string>();
        foreach (Renderer renderer in targets)
        {
            Undo.RecordObject(renderer, "Aplicar material de vereda");
            Material[] assigned = renderer.sharedMaterials;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (assigned[index] != null && acceptedOriginalMaterials.Contains(assigned[index].name))
                    assigned[index] = generatedMaterial;
            }
            renderer.sharedMaterials = assigned;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            appliedPaths.Add(GetHierarchyPath(renderer.transform));
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        string[] skippedPaths = namedSidewalks.Except(targets).Select(renderer => GetHierarchyPath(renderer.transform)).ToArray();
        List<string> report = new List<string>
        {
            "Scene: " + scene.path,
            "Material: " + MaterialPath,
            "Applied: " + appliedPaths.Count,
            "Skipped: " + skippedPaths.Length,
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "APPLIED",
        };
        report.AddRange(appliedPaths);
        report.Add(string.Empty);
        report.Add("SKIPPED");
        report.AddRange(skippedPaths);
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = generatedMaterial;
        EditorGUIUtility.PingObject(generatedMaterial);
        Debug.Log($"[Pueblo de Sombras] Material aplicado a {appliedPaths.Count} veredas. {skippedPaths.Length} superficies anómalas se conservaron sin cambios. La escena quedó sin guardar para revisión visual.");
    }

    private static string GetAppliedMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_Sidewalk_Applied.txt"));
    }

    private static RendererInfo CreateRendererInfo(Renderer renderer)
    {
        Bounds bounds = renderer.bounds;
        string hierarchyPath = GetHierarchyPath(renderer.transform);
        string mesh = string.Empty;
        MeshFilter filter = renderer.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
            mesh = filter.sharedMesh.name;
        else if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            mesh = skinned.sharedMesh.name;

        MaterialInfo[] materials = renderer.sharedMaterials
            .Where(material => material != null)
            .Select(CreateMaterialInfo)
            .ToArray();

        return new RendererInfo
        {
            score = ScoreCandidate(renderer, hierarchyPath, bounds, materials),
            hierarchyPath = hierarchyPath,
            rendererType = renderer.GetType().Name,
            mesh = mesh,
            active = renderer.gameObject.activeInHierarchy,
            centerX = bounds.center.x,
            centerY = bounds.center.y,
            centerZ = bounds.center.z,
            sizeX = bounds.size.x,
            sizeY = bounds.size.y,
            sizeZ = bounds.size.z,
            materials = materials,
        };
    }

    private static MaterialInfo CreateMaterialInfo(Material material)
    {
        Color color = Color.white;
        if (material.HasProperty("_BaseColor"))
            color = material.GetColor("_BaseColor");
        else if (material.HasProperty("_Color"))
            color = material.GetColor("_Color");

        return new MaterialInfo
        {
            name = material.name,
            assetPath = AssetDatabase.GetAssetPath(material),
            shader = material.shader != null ? material.shader.name : string.Empty,
            colorR = color.r,
            colorG = color.g,
            colorB = color.b,
            colorA = color.a,
        };
    }

    private static int ScoreCandidate(Renderer renderer, string hierarchyPath, Bounds bounds, MaterialInfo[] materials)
    {
        int score = 0;
        string searchable = (hierarchyPath + " " + renderer.name + " " + string.Join(" ", materials.Select(item => item.name))).ToLowerInvariant();
        string[] exactKeywords = { "vereda", "acera", "sidewalk", "pavimento", "pavement", "costanera", "paseo" };
        string[] broadKeywords = { "calle", "street", "road", "suelo", "ground", "floor" };
        if (exactKeywords.Any(searchable.Contains))
            score += 200;
        if (broadKeywords.Any(searchable.Contains))
            score += 70;

        float horizontalArea = bounds.size.x * bounds.size.z;
        float longestHorizontal = Mathf.Max(bounds.size.x, bounds.size.z);
        if (horizontalArea > 100.0f)
            score += 35;
        if (horizontalArea > 1000.0f)
            score += 35;
        if (longestHorizontal > 10.0f && bounds.size.y < longestHorizontal * 0.18f)
            score += 30;

        Color target = new Color(0.60f, 0.51f, 0.51f, 1.0f);
        foreach (MaterialInfo item in materials)
        {
            float distance = Vector3.Distance(
                new Vector3(item.colorR, item.colorG, item.colorB),
                new Vector3(target.r, target.g, target.b));
            if (distance < 0.22f)
                score += 45;
            else if (distance < 0.35f)
                score += 20;
        }

        return score;
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

    [MenuItem("Pueblo de Sombras/Aplicar material de vereda a la selección")]
    private static void ApplyToSelection()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Debug.LogError("El material de vereda todavía no está instalado.");
            return;
        }

        Renderer[] renderers = Selection.gameObjects
            .SelectMany(gameObject => gameObject.GetComponentsInChildren<Renderer>(true))
            .Distinct()
            .ToArray();
        foreach (Renderer renderer in renderers)
        {
            Undo.RecordObject(renderer, "Aplicar material de vereda");
            Material[] assigned = renderer.sharedMaterials;
            for (int index = 0; index < assigned.Length; index++)
                assigned[index] = material;
            renderer.sharedMaterials = assigned;
            EditorUtility.SetDirty(renderer);
        }

        Debug.Log($"[Pueblo de Sombras] Material aplicado a {renderers.Length} renderer(s) seleccionados.");
    }
}
#endif
