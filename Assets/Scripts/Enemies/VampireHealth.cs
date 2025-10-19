using UnityEngine;

public class VampireHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentHealth;

    void Awake()
    {
        InitializeHealth();
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        UpdateHealthColor();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        UpdateHealthColor();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthColor()
    {
        if (spriteRenderer == null) return;

        float healthPercent = (float)currentHealth / maxHealth;
        // Lerp from red (0 health) to white (full health)
        Color newColor = Color.Lerp(Color.red, Color.white, healthPercent);
        spriteRenderer.color = newColor;
    }

    private void Die()
    {
        // Notify level completion trigger
        LevelCompletionTrigger trigger = FindFirstObjectByType<LevelCompletionTrigger>();
        if (trigger != null)
        {
            trigger.RegisterEnemyDeath();
        }

        Destroy(gameObject);
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}
