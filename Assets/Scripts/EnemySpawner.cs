using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject enemyPrefab;   // Drag your enemy prefab here
    public Transform player;         // Reference to the player

    [Header("Spawn Settings")]
    public float spawnDistance = 10f;    // Distance within which the spawner triggers
    public float despawnDistance = 20f;  // Distance beyond which enemies despawn
    public int maxEnemies = 5;           // Maximum enemies allowed at once

    // Internal tracking
    private Transform[] spawnPoints;
    private int currentEnemies = 0;

    // Track which enemy came from which spawn point
    private Dictionary<GameObject, Transform> spawnedEnemies = new Dictionary<GameObject, Transform>();

    void Start()
    {
        // Gather all child objects as spawn points
        spawnPoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            spawnPoints[i] = transform.GetChild(i);
        }
    }

    void Update()
    {
        // 1. Spawn logic
        if (currentEnemies < maxEnemies)
        {
            // Check each spawn point
            foreach (Transform spawnPoint in spawnPoints)
            {
                float distanceToPlayer = Vector3.Distance(player.position, spawnPoint.position);

                // If player is close enough, spawn an enemy
                if (distanceToPlayer <= spawnDistance)
                {
                    SpawnEnemy(spawnPoint);
                    // Optional: break if you only want to spawn one enemy per frame
                    // break;
                }
            }
        }

        // 2. Despawn logic
        // Make a list of enemies to remove (can't modify a dictionary while iterating)
        List<GameObject> enemiesToRemove = new List<GameObject>();

        foreach (var kvp in spawnedEnemies)
        {
            GameObject enemy = kvp.Key;
            Transform spawnPoint = kvp.Value;

            // If enemy was destroyed by something else, just remove it from tracking
            if (enemy == null)
            {
                enemiesToRemove.Add(enemy);
                continue;
            }

            // Check the distance between the player and the spawn point
            float distanceToPlayer = Vector3.Distance(player.position, spawnPoint.position);

            // If player has moved beyond despawnDistance, destroy the enemy
            if (distanceToPlayer > despawnDistance)
            {
                Destroy(enemy);
                currentEnemies--;
                enemiesToRemove.Add(enemy);
            }
        }

        // Remove any despawned or destroyed enemies from the dictionary
        foreach (GameObject enemy in enemiesToRemove)
        {
            spawnedEnemies.Remove(enemy);
        }
    }

    void SpawnEnemy(Transform spawnPoint)
    {
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedEnemies.Add(newEnemy, spawnPoint); // Track which spawn point it came from
        currentEnemies++;

        // If you want to decrement currentEnemies when enemies die by other means,
        // subscribe to an event on the enemy script that calls HandleEnemyDied()
        // e.g. newEnemy.GetComponent<Enemy>().OnEnemyDied += HandleEnemyDied;
    }

    // Example method if you want enemies to reduce the count when they die
    public void HandleEnemyDied()
    {
        currentEnemies--;
        if (currentEnemies < 0) currentEnemies = 0;
    }
}
