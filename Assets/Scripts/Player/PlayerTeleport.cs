using UnityEngine;
using System.Collections;

public class PlayerTeleport : MonoBehaviour
{
    [SerializeField] private float teleportXOffset = 0f;
    [SerializeField] private float teleportYOffset = 10f;
    [SerializeField] private float teleportDuration = 1f;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private int nextLevelIndex = 1;
    [SerializeField] private ParticleSystem teleportEffect;
    [SerializeField] private GameObject arrowIndicator;

    private bool isTeleporting = false;

    void OnEnable()
    {
        // When teleport activates, tell the arrow to point here
        if (arrowIndicator != null)
        {
            ArrowIndicator arrow = arrowIndicator.GetComponent<ArrowIndicator>();
            if (arrow != null)
            {
                arrow.SetTarget(transform);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTeleporting)
        {
            // Disable arrow
            if (arrowIndicator != null)
                arrowIndicator.SetActive(false);

            // Notify completion trigger that teleport was used
            LevelCompletionTrigger completionTrigger = FindFirstObjectByType<LevelCompletionTrigger>();
            if (completionTrigger != null)
            {
                completionTrigger.OnTeleportUsed();
            }

            StartCoroutine(TeleportPlayer(other.transform));
        }
    }

    private IEnumerator TeleportPlayer(Transform player)
    {
        isTeleporting = true;

        Vector3 startPos = player.position;
        Vector3 targetPos = startPos + new Vector3(teleportXOffset, teleportYOffset, 0);

        // Hide player sprite
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite != null)
        {
            playerSprite.enabled = false;
        }

        // Play effect at start position
        if (teleportEffect != null)
        {
            ParticleSystem startEffect = Instantiate(teleportEffect, startPos, Quaternion.identity);
            startEffect.Play();
            Destroy(startEffect.gameObject, startEffect.main.duration + startEffect.main.startLifetime.constantMax);
        }

        // Lerp player position
        float elapsed = 0f;
        while (elapsed < teleportDuration)
        {
            elapsed += Time.deltaTime;
            player.position = Vector3.Lerp(startPos, targetPos, elapsed / teleportDuration);
            yield return null;
        }

        player.position = targetPos;

        // Play effect at end position
        if (teleportEffect != null)
        {
            ParticleSystem endEffect = Instantiate(teleportEffect, targetPos, Quaternion.identity);
            endEffect.Play();
            Destroy(endEffect.gameObject, endEffect.main.duration + endEffect.main.startLifetime.constantMax);
        }

        // Show player sprite
        if (playerSprite != null)
        {
            playerSprite.enabled = true;
        }

        // Load next level
        if (levelManager != null)
        {
            levelManager.LoadLevel(nextLevelIndex);
        }

        // Wait a frame for level to load, then trigger spawner
        yield return null;

        EnemySpawnerManager spawner = FindFirstObjectByType<EnemySpawnerManager>();
        if (spawner != null)
        {
            spawner.TriggerSpawn();
        }

        gameObject.SetActive(false);
    }
}
