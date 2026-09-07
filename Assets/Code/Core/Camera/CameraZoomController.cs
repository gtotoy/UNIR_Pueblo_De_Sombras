using UnityEngine;
using Unity.Cinemachine;

public class CameraZoomController : MonoBehaviour
{
    public static CameraZoomController Instance { get; private set; }

    [SerializeField] CinemachinePositionComposer composer;
    [SerializeField] float preparationDistance = 60f;
    [SerializeField] float waveDistance = 30f;
    [SerializeField] float lerpSpeed = 4f;

    float targetDistance;

    void Awake()
    {
        Instance = this;
        targetDistance = composer != null ? composer.CameraDistance : waveDistance;
    }

    void Update()
    {
        if (composer == null) return;
        composer.CameraDistance = Mathf.Lerp(composer.CameraDistance, targetDistance, Time.deltaTime * lerpSpeed);
    }

    public void EnterPreparation()
    {
        targetDistance = preparationDistance;
    }

    public void EnterWave()
    {
        targetDistance = waveDistance;
    }
}
