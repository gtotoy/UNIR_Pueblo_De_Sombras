using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class CorruptionLightFlicker : MonoBehaviour
{
    [Header("Variación continua")]
    [Range(0.4f, 1f)] public float minimumMultiplier = 0.82f;
    [Range(1f, 1.6f)] public float maximumMultiplier = 1.12f;
    [Range(0.05f, 3f)] public float noiseFrequency = 0.55f;
    [Range(0.1f, 20f)] public float smoothing = 8f;

    [Header("Pulsos ocasionales")]
    [Range(1f, 2f)] public float pulseMultiplier = 1.38f;
    public Vector2 pulseInterval = new Vector2(3.5f, 10f);
    public Vector2 pulseDuration = new Vector2(0.16f, 0.42f);

    private Light controlledLight;
    private float baseIntensity;
    private Color baseColor;
    private float seed;
    private float nextPulseTime;
    private float pulseStartTime = -100f;
    private float activePulseDuration;

    private void Awake()
    {
        controlledLight = GetComponent<Light>();
        baseIntensity = controlledLight.intensity;
        baseColor = controlledLight.color;
        seed = Mathf.Abs(GetInstanceID() * 0.01357f) % 1000f;
        ScheduleNextPulse(Time.time);
    }

    private void OnEnable()
    {
        if (controlledLight == null) controlledLight = GetComponent<Light>();
        if (baseIntensity <= 0f) baseIntensity = controlledLight.intensity;
        if (baseColor.maxColorComponent <= 0f) baseColor = controlledLight.color;
        ScheduleNextPulse(Time.time);
    }

    private void Update()
    {
        if (controlledLight == null) return;

        float noise = Mathf.PerlinNoise(seed, Time.time * noiseFrequency);
        float multiplier = Mathf.Lerp(minimumMultiplier, maximumMultiplier, noise);

        if (Time.time >= nextPulseTime)
        {
            pulseStartTime = Time.time;
            activePulseDuration = Random.Range(
                Mathf.Min(pulseDuration.x, pulseDuration.y),
                Mathf.Max(pulseDuration.x, pulseDuration.y));
            ScheduleNextPulse(Time.time + activePulseDuration);
        }

        float pulseAge = Time.time - pulseStartTime;
        if (pulseAge >= 0f && pulseAge <= activePulseDuration)
        {
            float normalized = pulseAge / Mathf.Max(activePulseDuration, 0.001f);
            float pulseShape = Mathf.Sin(normalized * Mathf.PI);
            multiplier *= Mathf.Lerp(1f, pulseMultiplier, pulseShape);
            controlledLight.color = Color.Lerp(baseColor, new Color(0.72f, 0.48f, 1f, 1f), pulseShape * 0.18f);
        }
        else
        {
            controlledLight.color = Color.Lerp(controlledLight.color, baseColor, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }

        float targetIntensity = baseIntensity * multiplier;
        controlledLight.intensity = Mathf.Lerp(
            controlledLight.intensity,
            targetIntensity,
            1f - Mathf.Exp(-smoothing * Time.deltaTime));
    }

    private void OnDisable()
    {
        if (controlledLight == null) return;
        controlledLight.intensity = baseIntensity;
        controlledLight.color = baseColor;
    }

    private void ScheduleNextPulse(float fromTime)
    {
        nextPulseTime = fromTime + Random.Range(
            Mathf.Min(pulseInterval.x, pulseInterval.y),
            Mathf.Max(pulseInterval.x, pulseInterval.y));
    }
}
