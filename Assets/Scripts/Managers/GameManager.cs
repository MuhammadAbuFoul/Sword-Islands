using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game State")]
    [SerializeField] private int totalLevels = 4; // Total number of levels in your game

    private int currentLevel = 0;
    private bool gameWon = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LevelCompleted(int levelIndex)
    {
        currentLevel = levelIndex;

        // Check if this was the final level
        if (currentLevel >= totalLevels - 1)
        {
            GameWon();
        }
    }

    public void GameWon()
    {
        gameWon = true;
        MenuManager.TriggerEndGame(true);
    }

    public void GameLost()
    {
        gameWon = false;
        MenuManager.TriggerEndGame(false);
    }

    public int GetCurrentLevel() => currentLevel;
    public bool IsGameWon() => gameWon;
}
