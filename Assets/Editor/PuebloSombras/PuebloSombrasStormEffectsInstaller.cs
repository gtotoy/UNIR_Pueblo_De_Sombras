using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasStormEffectsInstaller
{
    private const string RootName = "Atmosfera_Relampagos_y_Titileo";
    private const string SkyMaterialPath = "Assets/Escenario/Materials/PuebloSombras/M_PuebloSombras_ApocalypticSky.mat";
    private const string BoltMaterialPath = "Assets/Escenario/Materials/PuebloSombras/Particles/M_PuebloSombras_LightningBolt.mat";
    private const string BoltShaderName = "Pueblo de Sombras/Lightning Bolt";
    private const string MarkerName = "PuebloSombras_StormEffects_Applied.txt";

    private static string MarkerPath => Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Library",
        MarkerName);

    static PuebloSombrasStormEffectsInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Aplicar relámpagos y titileo violeta")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
            Shader boltShader = Shader.Find(BoltShaderName);
            if (skybox == null || boltShader == null)
            {
                Debug.LogWarning("[PuebloSombras Storm] El skybox todavía no está disponible; se reintentará.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            GameObject existing = FindExactObject(scene, RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Crear efectos de tormenta");

            GameObject flashObject = new GameObject("Destello_Global");
            flashObject.transform.SetParent(root.transform, false);
            flashObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);
            Light flashLight = flashObject.AddComponent<Light>();
            flashLight.type = LightType.Directional;
            flashLight.color = new Color(0.56f, 0.62f, 0.92f, 1f);
            flashLight.intensity = 0f;
            flashLight.shadows = LightShadows.None;
            flashLight.renderMode = LightRenderMode.ForcePixel;

            Material boltMaterial = CreateLightningMaterial(boltShader);
            GameObject boltObject = new GameObject("Rayo_Visible");
            boltObject.transform.SetParent(root.transform, false);
            LineRenderer boltRenderer = boltObject.AddComponent<LineRenderer>();
            boltRenderer.useWorldSpace = true;
            boltRenderer.positionCount = 0;
            boltRenderer.startWidth = 0.62f;
            boltRenderer.endWidth = 0.14f;
            boltRenderer.numCornerVertices = 2;
            boltRenderer.numCapVertices = 2;
            boltRenderer.textureMode = LineTextureMode.Stretch;
            boltRenderer.alignment = LineAlignment.View;
            boltRenderer.sharedMaterial = boltMaterial;
            boltRenderer.shadowCastingMode = ShadowCastingMode.Off;
            boltRenderer.receiveShadows = false;
            boltRenderer.enabled = false;

            ApocalypticLightningControllerV2 controller = root.AddComponent<ApocalypticLightningControllerV2>();
            controller.lightningLight = flashLight;
            controller.lightningBolt = boltRenderer;
            controller.skyboxSource = skybox;
            controller.interval = new Vector2(8f, 18f);
            controller.firstFlashDelay = new Vector2(2.5f, 5f);
            controller.doubleFlashChance = 0.82f;
            controller.lightningColor = new Color(0.56f, 0.62f, 0.92f, 1f);
            controller.peakIntensity = 1.55f;
            controller.skyExposureBoost = 0.58f;
            controller.ambientBoost = 0.26f;

            List<Light> selectedLights = FindVioletLights(scene);
            for (int i = 0; i < selectedLights.Count; i++)
            {
                Light light = selectedLights[i];
                CorruptionLightFlicker flicker = light.GetComponent<CorruptionLightFlicker>();
                if (flicker == null) flicker = Undo.AddComponent<CorruptionLightFlicker>(light.gameObject);
                Undo.RecordObject(flicker, "Configurar titileo de corrupción");
                flicker.minimumMultiplier = 0.82f;
                flicker.maximumMultiplier = 1.12f;
                flicker.noiseFrequency = 0.38f + (i % 7) * 0.065f;
                flicker.smoothing = 8f;
                flicker.pulseMultiplier = 1.38f;
                flicker.pulseInterval = new Vector2(3.5f + (i % 4) * 0.55f, 9f + (i % 5) * 0.7f);
                flicker.pulseDuration = new Vector2(0.16f, 0.42f);
                EditorUtility.SetDirty(flicker);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            var report = new List<string>
            {
                "Pueblo de Sombras - relámpagos y titileo",
                "Escena: " + scene.path,
                "Primer relámpago: 2.5-5 segundos",
                "Intervalo de relámpagos: 8-18 segundos",
                "Probabilidad de doble destello: 0.82",
                "Luz global de relámpago: " + GetPath(flashLight.transform),
                "Rayo visible: " + GetPath(boltRenderer.transform),
                "Luces violetas con titileo: " + selectedLights.Count,
                "Escena guardada automáticamente: no"
            };
            foreach (Light light in selectedLights)
            {
                report.Add("- " + GetPath(light.transform) + " | intensidad base " + light.intensity + " | color " + light.color);
            }

            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = root;
            Debug.Log("[PuebloSombras Storm] Relámpagos y titileo violeta aplicados. Se reproducen en Play Mode.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Material CreateLightningMaterial(Shader shader)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(BoltMaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_PuebloSombras_LightningBolt" };
            AssetDatabase.CreateAsset(material, BoltMaterialPath);
        }
        else material.shader = shader;

        material.SetColor("_Tint", new Color(1.45f, 1.20f, 2.55f, 1f));
        material.SetFloat("_Opacity", 1.15f);
        material.renderQueue = 3180;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static List<Light> FindVioletLights(Scene scene)
    {
        List<Light> candidates = new List<Light>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == RootName) continue;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (!light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (light.type != LightType.Point && light.type != LightType.Spot) continue;
                if (light.lightmapBakeType == LightmapBakeType.Baked) continue;
                if (light.intensity <= 0.01f || light.range <= 0.5f || !IsViolet(light.color)) continue;
                candidates.Add(light);
            }
        }

        candidates = candidates
            .OrderByDescending(light => light.intensity * Mathf.Max(light.range, 1f) * Mathf.Max(light.color.maxColorComponent, 0.1f))
            .ToList();

        List<Light> selected = new List<Light>();
        foreach (Light candidate in candidates)
        {
            if (selected.Any(existing => Vector3.Distance(existing.transform.position, candidate.transform.position) < 5f)) continue;
            selected.Add(candidate);
            if (selected.Count >= 18) break;
        }
        return selected;
    }

    private static bool IsViolet(Color color)
    {
        Color.RGBToHSV(color, out float hue, out float saturation, out float value);
        bool violetHue = hue >= 0.68f && hue <= 0.95f;
        bool channelBalance = color.b > color.g * 1.12f && color.r > color.g * 0.82f;
        return saturation >= 0.28f && value >= 0.18f && (violetHue || channelBalance);
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
