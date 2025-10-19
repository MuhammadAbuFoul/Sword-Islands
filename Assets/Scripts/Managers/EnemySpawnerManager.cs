using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawnerManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        public int spawnCount = 1;
    }

    [Header("Spawning")]
    [SerializeField] private List<EnemySpawnData> enemiesToSpawn = new List<EnemySpawnData>();
    [SerializeField] private Tilemap spawnTilemap;
    [SerializeField] private float spawnDelay = 0.3f;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Effects")]
    [SerializeField] private ParticleSystem spawnEffect;
    [SerializeField] private float effectDelay = 0f;

    private List<Vector3> validSpawnPositions = new List<Vector3>();
    private bool hasSpawned = false;


    void OnEnable()
    {
        // When level root is enabled, trigger spawn if game has started
        // (This handles levels 2+ being activated, but skips level 1 before Start button)
        if (!spawnOnStart && !hasSpawned && MenuManager.IsGameStarted())
        {
            TriggerSpawn();
        }
    }

    public void TriggerSpawn()
    {
        if (hasSpawned)
        {

            return;
        }
        hasSpawned = true;

        CollectValidSpawnPositions();
        StartCoroutine(SpawnEnemies());
    }

    private void CollectValidSpawnPositions()
    {
        if (spawnTilemap == null)
        {
            return;
        }

        validSpawnPositions.Clear();

        BoundsInt bounds = spawnTilemap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                if (spawnTilemap.HasTile(cellPosition))
                {
                    Vector3 worldPos = spawnTilemap.CellToWorld(cellPosition) + spawnTilemap.cellSize / 2f;
                    validSpawnPositions.Add(worldPos);
                }
            }
        }
    }

    private IEnumerator SpawnEnemies()
    {
        if (validSpawnPositions.Count == 0)
        {
            yield break;
        }

        LevelCompletionTrigger completionTrigger = FindFirstObjectByType<LevelCompletionTrigger>();

        foreach (EnemySpawnData enemyData in enemiesToSpawn)
        {
            if (enemyData.enemyPrefab == null)
            {
                continue;
            }

            for (int i = 0; i < enemyData.spawnCount; i++)
            {
                int randomIndex = Random.Range(0, validSpawnPositions.Count);
                Vector3 spawnPos = validSpawnPositions[randomIndex];
                validSpawnPositions.RemoveAt(randomIndex);

                if (spawnEffect != null)
                {
                    ParticleSystem effect = Instantiate(spawnEffect, spawnPos, Quaternion.identity);
                    effect.Play();
                    Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
                }

                GameObject spawnedEnemy = Instantiate(enemyData.enemyPrefab, spawnPos, Quaternion.identity);

                if (completionTrigger != null)
                {
                    completionTrigger.RegisterEnemySpawned();
                }

                yield return new WaitForSeconds(spawnDelay);
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (spawnTilemap == null) return;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        BoundsInt bounds = spawnTilemap.cellBounds;
        Vector3 size = new Vector3(bounds.size.x, bounds.size.y, 0) * spawnTilemap.cellSize.x;
        Vector3 center = spawnTilemap.CellToWorld(new Vector3Int(bounds.xMin, bounds.yMin, 0)) + size / 2f;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}
