using UnityEngine;

public class ArrowIndicator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    private Transform target;

    [Header("Positioning")]
    [Tooltip("How far from the player the arrow should sit, in the direction of the target.")]
    [SerializeField] private float distanceFromPlayer = 2f;
    [Tooltip("How fast the arrow slides into place each frame (0 = no smoothing).")]
    [SerializeField] private float positionLerp = 15f;

    [Header("Rotation")]
    [Tooltip("Arrow sprite's default facing.\nIf your sprite points RIGHT, use 0.\nUP = -90, LEFT = 180, DOWN = 90.")]
    [SerializeField] private float rotationOffsetDegrees = 0f;
    [SerializeField] private float rotationSpeed = 15f;

    [Header("Jitter Guards")]
    [Tooltip("If player and target are almost overlapping, freeze the arrow's rotation.")]
    [SerializeField] private float minDirectionMagnitude = 0.001f;

    [Header("Animation")]
    [Tooltip("Enable pulsing animation on the arrow.")]
    [SerializeField] private bool enablePulseAnimation = true;
    [Tooltip("How much the arrow scales (1.0 = no change, 1.2 = 20% larger).")]
    [SerializeField] private float pulseScale = 1.5f;
    [Tooltip("Speed of the pulse animation.")]
    [SerializeField] private float pulseSpeed = 2f;

    private Vector3 originalScale;

    void Start()
    {
        FindPlayerIfNeeded();
        originalScale = transform.localScale;
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null || target == null) return;

        Vector3 toTarget = (target.position - player.position);
        Vector3 dir = toTarget.sqrMagnitude > minDirectionMagnitude ? toTarget.normalized : transform.right; // fallback

        // 1) Place the arrow BESIDE the player, toward the target
        Vector3 desiredPos = player.position + dir * distanceFromPlayer;
        transform.position = positionLerp > 0f
            ? Vector3.Lerp(transform.position, desiredPos, positionLerp * Time.deltaTime)
            : desiredPos;

        // 2) Rotate the arrow to face the target (respecting your sprite's default facing)
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion desiredRot = Quaternion.AngleAxis(angle + rotationOffsetDegrees, Vector3.forward);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotationSpeed * Time.deltaTime);

        // 3) Pulse animation
        if (enablePulseAnimation)
        {
            float pulse = 1f + (pulseScale - 1f) * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
            transform.localScale = originalScale * pulse;
        }
    }

    void OnDisable()
    {
        // Reset scale when disabled
        if (originalScale != Vector3.zero)
        {
            transform.localScale = originalScale;
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

#if UNITY_EDITOR
    // Nice-to-have gizmo to visualize the placement radius.
    private void OnDrawGizmosSelected()
    {
        if (player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, distanceFromPlayer);
    }
#endif
}
