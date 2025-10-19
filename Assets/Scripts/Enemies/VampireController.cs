using UnityEngine;

public class VampireController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float meleeRange = 2f; // Close range melee attack
    [SerializeField] private float laserRange = 8f; // Long range laser attack
    [SerializeField] private GameObject laserPrefab;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private float keepDistanceRange = 5f; // Stay away from player

    private float lastAttackTime;
    private bool isAttacking;
    private bool hasAttacked;
    private bool isCollidingWithPlayer;
    private bool isLaserAttack; // Track if current attack is laser or melee

    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private Vector2 moveDirection;
    private Vector2 lastDirection = Vector2.right;
    private float directionChangeTimer = 0f;
    private float directionChangeDelay = 0.3f;

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
        TryAttackPlayer();
    }

    private void StopMovement()
    {
        moveDirection = Vector2.zero;
        UpdateAnimator();
    }

    private void UpdateMovement()
    {
        if (isCollidingWithPlayer)
        {
            moveDirection = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRange)
        {
            // Keep distance - back away if too close, chase if too far
            if (distanceToPlayer < keepDistanceRange)
            {
                HandleRetreatMovement();
            }
            else if (distanceToPlayer > laserRange * 0.8f)
            {
                HandleChaseMovement();
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

    private void HandleChaseMovement()
    {
        Vector2 desiredDirection = GetCardinalDirection();
        UpdateDirectionWithDelay(desiredDirection);
    }

    private void HandleRetreatMovement()
    {
        // Move away from player
        Vector2 desiredDirection = -GetCardinalDirection();
        UpdateDirectionWithDelay(desiredDirection);
    }

    private Vector2 GetCardinalDirection()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;

        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
        {
            return toPlayer.x > 0 ? Vector2.right : Vector2.left;
        }
        else
        {
            return toPlayer.y > 0 ? Vector2.up : Vector2.down;
        }
    }

    private void UpdateDirectionWithDelay(Vector2 desiredDirection)
    {
        if (desiredDirection != moveDirection)
        {
            directionChangeTimer += Time.deltaTime;
            if (directionChangeTimer >= directionChangeDelay || moveDirection == Vector2.zero)
            {
                moveDirection = desiredDirection;
                directionChangeTimer = 0f;
            }
        }
        else
        {
            directionChangeTimer = 0f;
        }
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
        if (!anim) return;

        SetMovementParameters();
        UpdateFacingDirection();
    }

    private void SetMovementParameters()
    {
        // Always face the player
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        anim.SetFloat(Hor, toPlayer.normalized.x);
        anim.SetFloat(Vert, toPlayer.normalized.y);
        anim.SetFloat(CurrSpeed, moveDirection.magnitude);
    }

    private void UpdateFacingDirection()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
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

    private void TryAttackPlayer()
    {
        if (isAttacking || !player) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Check if in melee range or laser range
        if (distanceToPlayer <= meleeRange && CanAttack())
        {
            PerformAttack(false); // Melee attack
        }
        else if (distanceToPlayer <= laserRange && CanAttack())
        {
            PerformAttack(true); // Laser attack
        }
    }

    private void PerformAttack(bool useLaser)
    {
        hasAttacked = false;
        isAttacking = true;
        isLaserAttack = useLaser;
        anim.SetBool(Attack, true);
    }

    // Called by Animation Event at the end of attack animation
    public void OnAttackFinished()
    {
        anim.SetBool(Attack, false);
    }

    // Called by Animation Event during attack animation
    public void OnAttackHit()
    {
        if (hasAttacked) return; // Prevent multiple hits per attack

        hasAttacked = true;

        if (isLaserAttack)
        {
            ShootLaser();
        }
        else
        {
            DealMeleeDamage();
        }

        lastAttackTime = Time.time;
    }

    private void DealMeleeDamage()
    {
        if (!player) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= meleeRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    private void ShootLaser()
    {
        if (!player || laserPrefab == null) return;

        // Calculate direction to player
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;

        // Instantiate laser
        GameObject laser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
        LaserBeam laserBeam = laser.GetComponent<LaserBeam>();
        if (laserBeam != null)
        {
            laserBeam.Initialize(direction, damage, 15f); // 15f is laser projectile range
        }
    }

    #region Combat
    public int GetDamage() => damage;
    public bool CanAttack() => Time.time >= lastAttackTime + attackCooldown;
    #endregion

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = true;
        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isCollidingWithPlayer = false;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, laserRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, meleeRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, keepDistanceRange);
    }
#endif
}
