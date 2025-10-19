using UnityEngine;

public class GiantVampireBossController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 2;
    [SerializeField] private GameObject laserPrefab;

    [Header("Attack Patterns")]
    [SerializeField] private float circleAttackCooldown = 10f; 
    [SerializeField] private float targetedAttackCooldown = 2f; 
    [SerializeField] private int circleDirections = 12; // Number of lasers in circle pattern

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float detectionRange = 50f;
    [SerializeField] private float optimalRange = 8f; // Preferred distance from player
    [SerializeField] private float rangeTolerance = 2f; // How close to optimal is acceptable

    private float lastCircleAttackTime;
    private float lastTargetedAttackTime;
    private bool isAttacking;

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private Vector2 moveDirection;
    private Vector2 lastDirection = Vector2.right;

    // Animator hashes
    private static readonly int Hor = Animator.StringToHash("Hor");
    private static readonly int Vert = Animator.StringToHash("Vert");
    private static readonly int LastHor = Animator.StringToHash("LastHor");
    private static readonly int LastVert = Animator.StringToHash("LastVert");
    private static readonly int CurrSpeed = Animator.StringToHash("CurrSpeed");
    private static readonly int Attack = Animator.StringToHash("Attack");

    void Start()
    {
        InitializeComponents();
        // Start attack timers at random offsets to avoid both firing at once
        lastCircleAttackTime = -circleAttackCooldown + Random.Range(0f, 2f);
        lastTargetedAttackTime = -targetedAttackCooldown + Random.Range(0f, 2f);
    }

    private void InitializeComponents()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        if (!player)
        {
            StopMovement();
            return;
        }

        UpdateMovement();
        UpdateAnimator();
        CheckAttackState();
        TryAttacks();
    }

    private void StopMovement()
    {
        moveDirection = Vector2.zero;
        UpdateAnimator();
    }

    private void UpdateMovement()
    {
        if (isAttacking)
        {
            moveDirection = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Try to maintain optimal range
            if (distanceToPlayer < optimalRange - rangeTolerance)
            {
                // Too close, back away
                moveDirection = GetCardinalDirection(true); // Retreat
            }
            else if (distanceToPlayer > optimalRange + rangeTolerance)
            {
                // Too far, approach
                moveDirection = GetCardinalDirection(false); // Chase
            }
            else
            {
                // In optimal range, stop moving
                moveDirection = Vector2.zero;
            }
        }
        else
        {
            moveDirection = Vector2.zero;
        }
    }

    private Vector2 GetCardinalDirection(bool retreat)
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;

        Vector2 direction;
        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
        {
            direction = toPlayer.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            direction = toPlayer.y > 0 ? Vector2.up : Vector2.down;
        }

        return retreat ? -direction : direction;
    }

    void FixedUpdate()
    {
        if (!isAttacking)
        {
            rb.linearVelocity = moveDirection.normalized * moveSpeed;
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void UpdateAnimator()
    {
        if (!anim || !player) return;

        // Always face the player
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        anim.SetFloat(Hor, toPlayer.normalized.x);
        anim.SetFloat(Vert, toPlayer.normalized.y);
        anim.SetFloat(CurrSpeed, moveDirection.magnitude);

        if (toPlayer.sqrMagnitude > 0.1f)
        {
            lastDirection = toPlayer.normalized;
        }

        anim.SetFloat(LastHor, lastDirection.x);
        anim.SetFloat(LastVert, lastDirection.y);
    }

    private void CheckAttackState()
    {
        bool currentlyAttacking = anim.GetBool(Attack);

        if (!currentlyAttacking && isAttacking)
        {
            // Attack just ended
            isAttacking = false;
        }
        else if (currentlyAttacking)
        {
            isAttacking = true;
        }
    }

    private void TryAttacks()
    {
        if (!player) return;

        // Circle attack fires automatically every 10 seconds (no animation)
        if (Time.time >= lastCircleAttackTime + circleAttackCooldown)
        {
            ShootCirclePattern();
            lastCircleAttackTime = Time.time;
        }

        // Targeted attack uses animation
        if (!isAttacking && Time.time >= lastTargetedAttackTime + targetedAttackCooldown)
        {
            PerformTargetedAttack();
        }
    }

    private void PerformTargetedAttack()
    {
        isAttacking = true;
        anim.SetBool(Attack, true);
    }

    // Called by Animation Event at the end of targeted attack animation
    public void OnTargetedAttackFinished()
    {
        anim.SetBool(Attack, false);
    }

    // Called by Animation Event during targeted attack animation
    public void OnTargetedAttackHit()
    {
        ShootTargetedLaser();
        lastTargetedAttackTime = Time.time;
    }

    private void ShootCirclePattern()
    {
        if (laserPrefab == null) return;

        float angleStep = 360f / circleDirections;

        for (int i = 0; i < circleDirections; i++)
        {
            float angle = i * angleStep;
            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));

            GameObject laser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
            LaserBeam laserBeam = laser.GetComponent<LaserBeam>();
            if (laserBeam != null)
            {
                laserBeam.Initialize(direction, damage, 20f); // 20f is laser range
            }
        }
    }

    private void ShootTargetedLaser()
    {
        if (!player || laserPrefab == null) return;

        // Calculate direction to player
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;

        GameObject laser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
        LaserBeam laserBeam = laser.GetComponent<LaserBeam>();
        if (laserBeam != null)
        {
            laserBeam.Initialize(direction, damage, 20f); // 20f is laser range
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, optimalRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalRange - rangeTolerance);
        Gizmos.DrawWireSphere(transform.position, optimalRange + rangeTolerance);
    }
#endif
}
