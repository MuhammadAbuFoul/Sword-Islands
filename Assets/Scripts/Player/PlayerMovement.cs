using UnityEngine;
using UnityEngine.Tilemaps;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 5f;

    [Header("Components")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D playerCollider;

    [Header("Combat")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 3f;
    [SerializeField] private float dashCooldown = 0.2f;
    [SerializeField] private float dashTime = 0.12f;
    [SerializeField] private float dashCheckStep = 0.5f;
    [SerializeField] private ParticleSystem dashVanishEffect;
    [SerializeField] private ParticleSystem dashAppearEffect;
    [SerializeField] private LayerMask groundLayer;


    // Movement
    private Vector2 moveDirection;
    private bool isDashing;
    private float lastDashTime;

    // Combat
    private bool hasAttacked;
    private bool isAttacking;
    private float lastAttackInputTime;
    private int attackComboCount;
    private const float ComboWindow = 0.5f;
    private const float MaxComboSpeed = 2f;
    private const float ComboSpeedIncrement = 0.3f;

    // Mouse tracking cache
    private Camera mainCamera;
    private Vector2 cachedMouseDirection;
    private bool mouseDirtyFlag = true;

    // Animator hashes (faster & safer than raw strings)
    private static readonly int Hor = Animator.StringToHash("Hor");
    private static readonly int Vert = Animator.StringToHash("Vert");
    private static readonly int CurrSpeed = Animator.StringToHash("CurrSpeed");
    private static readonly int AttackIdle = Animator.StringToHash("Attack_Idle");
    private static readonly int AttackWalk = Animator.StringToHash("Attack_Walk");



    void Awake()
    {
        rb.constraints |= RigidbodyConstraints2D.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInput();
        UpdateAnimator();
        CheckAttackState();
    }

    private void CheckAttackState()
    {
        // Reset animator speed if not attacking
        bool currentlyAttacking = anim.GetBool(AttackIdle) || anim.GetBool(AttackWalk);

        if (!currentlyAttacking && isAttacking)
        {
            // Attack just ended
            anim.speed = 1f;
            isAttacking = false;
        }
        else if (currentlyAttacking)
        {
            isAttacking = true;
        }
    }

    private void HandleInput()
    {
        if (isDashing) return;

        moveDirection = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        // Mark mouse direction as dirty when mouse moves
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            mouseDirtyFlag = true;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleAttackInput();
        }

        if (Input.GetKeyDown(KeyCode.Space) && CanDash())
        {
            HandleDashInput();
        }
    }

    private void HandleAttackInput()
    {
        // Always update combo and speed, even if already attacking
        UpdateAttackCombo();
        ApplyComboSpeed();

        // Only trigger new attack if not already attacking
        if (!isAttacking)
        {
            if (moveDirection.magnitude > 0.1f)
                TriggerWalkAttack();
            else
                TriggerAttack();
        }
    }

    private void UpdateAttackCombo()
    {
        if (Time.time - lastAttackInputTime <= ComboWindow)
            attackComboCount++;
        else
            attackComboCount = 1;

        lastAttackInputTime = Time.time;
    }

    private void ApplyComboSpeed()
    {
        if (isAttacking)
        {
            float speedMultiplier = 1f + (attackComboCount - 1) * ComboSpeedIncrement;
            anim.speed = Mathf.Min(speedMultiplier, MaxComboSpeed);
        }
    }

    private Vector2 GetMouseDirection()
    {
        if (mouseDirtyFlag)
        {
            if (mainCamera == null)
            {
                cachedMouseDirection = Vector2.right;
            }
            else
            {
                Vector3 screenPos = Input.mousePosition;

                // Clamp mouse position to screen bounds to avoid frustum warnings
                screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
                screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

                Vector3 mousePos = mainCamera.ScreenToWorldPoint(screenPos);
                mousePos.z = 0f;
                cachedMouseDirection = ((Vector2)mousePos - (Vector2)transform.position).normalized;
            }
            mouseDirtyFlag = false;
        }

        return cachedMouseDirection;
    }

    private void UpdateAnimator()
    {
        Vector2 mouseDir = GetMouseDirection();

        anim.SetFloat(Hor, mouseDir.x);
        anim.SetFloat(Vert, mouseDir.y);
        anim.SetFloat(CurrSpeed, moveDirection.magnitude);
    }


    void FixedUpdate()
    {
        if (!isDashing)
        {
            // Let physics handle collisions with tilemap colliders
            rb.linearVelocity = moveDirection.normalized * speed;
        }
    }

    private void TriggerAttack()
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
        PerformAttackDamage();
    }

    private void PerformAttackDamage()
    {
        if (SlashPoolManager.Instance == null) return;

        Vector2 attackDir = GetMouseDirection();
        SlashDirection slashDir = GetSlashDirection(attackDir);

        SwordSlash slash = SlashPoolManager.Instance.GetSlash(slashDir);
        if (slash != null)
        {
            slash.Initialize(attackDir, attackDamage, enemyLayer, attackRange, slashDir, transform.position);
        }
    }

    private SlashDirection GetSlashDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            return direction.x > 0 ? SlashDirection.Right : SlashDirection.Left;
        else
            return direction.y > 0 ? SlashDirection.Up : SlashDirection.Down;
    }

    private bool CanDash()
    {
        return moveDirection.magnitude > 0.1f && Time.time >= lastDashTime + dashCooldown;
    }

    private void HandleDashInput()
    {
        TryDash(moveDirection.normalized);
    }

    private void TryDash(Vector2 dir)
    {
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        float safeDist = CalculateSafeDashDistance(dir);

        if (safeDist == 0f) return;

        Vector3 targetPos = (Vector3)rb.position + (Vector3)(dir * safeDist);
        lastDashTime = Time.time;
        StartCoroutine(PerformDash(targetPos, dashTime));
    }

    private float CalculateSafeDashDistance(Vector2 dir)
    {
        Vector3 start = rb.position;
        float safeDist = 0f;

        for (float d = dashCheckStep; d <= dashDistance; d += dashCheckStep)
        {
            Vector3 checkPos = start + (Vector3)(dir * d);

            // Check if there's ground at this position
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, 0.2f, groundLayer);

            bool foundGround = false;

            foreach (Collider2D hit in hits)
            {
                if (hit != playerCollider) // Exclude player's own collider
                {
                    foundGround = true;
                    break;
                }
            }

            if (foundGround)
            {
                safeDist = d; // Valid position with ground
            }
            else
            {
                break; // No ground found, stop here
            }
        }

        return safeDist;
    }

    private System.Collections.IEnumerator PerformDash(Vector3 target, float time)
    {
        Vector3 startPos = rb.position;
        isDashing = true;
        rb.linearVelocity = Vector2.zero;

        SetPlayerVisibility(false);
        PlayVanishEffect(startPos, target);

        yield return LerpToTarget(startPos, target, time);

        PlayAppearEffect(target);
        yield return new WaitForSeconds(0.1f);

        SetPlayerVisibility(true);
        StopDashEffects();
        isDashing = false;
    }

    private void SetPlayerVisibility(bool visible)
    {
        spriteRenderer.enabled = visible;
        playerCollider.enabled = visible;
    }

    private void PlayVanishEffect(Vector3 from, Vector3 target)
    {
        if (dashVanishEffect == null) return;

        dashVanishEffect.transform.position = from;
        Vector2 dir = (target - from).normalized;
        float angle = Mathf.Atan2(-dir.y, -dir.x) * Mathf.Rad2Deg;
        dashVanishEffect.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        dashVanishEffect.Play();
    }

    private System.Collections.IEnumerator LerpToTarget(Vector3 from, Vector3 target, float time)
    {
        float elapsed = 0f;
        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            rb.position = Vector2.Lerp(from, target, Mathf.Clamp01(elapsed / time));
            yield return null;
        }
        rb.position = target;
    }

    private void PlayAppearEffect(Vector3 position)
    {
        if (dashAppearEffect == null) return;

        dashAppearEffect.transform.position = position;
        dashAppearEffect.Play();
    }

    private void StopDashEffects()
    {
        if (dashVanishEffect != null && dashVanishEffect.isPlaying)
            dashVanishEffect.Stop();
        if (dashAppearEffect != null && dashAppearEffect.isPlaying)
            dashAppearEffect.Stop();
    }

    public void IncreaseDamage(int amount)
    {
        attackDamage += amount;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}