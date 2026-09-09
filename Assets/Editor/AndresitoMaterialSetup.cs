using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AndresitoMaterialSetup
{
    private const string AssetName = "AndresitoGuacurari_Statue_01";
    private const string ModelPath = "Assets/Escenario/Props/Teaser/SM_AndresitoGuacurari_Statue_01.fbx";
    private const string MaterialFolder = "Assets/Escenario/Props/Teaser/Materials";
    private const string TextureFolder = "Assets/Escenario/Props/Teaser/Textures";
    private const string MaterialPath = MaterialFolder + "/M_AndresitoGuacurari_Statue_01.mat";
    private const string SessionKey = "TFM.AndresitoMaterialSetup.Applied.V3";

    [InitializeOnLoadMethod]
    private static void ScheduleAutomaticSetup()
    {
        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        EditorApplication.delayCall += Apply;
    }

    [MenuItem("Tools/TFM/Configurar material de Andresito")]
    public static void Apply()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);

        string baseColorPath = MoveTexture("T_AndresitoGuacurari_Statue_01_BaseColor.jpg");
        string metallicPath = MoveTexture("T_AndresitoGuacurari_Statue_01_MetallicSmoothness.png");
        string normalPath = MoveTexture("T_AndresitoGuacurari_Statue_01_Normal.jpg");
        MoveTexture("T_AndresitoGuacurari_Statue_01_Metallic.jpg");
        MoveTexture("T_AndresitoGuacurari_Statue_01_Roughness.jpg");

        ConfigureTexture(baseColorPath, TextureImporterType.Default, true);
        ConfigureTexture(metallicPath, TextureImporterType.Default, false);
        ConfigureTexture(normalPath, TextureImporterType.NormalMap, false);

        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
        Texture2D metallicSmoothness = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        if (baseColor == null || metallicSmoothness == null || normal == null)
        {
            Debug.LogError("No se pudieron cargar todas las texturas de " + AssetName + ".");
            return;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                Debug.LogError("No se encontró el shader Universal Render Pipeline/Lit.");
                return;
            }

            material = new Material(shader) { name = "M_" + AssetName };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.SetTexture("_BaseMap", baseColor);
        material.SetColor("_BaseColor", Color.white);
        // El mapa metalico de Tripo es casi blanco en toda la figura. URP lo usa al
        // 100 % y oculta el control de intensidad, convirtiendo la estatua en un
        // espejo negro bajo la iluminacion nocturna del nivel. Lo conservamos como
        // textura fuente, pero usamos valores uniformes de metal envejecido.
        material.SetTexture("_MetallicGlossMap", null);
        material.SetFloat("_Metallic", 0.12f);
        material.SetFloat("_Smoothness", 0.28f);
        material.SetTexture("_BumpMap", normal);
        material.SetFloat("_BumpScale", 1.0f);
        material.DisableKeyword("_METALLICSPECGLOSSMAP");
        material.EnableKeyword("_NORMALMAP");
        material.DisableKeyword("_SPECULAR_SETUP");
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();

        RemapImportedMaterial(material);
        AssignMaterialInOpenScenes(material);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();

        Debug.Log("Material de Andresito configurado: Base Color, Metallic/Smoothness corregido y Normal Map asignados.");
    }

    private static void RemapImportedMaterial(Material material)
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            return;

        foreach (UnityEngine.Object subAsset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (subAsset is Material importedMaterial)
            {
                var identifier = new AssetImporter.SourceAssetIdentifier(typeof(Material), importedMaterial.name);
                importer.AddRemap(identifier, material);
            }
        }

        importer.SaveAndReimport();
    }

    private static void AssignMaterialInOpenScenes(Material material)
    {
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Renderer renderer in renderers)
        {
            if (!renderer.gameObject.scene.IsValid() ||
                !renderer.gameObject.name.StartsWith("SM_AndresitoGuacurari_Statue_01", StringComparison.Ordinal))
                continue;

            Material[] slots = renderer.sharedMaterials;
            for (int index = 0; index < slots.Length; index++)
                slots[index] = material;

            renderer.sharedMaterials = slots;
            EditorUtility.SetDirty(renderer);
            EditorSceneManager.MarkSceneDirty(renderer.gameObject.scene);
        }
    }

    private static void ConfigureTexture(string path, TextureImporterType type, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
            return;

        bool changed = importer.textureType != type || importer.sRGBTexture != sRgb;
        importer.textureType = type;
        importer.sRGBTexture = sRgb;
        importer.alphaIsTransparency = false;
        if (changed)
            importer.SaveAndReimport();
    }

    private static string MoveTexture(string fileName)
    {
        string source = "Assets/" + fileName;
        string destination = TextureFolder + "/" + fileName;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destination) != null)
            return destination;

        if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(source) != null)
        {
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error))
                Debug.LogError(error);
        }

        return destination;
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int index = 1; index < parts.Length; index++)
        {
            string next = current + "/" + parts[index];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[index]);
            current = next;
        }
    }
}
