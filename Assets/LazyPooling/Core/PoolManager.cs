using System.Collections.Generic;
using LazyPooling.Components;
using UnityEngine;

namespace LazyPooling.Core
{
    /// <summary>
    /// Singleton manager that provides central registry and lookup for all pools.
    /// Automatically created when first accessed and persists across scenes.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance of the PoolManager.
        /// </summary>
        private static PoolManager _instance;
        
        /// <summary>
        /// Dictionary of all registered pools indexed by their unique IDs.
        /// </summary>
        private Dictionary<string, Pool> _registeredPools;
        
        /// <summary>
        /// Container GameObject for organizing pools in the hierarchy.
        /// </summary>
        private GameObject _poolContainer;
        
        /// <summary>
        /// Gets the singleton instance, creating it if necessary.
        /// </summary>
        private static PoolManager Instance
        {
            get
            {
                if (!_instance)
                {
                    CreateInstance();
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Creates the singleton instance of PoolManager.
        /// </summary>
        private static void CreateInstance()
        {
            var managerObject = new GameObject("[LazyPooling] PoolManager");
            _instance = managerObject.AddComponent<PoolManager>();
            DontDestroyOnLoad(managerObject);
        }
        
        private void Awake()
        {
            if (_instance && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            _registeredPools = new Dictionary<string, Pool>();
            
            CreatePoolContainer();
        }
        
        /// <summary>
        /// Creates the container GameObject for organizing pools.
        /// </summary>
        private void CreatePoolContainer()
        {
            _poolContainer = new GameObject("[LazyPooling] Pools");
            _poolContainer.transform.SetParent(transform);
        }
        
        /// <summary>
        /// Registers a pool with the manager.
        /// Called automatically by pools during their initialization.
        /// </summary>
        /// <param name="id">Unique identifier for the pool</param>
        /// <param name="pool">The pool instance to register</param>
        public static void RegisterPool(string id, Pool pool)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[LazyPooling] Cannot register pool with empty ID.", pool);
                return;
            }
            
            if (!pool)
            {
                Debug.LogError($"[LazyPooling] Cannot register null pool with ID '{id}'.");
                return;
            }
            
            var manager = Instance;
            
            if (manager._registeredPools.ContainsKey(id))
            {
                Debug.LogWarning($"[LazyPooling] Pool with ID '{id}' is already registered. " +
                               $"Replacing existing pool.", pool);
            }
            
            manager._registeredPools[id] = pool;
            
            // Create a container for this pool
            var poolContainerObject = new GameObject($"[Pool] {id}");
            poolContainerObject.transform.SetParent(manager._poolContainer.transform);
            pool.SetContainerParent(poolContainerObject.transform);
            
            Debug.Log($"[LazyPooling] Registered pool '{id}' with {pool.TotalObjectCount} objects.", pool);
        }
        
        /// <summary>
        /// Gets a pool by its unique identifier.
        /// </summary>
        /// <param name="poolId">The unique identifier of the pool</param>
        /// <returns>The pool instance, or null if not found</returns>
        public static Pool GetPool(string poolId)
        {
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogError("[LazyPooling] Cannot get pool with empty ID.");
                return null;
            }
            
            var manager = Instance;
            
            if (manager._registeredPools.TryGetValue(poolId, out var pool))
            {
                return pool;
            }
            
            Debug.LogError($"[LazyPooling] Pool with ID '{poolId}' not found. " +
                         $"Make sure the pool GameObject is active in the scene.");
            return null;
        }
        
        /// <summary>
        /// Gets a pool by its unique identifier, cast to a specific Pool subclass.
        /// </summary>
        /// <typeparam name="T">The specific Pool subclass type</typeparam>
        /// <param name="poolId">The unique identifier of the pool</param>
        /// <returns>The pool instance cast to type T, or null if not found</returns>
        public static T GetPool<T>(string poolId) where T : Pool
        {
            return GetPool(poolId) as T;
        }
        
        /// <summary>
        /// Attempts to find a pool by searching through all registered pools.
        /// This is slower than GetPool and should only be used as a fallback.
        /// </summary>
        /// <param name="poolId">The unique identifier of the pool</param>
        /// <returns>The pool instance, or null if not found</returns>
        public static Pool FindPool(string poolId)
        {
            Debug.LogWarning($"[LazyPooling] Using FindPool is not recommended. " +
                           $"Use GetPool for better performance.");
            return GetPool(poolId);
        }
        
        /// <summary>
        /// Gets all currently registered pools.
        /// </summary>
        /// <returns>Dictionary of all registered pools</returns>
        public static IReadOnlyDictionary<string, Pool> GetAllPools()
        {
            return Instance._registeredPools;
        }
        
        /// <summary>
        /// Checks if a pool with the given ID is registered.
        /// </summary>
        /// <param name="poolId">The unique identifier to check</param>
        /// <returns>True if a pool with this ID exists</returns>
        public static bool HasPool(string poolId)
        {
            if (string.IsNullOrEmpty(poolId)) return false;
            return Instance._registeredPools.ContainsKey(poolId);
        }
        
        /// <summary>
        /// Despawns all objects in all registered pools.
        /// Useful for level resets or cleanup.
        /// </summary>
        public static void DespawnAll()
        {
            foreach (var pool in Instance._registeredPools.Values)
            {
                if (pool)
                {
                    pool.DespawnAll();
                }
            }
        }
        
        /// <summary>
        /// Gets statistics about all registered pools.
        /// </summary>
        /// <returns>String containing pool statistics</returns>
        public static string GetPoolStatistics()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("[LazyPooling] Pool Statistics:");
            stats.AppendLine($"Total Pools: {Instance._registeredPools.Count}");
            
            foreach (var kvp in Instance._registeredPools)
            {
                var pool = kvp.Value;
                if (pool)
                {
                    stats.AppendLine($"  - {kvp.Key}: {pool.AvailableObjectCount}/{pool.TotalObjectCount} available, {pool.OverflowCount} overflow");
                }
            }
            
            return stats.ToString();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}