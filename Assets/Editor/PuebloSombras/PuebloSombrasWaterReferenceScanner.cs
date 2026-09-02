#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasWaterReferenceScanner
{
    private static readonly string[] ReferenceKeywords =
    {
        "FantasyCrystalIsland",
        "GlowingCrystalCrater",
        "CrystalRuinPlatform",
    };

    static PuebloSombrasWaterReferenceScanner()
    {
        EditorApplication.delayCall += Scan;
    }

    [MenuItem("Pueblo de Sombras/Escanear referencias de agua")]
    public static void Scan()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Scan;
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Renderer[] renderers = Resources.FindObjectsOfTypeAll<Renderer>()
            .Where(renderer => renderer != null && renderer.gameObject.scene == scene && !EditorUtility.IsPersistent(renderer))
            .ToArray();

        Renderer[] awa = renderers
            .Where(renderer => GetHierarchyPath(renderer.transform).Split('/').Any(part => part.Equals("AWA", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Renderer[] references = renderers
            .Where(renderer => ReferenceKeywords.Any(keyword => GetHierarchyPath(renderer.transform).IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
            .ToArray();

        Material[] materials = references
            .SelectMany(renderer => renderer.sharedMaterials)
            .Where(material => material != null)
            .Distinct()
            .OrderBy(material => material.name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        StringBuilder report = new StringBuilder();
        report.AppendLine("Scene: " + scene.path);
        report.AppendLine("AWA renderers: " + awa.Length);
        foreach (Renderer renderer in awa)
        {
            report.AppendLine("  " + GetHierarchyPath(renderer.transform));
            report.AppendLine("  Bounds: " + renderer.bounds.size);
            foreach (Material material in renderer.sharedMaterials.Where(item => item != null))
                report.AppendLine("  Material: " + DescribeMaterialHeader(material));
        }

        report.AppendLine();
        report.AppendLine("Reference renderers: " + references.Length);
        report.AppendLine("Unique reference materials: " + materials.Length);
        report.AppendLine();

        foreach (Material material in materials)
        {
            report.AppendLine("MATERIAL " + DescribeMaterialHeader(material));
            foreach (string property in new[]
            {
                "_BaseColor", "_Color", "_ShallowColor", "_DeepColor", "_FoamColor", "_EmissionColor",
            })
            {
                if (material.HasProperty(property))
                    report.AppendLine("  Color " + property + " = " + material.GetColor(property));
            }

            foreach (string property in new[]
            {
                "_Surface", "_Blend", "_Smoothness", "_Metallic", "_Alpha", "_Opacity", "_Cutoff",
                "_BumpScale", "_Distortion", "_WaveSpeed", "_FlowSpeed", "_ScrollSpeed", "_FoamStrength",
            })
            {
                if (material.HasProperty(property))
                    report.AppendLine("  Float " + property + " = " + material.GetFloat(property));
            }

            foreach (string textureProperty in material.GetTexturePropertyNames())
            {
                Texture texture = material.GetTexture(textureProperty);
                if (texture == null)
                    continue;
                report.AppendLine(
                    "  Texture " + textureProperty + " = " + texture.name +
                    " | " + AssetDatabase.GetAssetPath(texture) +
                    " | scale " + material.GetTextureScale(textureProperty) +
                    " | offset " + material.GetTextureOffset(textureProperty));
            }
            report.AppendLine();
        }

        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_WaterReferenceScan.txt"));
        File.WriteAllText(path, report.ToString());
        Debug.Log($"[Pueblo de Sombras] Referencias de agua escaneadas: {materials.Length} materiales en {references.Length} renderers.");
    }

    private static string DescribeMaterialHeader(Material material)
    {
        return material.name + " | " + AssetDatabase.GetAssetPath(material) + " | shader " +
               (material.shader == null ? "N/A" : material.shader.name) + " | queue " + material.renderQueue;
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
