using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasApocalypticSkyInstaller
{
    private const string TexturePath = "Assets/Escenario/Materials/PuebloSombras/Textures/Sky/PuebloSombras_ApocalypticSky_Encarnacion_Panorama.png";
    private const string OldHorizonRootName = "Encarnacion_Horizonte";
    private const string MaterialFolder = "Assets/Escenario/Materials/PuebloSombras";
    private const string MaterialPath = MaterialFolder + "/M_PuebloSombras_ApocalypticSky.mat";
    private const string MarkerName = "PuebloSombras_ApocalypticSky_Applied.txt";

    private static string MarkerPath => Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Library",
        MarkerName);

    static PuebloSombrasApocalypticSkyInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Aplicar cielo apocalíptico")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            ConfigureTextureImporter();

            Shader shader = Shader.Find("Skybox/Panoramic");
            if (shader == null)
            {
                Debug.LogWarning("[PuebloSombras Sky] Unity todavía no cargó Skybox/Panoramic; se reintentará.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (panorama == null)
            {
                Debug.LogError("[PuebloSombras Sky] No se pudo cargar el panorama: " + TexturePath);
                return;
            }

            EnsureFolder(MaterialFolder);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "M_PuebloSombras_ApocalypticSky" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetTexture("_MainTex", panorama);
            SetFloatIfPresent(material, "_Exposure", 0.85f);
            SetFloatIfPresent(material, "_Rotation", 0f);
            SetFloatIfPresent(material, "_Mapping", 1f);
            SetFloatIfPresent(material, "_ImageType", 0f);
            SetFloatIfPresent(material, "_MirrorOnBack", 0f);
            SetFloatIfPresent(material, "_Layout", 0f);
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1f));
            material.EnableKeyword("_MAPPING_LATITUDE_LONGITUDE_LAYOUT");
            material.DisableKeyword("_MAPPING_6_FRAMES_LAYOUT");
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();

            string oldSky = RenderSettings.skybox != null ? RenderSettings.skybox.name : "<ninguno>";
            Color oldFog = RenderSettings.fogColor;
            float oldFogDensity = RenderSettings.fogDensity;
            float oldAmbientIntensity = RenderSettings.ambientIntensity;
            float oldReflectionIntensity = RenderSettings.reflectionIntensity;

            RenderSettings.skybox = material;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.34f, 0.29f, 0.38f, 1f);
            RenderSettings.fogDensity = 0.0011f;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.85f;
            RenderSettings.reflectionIntensity = 0.55f;

            GameObject oldHorizon = FindExactObject(SceneManager.GetActiveScene(), OldHorizonRootName);
            if (oldHorizon != null) Undo.DestroyObjectImmediate(oldHorizon);

            Camera sceneCamera = FindExactObject(SceneManager.GetActiveScene(), "Camera")?.GetComponent<Camera>();
            if (sceneCamera != null && sceneCamera.clearFlags != CameraClearFlags.Skybox)
            {
                Undo.RecordObject(sceneCamera, "Usar cielo apocalíptico");
                sceneCamera.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(sceneCamera);
            }

            DynamicGI.UpdateEnvironment();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            SceneView.RepaintAll();

            var report = new List<string>
            {
                "Pueblo de Sombras - aplicación de cielo apocalíptico",
                "Escena: " + SceneManager.GetActiveScene().path,
                "Material nuevo: " + MaterialPath,
                "Panorama: " + TexturePath,
                "Skybox anterior: " + oldSky,
                "Fog anterior: " + oldFog + " | densidad: " + oldFogDensity,
                "Fog nuevo: " + RenderSettings.fogColor + " | densidad: " + RenderSettings.fogDensity,
                "Intensidad ambiente: " + oldAmbientIntensity + " -> " + RenderSettings.ambientIntensity,
                "Reflejos ambientales: " + oldReflectionIntensity + " -> " + RenderSettings.reflectionIntensity,
                "Cámara en modo Skybox: " + (sceneCamera != null && sceneCamera.clearFlags == CameraClearFlags.Skybox),
                "Escena guardada automáticamente: no"
            };

            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = material;
            EditorGUIUtility.PingObject(material);
            Debug.Log("[PuebloSombras Sky] Cielo apocalíptico aplicado. La escena quedó sin guardar para revisión.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureTextureImporter()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
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

    private static void SetFloatIfPresent(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static GameObject FindExactObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in transforms)
            {
                if (candidate.name == objectName) return candidate.gameObject;
            }
        }
        return null;
    }
}
