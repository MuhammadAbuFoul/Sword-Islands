using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    [SerializeField] private GameObject startMenuPanel;
    [SerializeField] private GameObject endMenuPanel;

    private static MenuManager instance;
    private bool gameStarted = false;
    private static bool skipStartMenu = false; // Flag to skip start menu on restart

    public static bool IsGameStarted()
    {
        return instance != null && instance.gameStarted;
    }

    void Awake()
    {
        // Safety: Always ensure time is running when scene loads
        Time.timeScale = 1f;

        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (skipStartMenu)
        {
            // Skip start menu and go straight to game
            skipStartMenu = false; // Reset flag
            StartGame();
        }
        else
        {
            ShowStartMenu();
        }
    }

    public void ShowStartMenu()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(true);

        if (endMenuPanel != null)
            endMenuPanel.SetActive(false);
        Time.timeScale = 0f; // Pause game
    }

    public void ShowEndMenu(bool won)
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);

        if (endMenuPanel != null)
        {
            endMenuPanel.SetActive(true);

            // Try to find and update the text
            TextMeshProUGUI textComponent = endMenuPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.text = won ? "Victory!" : "Defeat!";
            }
        }

        Time.timeScale = 0f; // Pause game
    }

    public void StartGame()
    {
        if (startMenuPanel != null)
            startMenuPanel.SetActive(false);
        Time.timeScale = 1f; // Resume game
        gameStarted = true;
        // Trigger spawning for the first level
        EnemySpawnerManager spawner = FindFirstObjectByType<EnemySpawnerManager>();
        if (spawner != null)
        {
            spawner.TriggerSpawn();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f; // Resume game

        // Set flag to skip start menu on next load
        skipStartMenu = true;

        // Destroy persistent managers to fully reset
        if (GameManager.Instance != null)
        {
            Destroy(GameManager.Instance.gameObject);
        }

        if (LevelRewardManager.Instance != null)
        {
            Destroy(LevelRewardManager.Instance.gameObject);
        }

        // Destroy self after reload
        Destroy(gameObject);

        // Reload the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // Resume time
        SceneManager.LoadScene(0); // Load first scene (assumed to be main menu)
    }

    // Static method to trigger end game from anywhere
    public static void TriggerEndGame(bool won)
    {
        if (instance != null)
        {
            instance.ShowEndMenu(won);
        }
    }
}
