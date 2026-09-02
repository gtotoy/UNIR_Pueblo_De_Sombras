#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PuebloSombrasMaterialScaleFix
{
    private const string SidewalkPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_Sidewalk.mat";
    private const string WallPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_VegetatedBasaltWall.mat";
    private static readonly Vector2 NewTiling = new Vector2(0.05f, 0.05f);

    static PuebloSombrasMaterialScaleFix()
    {
        if (!File.Exists(GetMarkerPath()))
            EditorApplication.delayCall += ApplyScale;
    }

    private static void ApplyScale()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ApplyScale;
            return;
        }

        try
        {
            Material sidewalk = LoadMaterial(SidewalkPath);
            Material wall = LoadMaterial(WallPath);
            ConfigureTiling(sidewalk);
            ConfigureTiling(wall);
            AssetDatabase.SaveAssets();

            File.WriteAllLines(GetMarkerPath(), new[]
            {
                "Sidewalk: " + SidewalkPath,
                "Wall: " + WallPath,
                "Tiling: 0.05 x 0.05",
                "Visual size: 20x larger than original",
            });

            Selection.activeObject = wall;
            EditorGUIUtility.PingObject(wall);
            Debug.Log("[Pueblo de Sombras] Escala corregida: muro y vereda ahora se ven 20 veces más grandes que el original (tiling 0.05 x 0.05).");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Material LoadMaterial(string path)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new InvalidOperationException("No se encontró el material: " + path);
        return material;
    }

    private static void ConfigureTiling(Material material)
    {
        string[] textureProperties =
        {
            "_BaseMap",
            "_MainTex",
            "_BumpMap",
            "_MetallicGlossMap",
            "_OcclusionMap",
        };

        Undo.RecordObject(material, "Agrandar textura Pueblo de Sombras");
        foreach (string property in textureProperties)
        {
            if (material.HasProperty(property))
            {
                material.SetTextureScale(property, NewTiling);
                material.SetTextureOffset(property, Vector2.zero);
            }
        }
        EditorUtility.SetDirty(material);
    }

    private static string GetMarkerPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PuebloSombras_MaterialScale_005_Applied.txt"));
    }
}
#endif
