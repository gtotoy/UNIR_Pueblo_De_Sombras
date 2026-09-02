using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PuebloSombrasRetroFilterInstaller
{
    private const string RootName = "Atmosfera_Filtro_Retro";
    private const string ProfileFolder = "Assets/Escenario/Settings/PostProcessing";
    private const string ProfilePath = ProfileFolder + "/VP_PuebloSombras_RetroSuave.asset";
    private const string MarkerName = "PuebloSombras_RetroFilter_Applied.txt";

    private static string MarkerPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", MarkerName);

    static PuebloSombrasRetroFilterInstaller()
    {
        EditorApplication.delayCall += ApplyOnLoad;
    }

    private static void ApplyOnLoad()
    {
        if (!File.Exists(MarkerPath)) Apply(false);
    }

    [MenuItem("Pueblo de Sombras/Aplicar filtro retro sutil")]
    public static void ApplyFromMenu()
    {
        Apply(true);
    }

    private static void Apply(bool manual)
    {
        try
        {
            Scene scene = SceneManager.GetActiveScene();
            Camera camera = FindGameCamera(scene);
            if (camera == null)
            {
                Debug.LogWarning("[PuebloSombras Retro] La cámara de juego todavía no está disponible.");
                if (!manual) EditorApplication.delayCall += ApplyOnLoad;
                return;
            }

            EnsureFolder(ProfileFolder);
            VolumeProfile profile = BuildProfile();

            GameObject existing = FindExactObject(scene, RootName);
            if (existing != null) Undo.DestroyObjectImmediate(existing);

            GameObject root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Crear filtro retro sutil");
            Volume volume = root.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 40f;
            volume.weight = 1f;
            volume.sharedProfile = profile;

            UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
            Undo.RecordObject(cameraData, "Activar postprocesado retro");
            cameraData.renderPostProcessing = true;
            cameraData.volumeLayerMask = ~0;
            cameraData.dithering = true;
            EditorUtility.SetDirty(cameraData);

            AssetDatabase.SaveAssets();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();

            string[] report =
            {
                "Pueblo de Sombras - filtro retro sutil",
                "Escena: " + scene.path,
                "Cámara: " + camera.name,
                "Volumen global: " + RootName,
                "Perfil: " + ProfilePath,
                "Grano: 0.14",
                "Viñeta: 0.16",
                "Aberración cromática: 0.018",
                "Saturación: -9 | Contraste: +6",
                "Escena guardada automáticamente: no"
            };
            File.WriteAllLines(MarkerPath, report);
            Selection.activeObject = root;
            Debug.Log("[PuebloSombras Retro] Filtro retro sutil aplicado.\n" + string.Join("\n", report));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static VolumeProfile BuildProfile()
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "VP_PuebloSombras_RetroSuave";
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        else
        {
            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                VolumeComponent component = profile.components[i];
                profile.components.RemoveAt(i);
                if (component != null) UnityEngine.Object.DestroyImmediate(component, true);
            }
        }

        ColorAdjustments color = AddComponent<ColorAdjustments>(profile);
        color.postExposure.Override(-0.06f);
        color.contrast.Override(6f);
        color.colorFilter.Override(new Color(1f, 0.965f, 0.93f, 1f));
        color.hueShift.Override(-1.5f);
        color.saturation.Override(-9f);

        WhiteBalance whiteBalance = AddComponent<WhiteBalance>(profile);
        whiteBalance.temperature.Override(-3f);
        whiteBalance.tint.Override(4f);

        FilmGrain grain = AddComponent<FilmGrain>(profile);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.14f);
        grain.response.Override(0.76f);

        Vignette vignette = AddComponent<Vignette>(profile);
        vignette.color.Override(new Color(0.055f, 0.035f, 0.075f, 1f));
        vignette.center.Override(new Vector2(0.5f, 0.5f));
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.78f);
        vignette.rounded.Override(false);

        ChromaticAberration chromatic = AddComponent<ChromaticAberration>(profile);
        chromatic.intensity.Override(0.018f);

        Bloom bloom = AddComponent<Bloom>(profile);
        bloom.intensity.Override(0.18f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.58f);
        bloom.tint.Override(new Color(0.96f, 0.91f, 1f, 1f));
        bloom.highQualityFiltering.Override(false);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
    {
        T component = ScriptableObject.CreateInstance<T>();
        component.name = typeof(T).Name;
        component.active = true;
        profile.components.Add(component);
        AssetDatabase.AddObjectToAsset(component, profile);
        return component;
    }

    private static Camera FindGameCamera(Scene scene)
    {
        GameObject preferred = FindExactObject(scene, "Camara Zona 1");
        Camera preferredCamera = preferred != null ? preferred.GetComponent<Camera>() : null;
        if (preferredCamera != null && preferredCamera.enabled && preferredCamera.gameObject.activeInHierarchy) return preferredCamera;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
            {
                if (candidate.enabled && candidate.gameObject.activeInHierarchy) return candidate;
            }
        }
        return null;
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
}
