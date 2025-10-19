using UnityEngine;

public class SlashPoolManager : MonoBehaviour
{
    [Header("Slash Prefab")]
    [SerializeField] private SwordSlash slashPrefab;

    [Header("Pool Settings")]
    [SerializeField] private int poolSize = 10;
    [SerializeField] private int maxPoolSize = 20;

    private BestObjectPool<SwordSlash> slashPool;

    public static SlashPoolManager Instance { get; private set; }

    void Awake()
    {
        InitializeSingleton();
    }

    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePools()
    {
        if (slashPrefab)
        {
            slashPool = new BestObjectPool<SwordSlash>(slashPrefab, poolSize, maxPoolSize);
        }
    }

    public SwordSlash GetSlash(SlashDirection direction)
    {
        return slashPool?.GetObject();
    }

    public void ReturnSlash(SwordSlash slash, SlashDirection direction)
    {
        slashPool?.ReleaseObject(slash);
    }
}

public enum SlashDirection
{
    Up,
    Down,
    Left,
    Right
}