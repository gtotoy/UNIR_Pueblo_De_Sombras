using UnityEngine;
using UnityEngine.UI;

/// Single screen-edge arrow that points towards the player's current objective:
/// the nearest alive enemy during a wave, the boss once it spawns, or the next
/// cleansing zone's sphere while preparing between waves.
[RequireComponent(typeof(RectTransform))]
public class ObjectiveIndicator : MonoBehaviour
{
    [SerializeField] RectTransform arrowRect;
    [SerializeField] Image arrowImage;
    [SerializeField] Camera targetCamera;
    [SerializeField] float edgePadding = 60f;
    [Tooltip("Degrees to add so the sprite's default orientation faces the target. 0 if the sprite points right by default, -90 if it points up.")]
    [SerializeField] float spriteRotationOffset = 0f;

    GameController gameController;
    Transform player;

    void Awake()
    {
        if (arrowRect == null) arrowRect = GetComponent<RectTransform>();
        if (arrowImage == null) arrowImage = GetComponent<Image>();
        if (targetCamera == null) targetCamera = Camera.main;

        if (arrowImage != null && arrowImage.sprite == null)
            arrowImage.sprite = BuildPlaceholderTriangleSprite();
    }

    void Start()
    {
        gameController = FindFirstObjectByType<GameController>();
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
    }

    void Update()
    {
        Transform target = GetCurrentTarget();
        if (target == null || targetCamera == null)
        {
            SetVisible(false);
            return;
        }

        Vector3 screenPos = targetCamera.WorldToScreenPoint(target.position);
        bool behindCamera = screenPos.z < 0f;
        if (behindCamera) screenPos *= -1f;

        bool onScreen = !behindCamera
            && screenPos.x > 0f && screenPos.x < Screen.width
            && screenPos.y > 0f && screenPos.y < Screen.height;

        if (onScreen)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        PositionAtEdge(screenPos);
    }

    Transform GetCurrentTarget()
    {
        var wm = WaveManager.Instance;
        if (wm == null || gameController == null) return null;
        if (wm.HasWon || wm.HasLost) return null;

        if (gameController.CurrentState == GameController.State.Wave && wm.EnemiesAlive > 0)
        {
            Transform nearest = FindNearestEnemy(wm.AliveEnemies);
            if (nearest != null) return nearest;
        }

        if (wm.BossTransform != null) return wm.BossTransform;

        return wm.CurrentZoneSphere;
    }

    Transform FindNearestEnemy(System.Collections.Generic.IReadOnlyList<EnemyController> enemies)
    {
        Vector3 origin = player != null ? player.position : (targetCamera != null ? targetCamera.transform.position : Vector3.zero);
        EnemyController nearest = null;
        float bestSqrDist = float.MaxValue;

        for (int i = 0; i < enemies.Count; i++)
        {
            var enemy = enemies[i];
            if (enemy == null) continue;
            float sqrDist = (enemy.transform.position - origin).sqrMagnitude;
            if (sqrDist < bestSqrDist)
            {
                bestSqrDist = sqrDist;
                nearest = enemy;
            }
        }

        return nearest != null ? nearest.transform : null;
    }

    void PositionAtEdge(Vector3 screenPos)
    {
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        Vector3 fromCenter = screenPos - screenCenter;
        if (Mathf.Approximately(fromCenter.x, 0f)) fromCenter.x = 0.0001f;

        float halfWidth = Screen.width / 2f - edgePadding;
        float halfHeight = Screen.height / 2f - edgePadding;
        float slope = fromCenter.y / fromCenter.x;

        Vector3 clamped;
        if (Mathf.Abs(halfWidth * slope) <= halfHeight)
        {
            clamped = new Vector3(Mathf.Sign(fromCenter.x) * halfWidth, Mathf.Sign(fromCenter.x) * halfWidth * slope, 0f);
        }
        else
        {
            clamped = new Vector3(Mathf.Sign(fromCenter.y) * halfHeight / slope, Mathf.Sign(fromCenter.y) * halfHeight, 0f);
        }

        arrowRect.position = screenCenter + clamped;

        float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg;
        arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle + spriteRotationOffset);
    }

    // Toggles the image only (not the GameObject) - disabling this component's own
    // GameObject would stop Update() from ever running again.
    void SetVisible(bool visible)
    {
        if (arrowImage != null) arrowImage.enabled = visible;
    }

    static Sprite BuildPlaceholderTriangleSprite()
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        var clear = new Color(0f, 0f, 0f, 0f);
        var fill = Color.white;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x / (float)(size - 1)) * 2f - 1f; // -1..1
                float ny = y / (float)(size - 1);              // 0..1 (0 = bottom)
                bool inside = Mathf.Abs(nx) <= ny;
                texture.SetPixel(x, y, inside ? fill : clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
