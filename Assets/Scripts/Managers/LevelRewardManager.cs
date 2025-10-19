using UnityEngine;

public class LevelRewardManager : MonoBehaviour
{
    public static LevelRewardManager Instance { get; private set; }

    [Header("Reward Panel")]
    [SerializeField] private GameObject rewardPanel;

    [Header("Upgrade Amounts")]
    [SerializeField] private int healthBoostAmount = 2;
    [SerializeField] private int damageBoostAmount = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);
    }

    public void ShowRewardChoice()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            Time.timeScale = 0f; // Pause game while choosing
        }
    }

    public void ChooseHealthBoost()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.IncreaseMaxHealth(healthBoostAmount);
        }

        HideRewardPanel();
    }

    public void ChooseDamageBoost()
    {
        Player player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            player.IncreaseDamage(damageBoostAmount);
        }

        HideRewardPanel();
    }

    private void HideRewardPanel()
    {
        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        Time.timeScale = 1f; // Resume game

        // Activate teleport after choice
        LevelCompletionTrigger trigger = FindFirstObjectByType<LevelCompletionTrigger>();
        if (trigger != null)
        {
            trigger.OnRewardChosen();
        }
    }
}
