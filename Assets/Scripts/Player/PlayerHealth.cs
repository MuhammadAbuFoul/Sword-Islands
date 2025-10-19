using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private int currentHealth;

    void Awake()
    {
        InitializeHealth();
    }

    private void InitializeHealth()
    {
        currentHealth = maxHealth;
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
        // Trigger game over
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameLost();
        }
    }

    public void IncreaseMaxHealth(int amount)
    {
        maxHealth += amount;
        currentHealth += amount; // Also heal the player by the boost amount
        UpdateHealthColor();
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
}
