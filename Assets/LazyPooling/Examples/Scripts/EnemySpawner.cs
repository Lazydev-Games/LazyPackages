using UnityEngine;
using System.Collections;
using LazyPooling.Components;
using LazyPooling.Core;

namespace LazyPooling.Examples.Scripts
{
    /// <summary>
    /// Example 2D spawner showing different ways to use the pooling system for 2D enemies.
    /// Demonstrates both direct pool reference and PoolManager lookup methods in a 2D context.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Direct Pool Reference (Recommended) - For Enemy2D")]
        [SerializeField]
        private Pool enemyPool;
        
        [Header("Pool Manager Lookup (Alternative)")]
        [SerializeField]
        private string enemyPoolId = "EnemyPool";
        
        [Header("Spawn Settings")]
        [SerializeField]
        private float spawnInterval = 2f;
        
        [SerializeField]
        private Transform[] spawnPoints;
        
        [SerializeField]
        private bool useDirectReference = true;
        
        private void Start()
        {
            // Start spawning enemies
            StartCoroutine(SpawnEnemies());
        }
        
        /// <summary>
        /// Coroutine that continuously spawns enemies.
        /// </summary>
        private IEnumerator SpawnEnemies()
        {
            while (true)
            {
                yield return new WaitForSeconds(spawnInterval);
                SpawnEnemy();
            }
        }
        
        /// <summary>
        /// Spawns a single enemy using the configured method.
        /// </summary>
        private void SpawnEnemy()
        {
            if (useDirectReference)
            {
                SpawnUsingDirectReference();
            }
            else
            {
                SpawnUsingPoolManager();
            }
        }
        
        /// <summary>
        /// Method 1: Spawn using direct pool reference (recommended).
        /// Best performance, compile-time safety, no string lookups.
        /// </summary>
        private void SpawnUsingDirectReference()
        {
            if (!enemyPool)
            {
                Debug.LogError("Enemy pool reference not assigned!", this);
                return;
            }
            
            // Get random spawn point
            var spawnPosition = GetRandomSpawnPosition(); // Will ensure Z is 0
            var spawnRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f)); // 2D rotation
            
            // Spawn Enemy2D at position
            var enemy = enemyPool.Spawn<Enemy2D>(spawnPosition, spawnRotation); // Changed to Enemy2D
            
            if (enemy)
            {
                // Configure the spawned enemy
                ConfigureEnemy(enemy); // Parameter type will be Enemy2D
            }
        }
        
        /// <summary>
        /// Method 2: Spawn using PoolManager lookup (alternative).
        /// Useful when pool reference isn't available at compile time.
        /// </summary>
        private void SpawnUsingPoolManager()
        {
            // Get pool from PoolManager
            var pool = PoolManager.GetPool(enemyPoolId);
            
            if (!pool)
            {
                Debug.LogError($"Pool with ID '{enemyPoolId}' not found!");
                return;
            }
            
            // Get random spawn point
            var spawnPosition = GetRandomSpawnPosition(); // Will ensure Z is 0
            var spawnRotation = Quaternion.Euler(0, 0, Random.Range(0, 360f)); // 2D rotation
            
            // Spawn Enemy2D at position
            var enemy = pool.Spawn<Enemy2D>(spawnPosition, spawnRotation); // Changed to Enemy2D
            
            if (enemy)
            {
                // Configure the spawned enemy
                ConfigureEnemy(enemy); // Parameter type will be Enemy2D
            }
        }
        
        /// <summary>
        /// Example of spawning without caring about the component type.
        /// </summary>
        public void SpawnGenericObject()
        {
            if (enemyPool)
            {
                // Just spawn the object without getting a specific component
                var obj = enemyPool.Spawn();
                
                // The object will initialize itself through events
                // No need to do anything else
            }
        }
        
        /// <summary>
        /// Example of spawning multiple objects at once.
        /// </summary>
        public void SpawnWave(int enemyCount)
        {
            for (var i = 0; i < enemyCount; i++)
            {
                var position = GetRandomSpawnPosition(); // Will ensure Z is 0
                var angle = (360f / enemyCount) * i;
                var rotation = Quaternion.Euler(0, 0, angle); // 2D rotation
                
                // Spawning Enemy2D implicitly here if enemyPool is configured for Enemy2D prefabs
                enemyPool.Spawn(position, rotation);
            }
        }
        
        /// <summary>
        /// Gets a random spawn position from configured spawn points, ensuring Z is 0 for 2D.
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            Vector3 position;
            if (spawnPoints is not { Length: > 0 })
            {
                position = transform.position;
            }
            else
            {
                var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
                position = spawnPoint ? spawnPoint.position : transform.position;
            }
            position.z = 0; // Ensure Z is 0 for 2D
            return position;
            // Default to spawner position if no spawn points
        }
        
        /// <summary>
        /// Configure the spawned 2D enemy with any additional setup.
        /// </summary>
        private void ConfigureEnemy(Enemy2D enemy) // Changed parameter type to Enemy2D
        {
            // Example: Set target, add to an enemy list, etc.
            // enemy.SetTarget(player); // Assuming player is Vector2 or player.transform
            // enemyManager.RegisterEnemy(enemy);
        }
        
        /// <summary>
        /// Example of checking pool statistics.
        /// </summary>
        [ContextMenu("Log Pool Statistics")]
        private void LogPoolStatistics()
        {
            if (enemyPool)
            {
                Debug.Log($"Direct Pool - Available: {enemyPool.AvailableObjectCount}, " +
                         $"Total: {enemyPool.TotalObjectCount}, " +
                         $"Overflow: {enemyPool.OverflowCount}");
            }
            
            // Log all pools
            Debug.Log(PoolManager.GetPoolStatistics());
        }
        
        /// <summary>
        /// Example of despawning all enemies.
        /// </summary>
        [ContextMenu("Despawn All Enemies")]
        public void DespawnAllEnemies()
        {
            if (enemyPool)
            {
                enemyPool.DespawnAll();
            }
        }
    }
}