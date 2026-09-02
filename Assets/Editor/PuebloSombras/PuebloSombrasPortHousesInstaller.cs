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
public static class PuebloSombrasPortHousesInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string OriginalMaterialPath = Root + "/M_PuebloSombras_House_Stucco.mat";
    private const string OriginalMaterialName = "M_PuebloSombras_House_Stucco";
    private const string WoodRoot = Root + "/Textures/PortHouseWood";
    private const string WoodPrefix = WoodRoot + "/PuebloSombras_Port_House_Wood";
    private const string DamageRoot = Root + "/Textures/AttackedStucco";
    private const string DamagePrefix = DamageRoot + "/PuebloSombras_Attacked_Stucco";

    private static readonly string[] PaletteNames = { "Turquoise", "PortBlue", "Sage", "Ochre", "Coral" };
    private static readonly string[] PaletteHex = { "#7FA39D", "#6F8797", "#929C7C", "#B89A65", "#AD746B" };
    private static readonly Vector2[] Offsets =
    {
        new Vector2(0.00f, 0.00f),
        new Vector2(0.23f, 0.17f),
        new Vector2(0.51f, 0.31f),
        new Vector2(0.13f, 0.59f),
        new Vector2(0.68f, 0.44f),
    };

    private static readonly string[] AttackKeywords =
    {
        "CrystalRuin",
        "GlowingCrystalCrater",
        "MagicPortal",
        "FantasyRuinsPortal",
        "FantasyCrystalIsland",
        "PurpleCrystalTree",
        "CrystalCluster",
    };

    static PuebloSombrasPortHousesInstaller()
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
            ConfigureGeneratedTextureSet(WoodPrefix);
            ConfigureGeneratedTextureSet(DamagePrefix);

            Material original = AssetDatabase.LoadAssetAtPath<Material>(OriginalMaterialPath);
            if (original == null)
                throw new InvalidOperationException("No se encontró el material base de las casas.");

            Material[] stuccoColors = CreateStuccoColorMaterials(original);
            Material[] woodColors = CreateGeneratedMaterials(WoodPrefix, "M_PuebloSombras_PortWood", 0.055f, 0.40f, 0.38f, false);
            Material[] attackedColors = CreateGeneratedMaterials(DamagePrefix, "M_PuebloSombras_AttackedStucco", 0.050f, 0.32f, 0.46f, true);
            ApplyDistribution(original, stuccoColors, woodColors, attackedColors);
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureGeneratedTextureSet(string prefix)
    {
        ConfigureTexture(prefix + "_BaseColor_2048.png", TextureImporterType.Default, true, false);
        ConfigureTexture(prefix + "_Normal_2048.png", TextureImporterType.NormalMap, false, false);
        ConfigureTexture(prefix + "_AO_2048.png", TextureImporterType.Default, false, false);
        ConfigureTexture(prefix + "_Height_2048.png", TextureImporterType.Default, false, false);
        ConfigureTexture(prefix + "_Roughness_2048.png", TextureImporterType.Default, false, false);
        ConfigureTexture(prefix + "_MetallicSmoothness_2048.png", TextureImporterType.Default, false, true);
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

    private static Material[] CreateStuccoColorMaterials(Material original)
    {
        Material[] materials = new Material[PaletteNames.Length];
        for (int index = 0; index < materials.Length; index++)
        {
            string materialName = "M_PuebloSombras_PortStucco_" + PaletteNames[index];
            string materialPath = Root + "/" + materialName + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(original) { name = materialName };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.CopyPropertiesFromMaterial(original);
            }

            Color tint = ParseColor(PaletteHex[index]);
            material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);
            EditorUtility.SetDirty(material);
            materials[index] = material;
        }
        return materials;
    }

    private static Material[] CreateGeneratedMaterials(
        string texturePrefix,
        string materialPrefix,
        float tiling,
        float bumpScale,
        float occlusionStrength,
        bool softenTint)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            throw new InvalidOperationException("No se encontró Universal Render Pipeline/Lit.");

        Texture2D baseColor = LoadTexture(texturePrefix + "_BaseColor_2048.png");
        Texture2D normal = LoadTexture(texturePrefix + "_Normal_2048.png");
        Texture2D packed = LoadTexture(texturePrefix + "_MetallicSmoothness_2048.png");
        Texture2D ao = LoadTexture(texturePrefix + "_AO_2048.png");

        Material[] materials = new Material[PaletteNames.Length];
        for (int index = 0; index < materials.Length; index++)
        {
            string materialName = materialPrefix + "_" + PaletteNames[index];
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

            Color tint = ParseColor(PaletteHex[index]);
            if (softenTint)
                tint = Color.Lerp(tint, Color.white, 0.32f);

            material.SetTexture("_BaseMap", baseColor);
            material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", tint);
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", bumpScale);
            material.SetTexture("_MetallicGlossMap", packed);
            material.SetFloat("_Metallic", 0.0f);
            material.SetFloat("_Smoothness", 1.0f);
            material.SetFloat("_SmoothnessTextureChannel", 0.0f);
            material.SetTexture("_OcclusionMap", ao);
            material.SetFloat("_OcclusionStrength", occlusionStrength);

            Vector2 scale = new Vector2(tiling, tiling);
            foreach (string property in new[] { "_BaseMap", "_MainTex", "_BumpMap", "_MetallicGlossMap", "_OcclusionMap" })
            {
                if (!material.HasProperty(property))
                    continue;
                material.SetTextureScale(property, scale);
                material.SetTextureOffset(property, Offsets[index]);
            }

            material.EnableKeyword("_NORMALMAP");
            material.EnableKeyword("_METALLICSPECGLOSSMAP");
            material.EnableKeyword("_OCCLUSIONMAP");
            EditorUtility.SetDirty(material);
            materials[index] = material;
        }
        return materials;
    }

    private static Texture2D LoadTexture(string path)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            throw new InvalidOperationException("No se pudo cargar la textura: " + path);
        return texture;
    }

    private static void ApplyDistribution(
        Material original,
        Material[] stuccoColors,
        Material[] woodColors,
        Material[] attackedColors)
    {
        string markerPath = GetMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada.");

        Renderer[] targets = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Muro básico Viviendas", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(renderer => renderer.sharedMaterials.Any(material => material != null && material.name == OriginalMaterialName))
            .ToArray();

        if (targets.Length == 0)
            throw new InvalidOperationException("No se encontraron fachadas con el material base actual.");

        Dictionary<string, List<Renderer>> groups = targets
            .GroupBy(renderer => GetSpatialKey(renderer.bounds.center))
            .ToDictionary(group => group.Key, group => group.OrderBy(item => GetHierarchyPath(item.transform), StringComparer.Ordinal).ToList());

        List<Vector3> attackSources = Resources.FindObjectsOfTypeAll<Transform>()
            .Where(transform => transform != null && transform.gameObject.scene == scene && !EditorUtility.IsPersistent(transform))
            .Where(transform => AttackKeywords.Any(keyword => transform.name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
            .Select(transform => transform.position)
            .ToList();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Variar casas portuarias Pueblo de Sombras");
        int preservedCream = 0;
        int coloredStucco = 0;
        int wooden = 0;
        int attacked = 0;
        int towerGroups = 0;
        int changedRenderers = 0;
        List<string> groupReport = new List<string>();

        foreach (KeyValuePair<string, List<Renderer>> entry in groups.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            List<Renderer> renderers = entry.Value;
            float minimumY = renderers.Min(renderer => renderer.bounds.center.y);
            float maximumY = renderers.Max(renderer => renderer.bounds.center.y);
            bool isTowerStack = renderers.Count >= 5 && maximumY - minimumY > 18.0f;
            if (isTowerStack)
            {
                towerGroups++;
                continue;
            }

            Vector3 center = new Vector3(
                renderers.Average(renderer => renderer.bounds.center.x),
                renderers.Average(renderer => renderer.bounds.center.y),
                renderers.Average(renderer => renderer.bounds.center.z));
            int hash = PositiveHash(entry.Key);
            int paletteIndex = PositiveHash(entry.Key + "_palette") % PaletteNames.Length;
            float distance = attackSources.Count == 0
                ? float.PositiveInfinity
                : attackSources.Min(source => HorizontalDistance(center, source));

            int attackRoll = hash % 100;
            bool isAttacked = attackSources.Count > 0
                ? distance < 26.0f || (distance < 50.0f && attackRoll < 65) || (distance < 80.0f && attackRoll < 22)
                : attackRoll < 14;

            Material replacement = original;
            string category;
            if (isAttacked)
            {
                replacement = attackedColors[paletteIndex];
                category = "ATACADA";
                attacked++;
            }
            else
            {
                int distribution = PositiveHash(entry.Key + "_distribution") % 100;
                if (distribution < 42)
                {
                    category = "CREMA";
                    preservedCream++;
                }
                else if (distribution < 82)
                {
                    replacement = stuccoColors[(distribution - 42) / 8];
                    category = "REVOQUE " + replacement.name;
                    coloredStucco++;
                }
                else
                {
                    replacement = woodColors[paletteIndex];
                    category = "MADERA " + replacement.name;
                    wooden++;
                }
            }

            if (replacement != original)
                changedRenderers += ReplaceMaterial(renderers, original, replacement);
            groupReport.Add(entry.Key + " | " + category + " | renderers " + renderers.Count + " | distancia ataque " + (float.IsInfinity(distance) ? "N/A" : distance.ToString("0.0")));
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        List<string> report = new List<string>
        {
            "Scene: " + scene.path,
            "Eligible facade renderers: " + targets.Length,
            "House groups: " + groups.Count,
            "Attack sources detected: " + attackSources.Count,
            "Preserved cream groups: " + preservedCream,
            "Colored stucco groups: " + coloredStucco,
            "Wood groups: " + wooden,
            "Attacked groups: " + attacked,
            "Excluded tower stacks: " + towerGroups,
            "Changed renderers: " + changedRenderers,
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "GROUPS",
        };
        report.AddRange(groupReport);
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = woodColors[0];
        EditorGUIUtility.PingObject(woodColors[0]);
        Debug.Log($"[Pueblo de Sombras] Casas portuarias aplicadas: {coloredStucco} grupos con revoque de color, {wooden} de madera y {attacked} atacados. Se preservaron {preservedCream} grupos crema y {towerGroups} pilas de la torre.");
    }

    private static int ReplaceMaterial(List<Renderer> renderers, Material original, Material replacement)
    {
        int changed = 0;
        foreach (Renderer renderer in renderers)
        {
            Material[] assigned = renderer.sharedMaterials;
            bool didChange = false;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (assigned[index] != original && (assigned[index] == null || assigned[index].name != OriginalMaterialName))
                    continue;
                assigned[index] = replacement;
                didChange = true;
            }

            if (!didChange)
                continue;
            Undo.RecordObject(renderer, "Variar fachada portuaria");
            renderer.sharedMaterials = assigned;
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            EditorUtility.SetDirty(renderer);
            changed++;
        }
        return changed;
    }

    private static string GetSpatialKey(Vector3 center)
    {
        int x = Mathf.RoundToInt(center.x / 22.0f);
        int z = Mathf.RoundToInt(center.z / 22.0f);
        return x + "_" + z;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        float x = first.x - second.x;
        float z = first.z - second.z;
        return Mathf.Sqrt(x * x + z * z);
    }

    private static int PositiveHash(string value)
    {
        unchecked
        {
            int hash = 17;
            foreach (char character in value)
                hash = hash * 31 + character;
            return hash == int.MinValue ? int.MaxValue : Mathf.Abs(hash);
        }
    }

    private static Color ParseColor(string html)
    {
        if (!ColorUtility.TryParseHtmlString(html, out Color color))
            throw new InvalidOperationException("Color inválido: " + html);
        return color;
    }

    private static string GetMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_PortHouses_Applied.txt"));
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
