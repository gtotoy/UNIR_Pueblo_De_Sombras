using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasEncarnacionHorizonInstaller
{
    private const string OldRootName = "Encarnacion_Horizonte";
    private const string SkyTexturePath = "Assets/Escenario/Materials/PuebloSombras/Textures/Sky/PuebloSombras_ApocalypticSky_Encarnacion_Panorama.png";
    private const string SkyMaterialPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_ApocalypticSky.mat";
    private const string WaterMaterialPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_AWA_Water.mat";
    private const string WaterTextureFolder = "Assets/Escenario/Materials/PuebloSombras/Textures/AWAWater";
    private const string MarkerName = "PuebloSombras_EncarnacionHorizon_Applied.txt";

    private static string MarkerPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", MarkerName);

    static PuebloSombrasEncarnacionHorizonInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Integrar Encarnación en el skybox y mejorar agua")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            ConfigureSkyTexture();

            Shader skyShader = Shader.Find("Skybox/Panoramic");
            Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(SkyTexturePath);
            if (skyShader == null || panorama == null)
            {
                Debug.LogWarning("[PuebloSombras Encarnacion] El panorama o el shader del skybox todavía no están disponibles.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            if (sky == null)
            {
                sky = new Material(skyShader) { name = "M_PuebloSombras_ApocalypticSky" };
                AssetDatabase.CreateAsset(sky, SkyMaterialPath);
            }
            else sky.shader = skyShader;

            sky.SetTexture("_MainTex", panorama);
            SetFloatIfPresent(sky, "_Exposure", 0.85f);
            SetFloatIfPresent(sky, "_Rotation", 0f);
            SetFloatIfPresent(sky, "_Mapping", 1f);
            SetFloatIfPresent(sky, "_ImageType", 0f);
            SetFloatIfPresent(sky, "_MirrorOnBack", 0f);
            SetFloatIfPresent(sky, "_Layout", 0f);
            if (sky.HasProperty("_Tint")) sky.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1f));
            sky.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            sky.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
            EditorUtility.SetDirty(sky);

            GameObject oldHorizon = FindExactObject(scene, OldRootName);
            if (oldHorizon != null) Undo.DestroyObjectImmediate(oldHorizon);

            Material water = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
            float previousTiling = -1f;
            if (water != null)
            {
                if (water.HasProperty("_Tiling")) previousTiling = water.GetFloat("_Tiling");
                water.SetColor("_DeepColor", new Color(0.14f, 0.22f, 0.26f, 1f));
                water.SetColor("_ShallowColor", new Color(0.33f, 0.45f, 0.49f, 1f));
                water.SetColor("_FresnelColor", new Color(0.52f, 0.63f, 0.68f, 1f));
                water.SetFloat("_Opacity", 0.95f);
                water.SetFloat("_Tiling", 3.6f);
                water.SetFloat("_NormalStrength", 0.58f);
                water.SetFloat("_Smoothness", 0.84f);
                EditorUtility.SetDirty(water);
            }

            ImproveWaterTextureImport(WaterTextureFolder + "/PuebloSombras_AWA_Water_BaseColor_2048.png");
            ImproveWaterTextureImport(WaterTextureFolder + "/PuebloSombras_AWA_Water_Normal_2048.png");

            RenderSettings.skybox = sky;
            DynamicGI.UpdateEnvironment();
            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();

            string[] report =
            {
                "Pueblo de Sombras - Encarnación integrada en el skybox",
                "Escena: " + scene.path,
                "Panorama: " + SkyTexturePath,
                "Geometría 3D eliminada: " + (oldHorizon != null),
                "Tiling anterior del agua: " + previousTiling,
                "Tiling nuevo del agua: 3.6",
                "Escena guardada automáticamente: no"
            };
            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = sky;
            EditorGUIUtility.PingObject(sky);
            Debug.Log("[PuebloSombras Encarnacion] Ciudad integrada en el skybox; se eliminó la maqueta 3D.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureSkyTexture()
    {
        TextureImporter importer = AssetImporter.GetAtPath(SkyTexturePath) as TextureImporter;
        if (importer == null) return;
        bool changed = false;
        if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; changed = true; }
        if (!importer.sRGBTexture) { importer.sRGBTexture = true; changed = true; }
        if (importer.wrapModeU != TextureWrapMode.Repeat) { importer.wrapModeU = TextureWrapMode.Repeat; changed = true; }
        if (importer.wrapModeV != TextureWrapMode.Clamp) { importer.wrapModeV = TextureWrapMode.Clamp; changed = true; }
        if (importer.filterMode != FilterMode.Trilinear) { importer.filterMode = FilterMode.Trilinear; changed = true; }
        if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
        if (importer.maxTextureSize != 2048) { importer.maxTextureSize = 2048; changed = true; }
        if (importer.npotScale != TextureImporterNPOTScale.ToNearest) { importer.npotScale = TextureImporterNPOTScale.ToNearest; changed = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (changed) importer.SaveAndReimport();
    }

    private static void ImproveWaterTextureImport(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        bool changed = false;
        if (importer.anisoLevel != 8) { importer.anisoLevel = 8; changed = true; }
        if (Mathf.Abs(importer.mipMapBias + 0.35f) > 0.001f) { importer.mipMapBias = -0.35f; changed = true; }
        if (changed) importer.SaveAndReimport();
    }

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }

    private static GameObject FindExactObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == objectName) return candidate.gameObject;
            }
        }
        return null;
    }
}
