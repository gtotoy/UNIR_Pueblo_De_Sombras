using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasFullAtmosphereInstaller
{
    private const string SkyMaterialPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_ApocalypticSky.mat";
    private const string FogMaterialPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_GroundFog.mat";
    private const string FogShaderName = "Pueblo de Sombras/Low Ground Fog";
    private const string FogRootName = "Atmosfera_Neblina_Baja";
    private const string MarkerName = "PuebloSombras_FullAtmosphere_Applied.txt";

    private static string MarkerPath => Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Library",
        MarkerName);

    static PuebloSombrasFullAtmosphereInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Reaplicar atmósfera completa")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            Shader fogShader = Shader.Find(FogShaderName);
            if (sky == null || fogShader == null)
            {
                Debug.LogWarning("[PuebloSombras Atmosphere] Los materiales todavía se están importando; se reintentará.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            Material fogMaterial = CreateOrUpdateFogMaterial(fogShader);
            string oldSky = RenderSettings.skybox != null ? RenderSettings.skybox.name : "<ninguno>";
            Color oldFogColor = RenderSettings.fogColor;
            float oldFogDensity = RenderSettings.fogDensity;

            RenderSettings.skybox = sky;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.27f, 0.23f, 0.31f, 1f);
            RenderSettings.fogDensity = 0.003f;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = 0.65f;
            RenderSettings.reflectionIntensity = 0.35f;

            Camera sceneCamera = FindExactObject(scene, "Camera")?.GetComponent<Camera>();
            if (sceneCamera != null)
            {
                Undo.RecordObject(sceneCamera, "Restaurar cielo apocalíptico");
                sceneCamera.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(sceneCamera);
            }

            Light directional = FindDirectionalLight(scene);
            string directionalReport = "No se encontró luz direccional";
            if (directional != null)
            {
                Color oldColor = directional.color;
                float oldIntensity = directional.intensity;
                Undo.RecordObject(directional, "Acompañar iluminación apocalíptica");
                directional.color = new Color(0.58f, 0.54f, 0.68f, 1f);
                directional.intensity = Mathf.Min(directional.intensity, 0.65f);
                EditorUtility.SetDirty(directional);
                directionalReport = $"{GetPath(directional.transform)} | color {oldColor} -> {directional.color} | intensidad {oldIntensity} -> {directional.intensity}";
            }

            Bounds cityBounds = CalculateCityBounds(scene);
            GameObject fogRoot = RebuildFogLayers(scene, cityBounds, fogMaterial);

            DynamicGI.UpdateEnvironment();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();

            var report = new List<string>
            {
                "Pueblo de Sombras - atmósfera completa",
                "Escena: " + scene.path,
                "Skybox anterior: " + oldSky,
                "Skybox actual: " + sky.name,
                "Fog anterior: " + oldFogColor + " | densidad " + oldFogDensity,
                "Fog actual: " + RenderSettings.fogColor + " | modo " + RenderSettings.fogMode + " | densidad " + RenderSettings.fogDensity,
                "Ambiente: 0.65 | reflejos: 0.35",
                "Luz direccional: " + directionalReport,
                "Bounds usados para neblina baja: centro " + cityBounds.center + " | tamaño " + cityBounds.size,
                "Capas de neblina baja: " + fogRoot.transform.childCount,
                "Escena guardada automáticamente: no"
            };

            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = fogRoot;
            Debug.Log("[PuebloSombras Atmosphere] Fondo, fog global y neblina baja aplicados. La escena quedó sin guardar para revisión.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Material CreateOrUpdateFogMaterial(Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_PuebloSombras_GroundFog" };
            AssetDatabase.CreateAsset(material, FogMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_FogColor", new Color(0.31f, 0.26f, 0.36f, 1f));
        material.SetFloat("_Opacity", 0.135f);
        material.SetFloat("_NoiseScale", 0.018f);
        material.SetVector("_Speed", new Vector4(0.10f, 0.06f, 0f, 0f));
        material.SetFloat("_EdgeFeather", 0.22f);
        material.SetFloat("_SoftIntersection", 5f);
        material.renderQueue = 3080;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static GameObject RebuildFogLayers(Scene scene, Bounds bounds, Material material)
    {
        GameObject existing = FindExactObject(scene, FogRootName);
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        GameObject root = new GameObject(FogRootName);
        Undo.RegisterCreatedObjectUndo(root, "Crear neblina baja");

        float width = Mathf.Clamp(bounds.size.x * 0.62f, 180f, 360f);
        float depth = Mathf.Clamp(bounds.size.z * 0.62f, 160f, 340f);
        Vector2[] normalizedOffsets =
        {
            new Vector2(-0.34f,  0.28f),
            new Vector2( 0.00f,  0.31f),
            new Vector2( 0.35f,  0.22f),
            new Vector2(-0.40f,  0.00f),
            new Vector2(-0.04f,  0.02f),
            new Vector2( 0.38f, -0.04f),
            new Vector2(-0.28f, -0.30f),
            new Vector2( 0.08f, -0.31f),
            new Vector2( 0.39f, -0.28f)
        };

        for (int i = 0; i < normalizedOffsets.Length; i++)
        {
            GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Plane);
            patch.name = $"Neblina_Baja_{i + 1:00}";
            Undo.RegisterCreatedObjectUndo(patch, "Crear capa de neblina");
            patch.transform.SetParent(root.transform, false);

            float y = 2.1f + (i % 3) * 0.75f;
            float x = bounds.center.x + normalizedOffsets[i].x * width;
            float z = bounds.center.z + normalizedOffsets[i].y * depth;
            patch.transform.position = new Vector3(x, y, z);

            float patchWidth = width * (0.50f + (i % 2) * 0.08f);
            float patchDepth = depth * (0.43f + ((i + 1) % 3) * 0.04f);
            patch.transform.localScale = new Vector3(patchWidth / 10f, 1f, patchDepth / 10f);
            patch.transform.rotation = Quaternion.Euler(0f, i * 17f, 0f);

            Collider collider = patch.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.DestroyImmediate(collider);

            MeshRenderer renderer = patch.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        return root;
    }

    private static Bounds CalculateCityBounds(Scene scene)
    {
        bool initialized = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 200f);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == FogRootName || root.name == "AWA") continue;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds candidate = renderer.bounds;
                if (!renderer.enabled || candidate.size.x <= 0.01f || candidate.size.z <= 0.01f) continue;
                if (candidate.size.x > 420f || candidate.size.z > 420f) continue;
                if (!initialized) { bounds = candidate; initialized = true; }
                else bounds.Encapsulate(candidate);
            }
        }

        if (!initialized)
        {
            GameObject awa = FindExactObject(scene, "AWA");
            if (awa != null) bounds = new Bounds(awa.transform.position, new Vector3(300f, 20f, 280f));
        }

        bounds.size = new Vector3(
            Mathf.Clamp(bounds.size.x, 220f, 560f),
            bounds.size.y,
            Mathf.Clamp(bounds.size.z, 200f, 520f));
        return bounds;
    }

    private static Light FindDirectionalLight(Scene scene)
    {
        GameObject exact = FindExactObject(scene, "Directional Light");
        Light exactLight = exact != null ? exact.GetComponent<Light>() : null;
        if (exactLight != null && exactLight.type == LightType.Directional) return exactLight;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional) return light;
            }
        }
        return null;
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

    private static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }
}
