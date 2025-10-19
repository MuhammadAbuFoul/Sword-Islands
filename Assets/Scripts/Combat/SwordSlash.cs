using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    [SerializeField] private LayerMask enemyLayer;

    // Per-prefab tuning:
    [Tooltip("Direction the sprite/art is drawn to face (local forward). E.g. Right=(1,0), Up=(0,1).")]
    [SerializeField] private Vector2 authoredForward = Vector2.right;
    [Tooltip("Small visual nudge after alignment if needed.")]
    [SerializeField] private float rotationOffsetDegrees = 0f;

    private const float speed = 15f;

    private int damage;
    private Vector2 direction;
    private bool hasHit;
    private Vector3 startPosition;
    private float maxRange;
    private float maxRangeSqr;
    private SlashDirection slashDirection;

    void Update()
    {
        if (hasHit) return;

        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        // Use squared distance to avoid sqrt each frame
        if ((transform.position - startPosition).sqrMagnitude >= maxRangeSqr)
        {
            ReturnToPool();
        }
    }

    public void Initialize(Vector2 dir, int dmg, LayerMask enemies, float range, SlashDirection slashDir, Vector3 spawnPosition)
    {
        // 1) Place the object FIRST (important for pooling correctness)
        transform.position = spawnPosition;
        startPosition = spawnPosition;

        // 2) Reset state
        hasHit = false;
        direction = dir.normalized;
        damage = dmg;
        enemyLayer = enemies;
        maxRange = range;
        maxRangeSqr = range * range;
        slashDirection = slashDir;

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


    private void ReturnToPool()
    {
        hasHit = false;
        if (SlashPoolManager.Instance != null)
        {
            SlashPoolManager.Instance.ReturnSlash(this, slashDirection);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // Cache gameObject reference to avoid property access
        GameObject otherObj = other.gameObject;
        if (!IsOnEnemyLayer(otherObj)) return;

        // Check for OrcHealth
        if (other.TryGetComponent<OrcHealth>(out var orcHealth))
        {
            hasHit = true; // Set this FIRST to prevent multiple hits
            orcHealth.TakeDamage(damage);
            CancelInvoke(); // Cancel any pending returns
            Invoke(nameof(ReturnToPool), 0.1f);
            return;
        }

        // Check for VampireHealth
        if (other.TryGetComponent<VampireHealth>(out var vampireHealth))
        {
            hasHit = true; // Set this FIRST to prevent multiple hits
            vampireHealth.TakeDamage(damage);
            CancelInvoke(); // Cancel any pending returns
            Invoke(nameof(ReturnToPool), 0.1f);
            return;
        }

        // Check for GiantVampireHealth
        if (other.TryGetComponent<GiantVampireHealth>(out var giantVampireHealth))
        {
            hasHit = true; // Set this FIRST to prevent multiple hits
            giantVampireHealth.TakeDamage(damage);
            CancelInvoke(); // Cancel any pending returns
            Invoke(nameof(ReturnToPool), 0.1f);
        }
    }

    private bool IsOnEnemyLayer(GameObject obj)
    {
        return (enemyLayer.value & (1 << obj.layer)) > 0;
    }
}