using UnityEngine;

public class LevelCompletionTrigger : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private CompletionType completionType;
    [SerializeField] private Transform teleportsParent; // Parent containing all teleport children
    [SerializeField] private GameObject arrowIndicator;

    public enum CompletionType
    {
        AllEnemiesDefeated,
        TriggerZone,
        Manual
    }

    private int enemiesAlive = 0;
    private bool levelCompleted = false;
    private int currentTeleportIndex = 0;

    public void RegisterEnemySpawned()
    {
        enemiesAlive++;
    }

    public void RegisterEnemyDeath()
    {
        if (levelCompleted) return;

        enemiesAlive--;

        if (enemiesAlive <= 0 && completionType == CompletionType.AllEnemiesDefeated)
        {
            CompleteLevel();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (completionType != CompletionType.TriggerZone) return;
        if (levelCompleted) return;

        if (other.CompareTag("Player"))
        {
            CompleteLevel();
        }
    }

    public void CompleteLevel()
    {
        if (levelCompleted) return;

        levelCompleted = true;

        if (levelManager != null)
        {
            levelManager.CompleteLevel();
        }

        // Check if this is the final level
        bool isFinalLevel = levelManager != null &&
                           levelManager.GetCurrentLevelIndex() >= 3; // Level 4 (index 3) is final

        if (isFinalLevel)
        {
            // Final level - no reward, just win screen
            // GameManager will trigger the win screen
        }
        else
        {
            // Not final level - show reward choice menu
            if (LevelRewardManager.Instance != null)
            {
                LevelRewardManager.Instance.ShowRewardChoice();
            }
        }
    }

    // Called after player chooses a reward
    public void OnRewardChosen()
    {
        ActivateNextTeleport();
    }

    private void ActivateNextTeleport()
    {
        if (teleportsParent == null || currentTeleportIndex >= teleportsParent.childCount)
        {
            return;
        }

        // Get the next teleport child
        GameObject nextTeleport = teleportsParent.GetChild(currentTeleportIndex).gameObject;
        nextTeleport.SetActive(true);

        // Activate arrow
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(true);
        }
    }

    public void OnTeleportUsed()
    {
        // Called by PlayerTeleport when player uses it
        levelCompleted = false;
        currentTeleportIndex++;
    }
}
