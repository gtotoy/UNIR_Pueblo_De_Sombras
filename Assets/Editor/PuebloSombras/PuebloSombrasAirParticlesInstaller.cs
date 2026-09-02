using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasAirParticlesInstaller
{
    // Visibility pass v3: tuned for the wide gameplay camera.
    private const string ShaderName = "Pueblo de Sombras/Air Particles";
    private const string MaterialFolder = "Assets/Escenario/Materials/PuebloSombras/Particles";
    private const string VioletMaterialPath = MaterialFolder + "/M_PuebloSombras_VioletMotes.mat";
    private const string AshMaterialPath = MaterialFolder + "/M_PuebloSombras_AshParticles.mat";
    private const string RootName = "Atmosfera_Particulas_Aereas";
    private const string MarkerName = "PuebloSombras_AirParticles_Applied.txt";

    private static string MarkerPath => Path.Combine(
        Directory.GetParent(Application.dataPath).FullName,
        "Library",
        MarkerName);

    static PuebloSombrasAirParticlesInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Aplicar partículas violetas y ceniza")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning("[PuebloSombras Air] El shader todavía se está importando; se reintentará.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            EnsureFolder(MaterialFolder);
            Material violetMaterial = CreateVioletMaterial(shader);
            Material ashMaterial = CreateAshMaterial(shader);
            Scene scene = SceneManager.GetActiveScene();

            GameObject existing = FindExactObject(scene, RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Crear partículas ambientales");

            GameObject violetRoot = new GameObject("Motas_Violetas");
            violetRoot.transform.SetParent(root.transform, false);
            GameObject ashRoot = new GameObject("Ceniza_Ambiental");
            ashRoot.transform.SetParent(root.transform, false);

            List<Light> violetLights = FindVioletLights(scene);
            for (int i = 0; i < violetLights.Count; i++)
            {
                CreateVioletSystem(violetRoot.transform, violetLights[i], violetMaterial, i);
            }

            Bounds cityBounds = CalculateCityBounds(scene);
            CreateAshSystems(ashRoot.transform, cityBounds, ashMaterial);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();

            var report = new List<string>
            {
                "Pueblo de Sombras - partículas ambientales",
                "Escena: " + scene.path,
                "Luces violetas detectadas: " + violetLights.Count,
                "Sistemas de motas violetas: " + violetRoot.transform.childCount,
                "Sistemas de ceniza: " + ashRoot.transform.childCount,
                "Bounds de ceniza: centro " + cityBounds.center + " | tamaño " + cityBounds.size,
                "Material violeta: " + VioletMaterialPath,
                "Material ceniza: " + AshMaterialPath,
                "Escena guardada automáticamente: no"
            };
            foreach (Light light in violetLights)
            {
                report.Add("- " + GetPath(light.transform) + " | color " + light.color + " | intensidad " + light.intensity + " | rango " + light.range);
            }

            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = root;
            foreach (ParticleSystem system in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Simulate(4f, true, true, false);
                system.Play(true);
            }
            Debug.Log("[PuebloSombras Air] Motas violetas y ceniza aplicadas. La escena quedó sin guardar para revisión.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static Material CreateVioletMaterial(Shader shader)
    {
        Material material = LoadOrCreateMaterial(VioletMaterialPath, shader, "M_PuebloSombras_VioletMotes");
        material.SetColor("_Tint", new Color(0.93f, 0.28f, 1.35f, 0.92f));
        material.SetFloat("_Opacity", 1.15f);
        material.SetFloat("_Shape", 0f);
        material.SetFloat("_SoftDistance", 0.7f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.renderQueue = 3120;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Material CreateAshMaterial(Shader shader)
    {
        Material material = LoadOrCreateMaterial(AshMaterialPath, shader, "M_PuebloSombras_AshParticles");
        material.SetColor("_Tint", new Color(0.64f, 0.60f, 0.67f, 0.84f));
        material.SetFloat("_Opacity", 0.98f);
        material.SetFloat("_Shape", 1f);
        material.SetFloat("_SoftDistance", 1.3f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.renderQueue = 3110;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static Material LoadOrCreateMaterial(string path, Shader shader, string materialName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = materialName };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.shader = shader;
        return material;
    }

    private static void CreateVioletSystem(Transform parent, Light light, Material material, int index)
    {
        GameObject particleObject = new GameObject($"Motas_Violetas_{index + 1:00}_{light.name}");
        particleObject.transform.SetParent(parent, false);
        particleObject.transform.position = light.transform.position;
        ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();

        ParticleSystem.MainModule main = system.main;
        main.duration = 6f;
        main.loop = true;
        main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f, 6.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.04f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.42f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.72f, 0.35f, 1f, 0.55f),
            new Color(1f, 0.58f, 1f, 0.92f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(12f, 20f);

        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = Mathf.Clamp(light.range * 0.22f, 0.8f, 3.4f);
        shape.radiusThickness = 1f;

        ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.10f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.08f, 0.34f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.10f);

        ParticleSystem.NoiseModule noise = system.noise;
        noise.enabled = true;
        noise.quality = ParticleSystemNoiseQuality.Medium;
        noise.strength = new ParticleSystem.MinMaxCurve(0.10f, 0.26f);
        noise.frequency = 0.42f;
        noise.scrollSpeed = 0.10f;
        noise.damping = true;
        noise.octaveCount = 2;

        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.92f, 0.72f, 1f), 0.55f),
                new GradientColorKey(new Color(0.70f, 0.38f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.18f),
                new GradientAlphaKey(0.65f, 0.72f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = new ParticleSystem.MinMaxGradient(gradient);

        renderer.sharedMaterial = material;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.minParticleSize = 0f;
        renderer.maxParticleSize = 0.050f;
    }

    private static void CreateAshSystems(Transform parent, Bounds bounds, Material material)
    {
        float tileWidth = Mathf.Clamp(bounds.size.x * 0.58f, 150f, 300f);
        float tileDepth = Mathf.Clamp(bounds.size.z * 0.58f, 150f, 290f);
        Vector2[] offsets =
        {
            new Vector2(-0.24f,  0.24f),
            new Vector2( 0.24f,  0.24f),
            new Vector2(-0.24f, -0.24f),
            new Vector2( 0.24f, -0.24f)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            GameObject particleObject = new GameObject($"Ceniza_Ambiental_{i + 1:00}");
            particleObject.transform.SetParent(parent, false);
            particleObject.transform.position = new Vector3(
                bounds.center.x + offsets[i].x * bounds.size.x,
                22f,
                bounds.center.z + offsets[i].y * bounds.size.z);

            ParticleSystem system = particleObject.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();

            ParticleSystem.MainModule main = system.main;
            main.duration = 8f;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(18f, 32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f, 0.10f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.55f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.52f, 0.49f, 0.55f, 0.30f),
                new Color(0.78f, 0.74f, 0.78f, 0.64f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 1200;
            main.playOnAwake = true;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(80f, 115f);

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(tileWidth, 28f, tileDepth);

            ParticleSystem.VelocityOverLifetimeModule velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.72f, -0.32f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.10f);

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.quality = ParticleSystemNoiseQuality.Low;
            noise.strength = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            noise.frequency = 0.20f;
            noise.scrollSpeed = 0.06f;
            noise.damping = true;
            noise.octaveCount = 1;

            ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.62f, 0.59f, 0.64f), 0f),
                    new GradientColorKey(new Color(0.43f, 0.40f, 0.46f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.78f, 0.12f),
                    new GradientAlphaKey(0.62f, 0.82f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = new ParticleSystem.MinMaxGradient(gradient);

            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = 0.050f;
        }
    }

    private static List<Light> FindVioletLights(Scene scene)
    {
        List<Light> candidates = new List<Light>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (!light.enabled || !light.gameObject.activeInHierarchy) continue;
                if (light.type != LightType.Point && light.type != LightType.Spot) continue;
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
            bool tooClose = selected.Any(existing => Vector3.Distance(existing.transform.position, candidate.transform.position) < 3.2f);
            if (tooClose) continue;
            selected.Add(candidate);
            if (selected.Count >= 36) break;
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

    private static Bounds CalculateCityBounds(Scene scene)
    {
        bool initialized = false;
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(420f, 60f, 400f));
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "AWA" || root.name.StartsWith("Atmosfera_", StringComparison.Ordinal)) continue;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Bounds candidate = renderer.bounds;
                if (!renderer.enabled || candidate.size.x <= 0.01f || candidate.size.z <= 0.01f) continue;
                if (candidate.size.x > 420f || candidate.size.z > 420f) continue;
                if (!initialized) { bounds = candidate; initialized = true; }
                else bounds.Encapsulate(candidate);
            }
        }

        bounds.size = new Vector3(
            Mathf.Clamp(bounds.size.x, 240f, 540f),
            Mathf.Clamp(bounds.size.y, 40f, 120f),
            Mathf.Clamp(bounds.size.z, 220f, 520f));
        return bounds;
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
