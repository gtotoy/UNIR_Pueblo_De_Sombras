using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ApocalypticLightningControllerV2 : MonoBehaviour
{
    [Header("Referencias")]
    public Light lightningLight;
    public LineRenderer lightningBolt;
    public Material skyboxSource;

    [Header("Frecuencia")]
    public Vector2 interval = new Vector2(8f, 18f);
    public Vector2 firstFlashDelay = new Vector2(2.5f, 5f);
    [Range(0f, 1f)] public float doubleFlashChance = 0.82f;

    [Header("Destello")]
    public Color lightningColor = new Color(0.56f, 0.62f, 0.92f, 1f);
    [Range(0.05f, 3f)] public float peakIntensity = 1.55f;
    [Range(0f, 1.5f)] public float skyExposureBoost = 0.58f;
    [Range(0f, 1f)] public float ambientBoost = 0.26f;

    private Material originalSkybox;
    private Material runtimeSkybox;
    private float baseExposure;
    private float baseAmbientIntensity;
    private Coroutine stormRoutine;

    private void OnEnable()
    {
        if (lightningLight != null)
        {
            lightningLight.enabled = true;
            lightningLight.intensity = 0f;
            lightningLight.color = lightningColor;
        }
        if (lightningBolt != null) lightningBolt.enabled = false;

        originalSkybox = RenderSettings.skybox;
        Material source = skyboxSource != null ? skyboxSource : originalSkybox;
        if (source != null)
        {
            runtimeSkybox = new Material(source) { name = source.name + " (Runtime Lightning)" };
            RenderSettings.skybox = runtimeSkybox;
            if (runtimeSkybox.HasProperty("_Exposure")) baseExposure = runtimeSkybox.GetFloat("_Exposure");
        }

        baseAmbientIntensity = RenderSettings.ambientIntensity;
        stormRoutine = StartCoroutine(StormLoop());
    }

    private IEnumerator StormLoop()
    {
        yield return new WaitForSeconds(Random.Range(
            Mathf.Min(firstFlashDelay.x, firstFlashDelay.y),
            Mathf.Max(firstFlashDelay.x, firstFlashDelay.y)));

        while (enabled)
        {
            GenerateBolt();
            yield return FlashOnce(Random.Range(0.88f, 1.10f), Random.Range(0.16f, 0.26f));

            if (Random.value <= doubleFlashChance)
            {
                yield return new WaitForSeconds(Random.Range(0.08f, 0.20f));
                yield return FlashOnce(Random.Range(0.98f, 1.25f), Random.Range(0.18f, 0.32f));
            }

            ApplyFlash(0f);
            yield return new WaitForSeconds(Random.Range(
                Mathf.Min(interval.x, interval.y),
                Mathf.Max(interval.x, interval.y)));
        }
    }

    private IEnumerator FlashOnce(float intensityScale, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.001f));
            float flash = Mathf.Pow(1f - normalized, 2.2f);
            ApplyFlash(flash * intensityScale);
            yield return null;
        }
        ApplyFlash(0f);
    }

    private void ApplyFlash(float amount)
    {
        if (lightningLight != null)
        {
            lightningLight.intensity = peakIntensity * amount;
            lightningLight.color = lightningColor;
        }

        if (lightningBolt != null)
        {
            Color boltColor = lightningColor * Mathf.Lerp(1.8f, 3.2f, Mathf.Clamp01(amount));
            boltColor.a = Mathf.Clamp01(amount * 1.4f);
            lightningBolt.startColor = boltColor;
            lightningBolt.endColor = boltColor;
            lightningBolt.enabled = amount > 0.025f;
        }

        if (runtimeSkybox != null && runtimeSkybox.HasProperty("_Exposure"))
        {
            runtimeSkybox.SetFloat("_Exposure", baseExposure + skyExposureBoost * amount);
        }
        RenderSettings.ambientIntensity = baseAmbientIntensity + ambientBoost * amount;
    }

    private void GenerateBolt()
    {
        if (lightningBolt == null) return;
        Camera camera = Camera.main;
        if (camera == null)
        {
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera candidate in cameras)
            {
                if (candidate.enabled && candidate.gameObject.activeInHierarchy)
                {
                    camera = candidate;
                    break;
                }
            }
        }
        if (camera == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(camera.transform.right, Vector3.up).normalized;
        float distance = Random.Range(250f, 380f);
        float side = Random.Range(-115f, 120f);
        Vector3 top = camera.transform.position + forward * distance + right * side;
        top.y = camera.transform.position.y + Random.Range(95f, 150f);
        Vector3 bottom = top + right * Random.Range(-24f, 24f);
        bottom.y = Random.Range(8f, 24f);

        int pointCount = Random.Range(11, 15);
        lightningBolt.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            float t = i / (float)(pointCount - 1);
            Vector3 point = Vector3.Lerp(top, bottom, t);
            float taper = Mathf.Sin(t * Mathf.PI);
            point += right * Random.Range(-9f, 9f) * taper;
            point += forward * Random.Range(-3f, 3f) * taper;
            lightningBolt.SetPosition(i, point);
        }
    }

    private void OnDisable()
    {
        if (stormRoutine != null) StopCoroutine(stormRoutine);
        ApplyFlash(0f);
        if (lightningBolt != null) lightningBolt.enabled = false;
        RenderSettings.ambientIntensity = baseAmbientIntensity;
        if (originalSkybox != null) RenderSettings.skybox = originalSkybox;
        if (runtimeSkybox != null)
        {
            if (Application.isPlaying) Destroy(runtimeSkybox);
            else DestroyImmediate(runtimeSkybox);
        }
    }
}
