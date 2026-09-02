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
public static class PuebloSombrasSubtleHouseColorsInstaller
{
    private const string Root = "Assets/Escenario/Materials/PuebloSombras";
    private const string OriginalMaterialPath = Root + "/M_PuebloSombras_House_Stucco.mat";
    private const string OriginalMaterialName = "M_PuebloSombras_House_Stucco";

    private static readonly string[] PreviousPrefixes =
    {
        "M_PuebloSombras_PortStucco_",
        "M_PuebloSombras_PortWood_",
        "M_PuebloSombras_AttackedStucco_",
        "M_PuebloSombras_SubtleStucco_",
    };

    private static readonly string[] PaletteNames =
    {
        "MistBlue",
        "PaleSage",
        "DustRose",
        "Sand",
        "SoftAqua",
    };

    private static readonly string[] PaletteHex =
    {
        "#D4DCDB",
        "#D9DCCF",
        "#DED1CC",
        "#DED5C2",
        "#D3DFDA",
    };

    static PuebloSombrasSubtleHouseColorsInstaller()
    {
        if (!File.Exists(GetMarkerPath()))
            EditorApplication.delayCall += InstallAndApply;
    }

    [MenuItem("Pueblo de Sombras/Aplicar fachadas sutiles")]
    public static void ApplyFromMenu()
    {
        InstallAndApply();
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
            Material original = AssetDatabase.LoadAssetAtPath<Material>(OriginalMaterialPath);
            if (original == null)
                throw new InvalidOperationException("No se encontró el material base de las casas.");

            Material[] subtleColors = CreateSubtleMaterials(original);
            ApplySubtleDistribution(original, subtleColors);
            AssetDatabase.SaveAssets();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Material[] CreateSubtleMaterials(Material original)
    {
        Material[] materials = new Material[PaletteNames.Length];
        for (int index = 0; index < materials.Length; index++)
        {
            string materialName = "M_PuebloSombras_SubtleStucco_" + PaletteNames[index];
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

    private static void ApplySubtleDistribution(Material original, Material[] subtleColors)
    {
        string markerPath = GetMarkerPath();
        if (File.Exists(markerPath))
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException("No hay una escena activa cargada.");

        Renderer[] eligible = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .Where(renderer => GetHierarchyPath(renderer.transform).IndexOf("Muro básico Viviendas", StringComparison.OrdinalIgnoreCase) >= 0)
            .Where(renderer => renderer.sharedMaterials.Any(IsHouseMaterial))
            .ToArray();

        if (eligible.Length == 0)
            throw new InvalidOperationException("No se encontraron fachadas para suavizar.");

        Dictionary<string, List<Renderer>> groups = eligible
            .GroupBy(renderer => GetSpatialKey(renderer.bounds.center))
            .ToDictionary(group => group.Key, group => group.ToList());

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Suavizar colores de casas Pueblo de Sombras");
        int creamGroups = 0;
        int pastelGroups = 0;
        int towerGroups = 0;
        int changedRenderers = 0;
        Dictionary<string, int> paletteCounts = PaletteNames.ToDictionary(name => name, _ => 0);

        foreach (KeyValuePair<string, List<Renderer>> entry in groups.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            List<Renderer> renderers = entry.Value;
            float minimumY = renderers.Min(renderer => renderer.bounds.center.y);
            float maximumY = renderers.Max(renderer => renderer.bounds.center.y);
            bool isTowerStack = renderers.Count >= 5 && maximumY - minimumY > 18.0f;

            Material replacement;
            if (isTowerStack)
            {
                replacement = original;
                towerGroups++;
            }
            else
            {
                int distribution = PositiveHash(entry.Key + "_subtle_distribution") % 100;
                if (distribution < 68)
                {
                    replacement = original;
                    creamGroups++;
                }
                else
                {
                    int paletteIndex = PositiveHash(entry.Key + "_subtle_palette") % subtleColors.Length;
                    replacement = subtleColors[paletteIndex];
                    pastelGroups++;
                    paletteCounts[PaletteNames[paletteIndex]]++;
                }
            }

            changedRenderers += ReplaceHouseMaterials(renderers, replacement);
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);

        List<string> report = new List<string>
        {
            "Scene: " + scene.path,
            "Eligible facade renderers: " + eligible.Length,
            "House groups: " + groups.Count,
            "Cream groups: " + creamGroups,
            "Subtle pastel groups: " + pastelGroups,
            "Tower stacks restored to neutral: " + towerGroups,
            "Changed renderers: " + changedRenderers,
            "Previous wood and attacked materials assigned: 0",
            "Saved: false (scene left dirty for visual review)",
            string.Empty,
            "PALETTE",
        };
        report.AddRange(paletteCounts.Select(pair => pair.Key + ": " + pair.Value));
        File.WriteAllLines(markerPath, report);

        Selection.activeObject = subtleColors[0];
        EditorGUIUtility.PingObject(subtleColors[0]);
        Debug.Log($"[Pueblo de Sombras] Fachadas suavizadas: {creamGroups} grupos crema y {pastelGroups} grupos pastel. Se retiraron las asignaciones de madera y daño intenso; la torre quedó neutra.");
    }

    private static bool IsHouseMaterial(Material material)
    {
        if (material == null)
            return false;
        if (material.name == OriginalMaterialName)
            return true;
        return PreviousPrefixes.Any(prefix => material.name.StartsWith(prefix, StringComparison.Ordinal));
    }

    private static int ReplaceHouseMaterials(List<Renderer> renderers, Material replacement)
    {
        int changed = 0;
        foreach (Renderer renderer in renderers)
        {
            Material[] assigned = renderer.sharedMaterials;
            bool didChange = false;
            for (int index = 0; index < assigned.Length; index++)
            {
                if (!IsHouseMaterial(assigned[index]))
                    continue;
                if (assigned[index] == replacement)
                    continue;
                assigned[index] = replacement;
                didChange = true;
            }

            if (!didChange)
                continue;
            Undo.RecordObject(renderer, "Suavizar fachada");
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
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_SubtleHouseColors_Applied.txt"));
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
