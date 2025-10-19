using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [System.Serializable]
    public class Level
    {
        public string levelName;
        public GameObject levelRoot; // Parent GameObject to enable/disable
    }

    [SerializeField] private Level[] levels;

    private int currentLevelIndex = -1;

    void Start()
    {
        if (levels.Length > 0)
        {
            LoadLevel(0);
        }
    }

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length)
        {
            return;
        }

        // Disable current level
        if (currentLevelIndex >= 0 && currentLevelIndex < levels.Length)
        {
            if (levels[currentLevelIndex].levelRoot != null)
            {
                levels[currentLevelIndex].levelRoot.SetActive(false);
            }
        }

        // Enable new level
        currentLevelIndex = levelIndex;
        Level currentLevel = levels[currentLevelIndex];

        if (currentLevel.levelRoot != null)
        {
            currentLevel.levelRoot.SetActive(true);
        }
    }

    public void CompleteLevel()
    {
        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LevelCompleted(currentLevelIndex);
        }
    }

    public void LoadNextLevel()
    {
        LoadLevel(currentLevelIndex + 1);
    }

    public void LoadPreviousLevel()
    {
        LoadLevel(currentLevelIndex - 1);
    }

    public int GetCurrentLevelIndex() => currentLevelIndex;
}
