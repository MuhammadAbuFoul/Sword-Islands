using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayer;

    // Per-prefab tuning:
    [Tooltip("Direction the sprite/art is drawn to face (local forward). E.g. Right=(1,0), Up=(0,1).")]
    [SerializeField] private Vector2 authoredForward = Vector2.right;
    [Tooltip("Small visual nudge after alignment if needed.")]
    [SerializeField] private float rotationOffsetDegrees = 0f;

    private const float speed = 12f;

    private int damage;
    private Vector2 direction;
    private bool hasHit;
    private Vector3 startPosition;
    private float maxRange;
    private float maxRangeSqr;

    void Update()
    {
        if (hasHit) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Use squared distance to avoid sqrt each frame
        if ((transform.position - startPosition).sqrMagnitude >= maxRangeSqr)
        {
            DestroyLaser();
        }
    }

    public void Initialize(Vector2 dir, int dmg, float range)
    {
        // 1) Place the object FIRST
        startPosition = transform.position;

        // 2) Reset state
        hasHit = false;
        direction = dir.normalized;
        damage = dmg;
        maxRange = range;
        maxRangeSqr = range * range;

        // 3) Apply rotation with per-prefab offset
        ApplyRotation(direction);
    }

    private void ApplyRotation(Vector2 dir)
    {
        // Calculate the angle needed to rotate from authored forward to desired direction
        float rotationAngle = Vector2.SignedAngle(authoredForward, dir);

        // Apply the rotation plus any additional offset
        transform.rotation = Quaternion.AngleAxis(rotationAngle + rotationOffsetDegrees, Vector3.forward);
    }

    private void DestroyLaser()
    {
        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Cache gameObject reference to avoid property access
        GameObject otherObj = other.gameObject;
        if (!IsOnPlayerLayer(otherObj)) return;

        if (other.TryGetComponent<PlayerHealth>(out var playerHealth))
        {
            hasHit = true; // Set this FIRST to prevent multiple hits
            playerHealth.TakeDamage(damage);
            Destroy(gameObject, 0.1f);
        }
    }

    private bool IsOnPlayerLayer(GameObject obj)
    {
        return (playerLayer.value & (1 << obj.layer)) > 0;
    }
}
