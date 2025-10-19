using UnityEngine;

public class OrcController : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float attackRange = 1.5f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 8f;

    private float lastAttackTime;
    private bool isAttacking;
    private bool hasAttacked;
    private bool isCollidingWithPlayer;

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
    private static readonly int AttackIdle = Animator.StringToHash("Attack_Idle");
    private static readonly int AttackWalk = Animator.StringToHash("Attack_Walk");
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
            HandleChaseMovement();
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
        anim.SetFloat(Hor, moveDirection.x);
        anim.SetFloat(Vert, moveDirection.y);
        anim.SetFloat(CurrSpeed, moveDirection.magnitude);
    }

    private void UpdateFacingDirection()
    {
        if (moveDirection.sqrMagnitude > 0.1f)
        {
            lastDirection = moveDirection;
        }

        anim.SetFloat(LastHor, lastDirection.x);
        anim.SetFloat(LastVert, lastDirection.y);
    }

    private void CheckAttackState()
    {
        // Reset animator speed if not attacking
        bool currentlyAttacking = anim.GetBool(AttackIdle) || anim.GetBool(AttackWalk);

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

        if (distanceToPlayer <= attackRange && CanAttack())
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        if (moveDirection.magnitude > 0.1f)
            TriggerWalkAttack();
        else
            TriggerIdleAttack();
    }

    private void TriggerIdleAttack()
    {
        hasAttacked = false;
        isAttacking = true;
        anim.SetBool(AttackIdle, true);
    }

    private void TriggerWalkAttack()
    {
        hasAttacked = false;
        isAttacking = true;
        anim.SetBool(AttackWalk, true);
    }

    // Called by Animation Event at the end of attack animations
    public void OnAttackFinished()
    {
        anim.SetBool(AttackIdle, false);
    }

    public void OnWalkAttackFinished()
    {
        anim.SetBool(AttackWalk, false);
    }

    // Called by Animation Event during attack animation
    public void OnAttackHit()
    {
        if (hasAttacked) return; // Prevent multiple hits per attack

        hasAttacked = true;
        DealDamageToPlayer();
    }

    private void DealDamageToPlayer()
    {
        if (!player) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
            lastAttackTime = Time.time;
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
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}