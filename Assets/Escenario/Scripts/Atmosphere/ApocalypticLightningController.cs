using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ApocalypticLightningController : MonoBehaviour
{
    [Header("Referencias")]
    public Light lightningLight;
    public Material skyboxSource;

    [Header("Frecuencia")]
    public Vector2 interval = new Vector2(12f, 30f);
    [Range(0f, 1f)] public float doubleFlashChance = 0.72f;

    [Header("Destello")]
    public Color lightningColor = new Color(0.56f, 0.62f, 0.92f, 1f);
    [Range(0.05f, 3f)] public float peakIntensity = 1.15f;
    [Range(0f, 1.5f)] public float skyExposureBoost = 0.42f;
    [Range(0f, 1f)] public float ambientBoost = 0.18f;

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
        while (enabled)
        {
            yield return new WaitForSeconds(Random.Range(
                Mathf.Min(interval.x, interval.y),
                Mathf.Max(interval.x, interval.y)));

            yield return FlashOnce(Random.Range(0.82f, 1.08f), Random.Range(0.08f, 0.15f));

            if (Random.value <= doubleFlashChance)
            {
                yield return new WaitForSeconds(Random.Range(0.05f, 0.16f));
                yield return FlashOnce(Random.Range(0.92f, 1.22f), Random.Range(0.11f, 0.20f));
            }
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

        if (runtimeSkybox != null && runtimeSkybox.HasProperty("_Exposure"))
        {
            runtimeSkybox.SetFloat("_Exposure", baseExposure + skyExposureBoost * amount);
        }
        RenderSettings.ambientIntensity = baseAmbientIntensity + ambientBoost * amount;
    }

    private void OnDisable()
    {
        if (stormRoutine != null) StopCoroutine(stormRoutine);
        ApplyFlash(0f);
        RenderSettings.ambientIntensity = baseAmbientIntensity;
        if (originalSkybox != null) RenderSettings.skybox = originalSkybox;
        if (runtimeSkybox != null)
        {
            if (Application.isPlaying) Destroy(runtimeSkybox);
            else DestroyImmediate(runtimeSkybox);
        }
    }
}
