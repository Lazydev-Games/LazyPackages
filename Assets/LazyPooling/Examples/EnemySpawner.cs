using UnityEngine;
using System.Collections;
using LazyPooling.Components;
using LazyPooling.Core;

namespace LazyPooling.Examples
{
    /// <summary>
    /// Example spawner showing different ways to use the pooling system.
    /// Demonstrates both direct pool reference and PoolManager lookup methods.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Direct Pool Reference (Recommended)")]
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
            var spawnPosition = GetRandomSpawnPosition();
            var spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Spawn enemy at position
            var enemy = enemyPool.Spawn<Enemy>(spawnPosition, spawnRotation);
            
            if (enemy)
            {
                // Configure the spawned enemy
                ConfigureEnemy(enemy);
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
            var spawnPosition = GetRandomSpawnPosition();
            var spawnRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            
            // Spawn enemy at position
            var enemy = pool.Spawn<Enemy>(spawnPosition, spawnRotation);
            
            if (enemy)
            {
                // Configure the spawned enemy
                ConfigureEnemy(enemy);
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
                var position = GetRandomSpawnPosition();
                var angle = (360f / enemyCount) * i;
                var rotation = Quaternion.Euler(0, angle, 0);
                
                enemyPool.Spawn(position, rotation);
            }
        }
        
        /// <summary>
        /// Gets a random spawn position from configured spawn points.
        /// </summary>
        private Vector3 GetRandomSpawnPosition()
        {
            if (spawnPoints is not { Length: > 0 }) return transform.position;
            var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return spawnPoint ? spawnPoint.position : transform.position;

            // Default to spawner position if no spawn points
        }
        
        /// <summary>
        /// Configure the spawned enemy with any additional setup.
        /// </summary>
        private void ConfigureEnemy(Enemy enemy)
        {
            // Example: Set target, add to an enemy list, etc.
            // enemy.SetTarget(player);
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