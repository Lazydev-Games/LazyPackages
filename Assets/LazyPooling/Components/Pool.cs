using System.Collections.Generic;
using LazyPooling.Core;
using UnityEngine;

namespace LazyPooling.Components
{
    /// <summary>
    /// Manages a pool of objects for a specific prefab type.
    /// Handles object lifecycle, spawning, despawning, and overflow management.
    /// Self-registers with PoolManager on initialization.
    /// </summary>
    public class Pool : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField]
        [Tooltip("Unique identifier for this pool. Used for PoolManager lookups.")]
        private string poolId;

        [SerializeField] [Tooltip("The prefab to instantiate for this pool. Must have a PoolableObject component.")]
        private PoolableObject prefab;

        [SerializeField] [Tooltip("Initial number of objects to create in the pool.")]
        private int poolSize = 50;

        [SerializeField] [Tooltip("If true, this pool persists across scene changes.")]
        private bool persistAcrossScenes;

        [SerializeField] [Tooltip("If true, logs warnings when creating temporary overflow objects.")]
        private bool logOverflowWarnings = true;

        /// <summary>
        /// Queue of available objects ready to be spawned.
        /// </summary>
        private Queue<PoolableObject> _availableObjects;

        /// <summary>
        /// List of all objects managed by this pool (including active ones).
        /// </summary>
        private List<PoolableObject> _pooledObjects;

        /// <summary>
        /// Transform parent for organizing pooled objects in the hierarchy.
        /// </summary>
        private Transform _containerParent;

        /// <summary>
        /// Counter for tracking temporary overflow instances.
        /// </summary>
        private int _overflowCount;

        /// <summary>
        /// Gets the unique identifier for this pool.
        /// </summary>
        public string PoolId => poolId;

        /// <summary>
        /// Gets the current number of temporary overflow objects.
        /// </summary>
        public int OverflowCount => _overflowCount;

        /// <summary>
        /// Gets the total number of objects managed by this pool.
        /// </summary>
        public int TotalObjectCount => _pooledObjects.Count;

        /// <summary>
        /// Gets the number of objects currently available in the pool.
        /// </summary>
        public int AvailableObjectCount => _availableObjects.Count;

        private void Awake()
        {
            ValidateConfiguration();
            InitializePool();
            RegisterWithPoolManager();

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        /// <summary>
        /// Validates the pool configuration and logs errors if invalid.
        /// </summary>
        private void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(poolId))
            {
                Debug.LogError($"[LazyPooling] Pool on {name} has no poolId set!", this);
            }

            if (!prefab)
            {
                Debug.LogError($"[LazyPooling] Pool '{poolId}' has no prefab assigned!", this);
            }

            if (poolSize < 0)
            {
                Debug.LogWarning($"[LazyPooling] Pool '{poolId}' has negative pool size. Setting to 0.", this);
                poolSize = 0;
            }
        }

        /// <summary>
        /// Initializes the pool data structures and warms up the pool.
        /// </summary>
        private void InitializePool()
        {
            _availableObjects = new Queue<PoolableObject>(poolSize);
            _pooledObjects = new List<PoolableObject>(poolSize);

            WarmPool();
        }

        /// <summary>
        /// Registers this pool with the PoolManager.
        /// </summary>
        private void RegisterWithPoolManager()
        {
            if (!string.IsNullOrEmpty(poolId))
            {
                PoolManager.RegisterPool(poolId, this);
            }
        }

        /// <summary>
        /// Sets the container parent for organizing pooled objects.
        /// Called by PoolManager during registration.
        /// </summary>
        /// <param name="parent">The transform to use as a parent for pooled objects</param>
        internal void SetContainerParent(Transform parent)
        {
            _containerParent = parent;

            // Move existing objects to the new parent
            foreach (var obj in _pooledObjects)
            {
                if (obj)
                {
                    obj.transform.SetParent(_containerParent);
                }
            }
        }

        /// <summary>
        /// Creates the initial pool of objects.
        /// </summary>
        private void WarmPool()
        {
            if (!prefab) return;

            for (var i = 0; i < poolSize; i++)
            {
                CreatePooledObject();
            }
        }

        /// <summary>
        /// Creates a new pooled object and adds it to the pool.
        /// </summary>
        /// <returns>The created PoolableObject</returns>
        private PoolableObject CreatePooledObject()
        {
            var obj = Instantiate(prefab.gameObject, _containerParent);
            obj.name = $"{prefab.name}_Pooled_{_pooledObjects.Count}";
            obj.SetActive(false);

            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable)
            {
                poolable.SetParentPool(this);
                _pooledObjects.Add(poolable);
                _availableObjects.Enqueue(poolable);
            }

            return poolable;
        }

        /// <summary>
        /// Creates a temporary overflow instance when the pool is exhausted.
        /// </summary>
        /// <returns>The created temporary PoolableObject</returns>
        private PoolableObject CreateTemporaryInstance()
        {
            if (logOverflowWarnings)
            {
                Debug.LogWarning($"[LazyPooling] Pool '{poolId}' is creating temporary overflow object. " +
                                 $"Consider increasing pool size from {poolSize}. Current overflow: {_overflowCount + 1}",
                    this);
            }

            GameObject obj = Instantiate(prefab.gameObject, _containerParent);
            obj.name = $"{prefab.name}_Temp_{_overflowCount}";

            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable)
            {
                poolable.SetParentPool(this);
                poolable.MarkAsTemporary();
                _overflowCount++;
            }

            return poolable;
        }

        /// <summary>
        /// Spawns an object from the pool at the default position and rotation.
        /// </summary>
        /// <returns>The spawned PoolableObject</returns>
        public PoolableObject Spawn()
        {
            return Spawn(Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Spawns an object from the pool at the specified position and rotation.
        /// </summary>
        /// <param name="position">World position for the spawned object</param>
        /// <param name="rotation">World rotation for the spawned object</param>
        /// <returns>The spawned PoolableObject</returns>
        public PoolableObject Spawn(Vector3 position, Quaternion rotation)
        {
            var obj = _availableObjects.Count > 0 ? _availableObjects.Dequeue() : CreateTemporaryInstance();

            if (!obj) return obj;

            var objTransform = obj.transform;
            objTransform.position = position;
            objTransform.rotation = rotation;

            obj.gameObject.SetActive(true);
            obj.OnSpawn();

            return obj;
        }

        /// <summary>
        /// Spawns an object from the pool and returns a specific component.
        /// </summary>
        /// <typeparam name="T">The component type to return</typeparam>
        /// <returns>The requested component on the spawned object</returns>
        public T Spawn<T>() where T : Component
        {
            return Spawn<T>(Vector3.zero, Quaternion.identity);
        }

        /// <summary>
        /// Spawns an object from the pool at the specified position and rotation,
        /// returning a specific component.
        /// </summary>
        /// <typeparam name="T">The component type to return</typeparam>
        /// <param name="position">World position for the spawned object</param>
        /// <param name="rotation">World rotation for the spawned object</param>
        /// <returns>The requested component on the spawned object</returns>
        public T Spawn<T>(Vector3 position, Quaternion rotation) where T : Component
        {
            var obj = Spawn(position, rotation);
            return obj ? obj.GetComponent<T>() : null;
        }

        /// <summary>
        /// Returns an object to the pool or destroys it if temporary.
        /// </summary>
        /// <param name="obj">The PoolableObject to despawn</param>
        public void Despawn(PoolableObject obj)
        {
            if (!obj) return;

            obj.OnDespawn();

            if (obj.IsTemporary)
            {
                _overflowCount--;
                Destroy(obj.gameObject);
            }
            else
            {
                obj.gameObject.SetActive(false);
                obj.transform.SetParent(_containerParent);
                _availableObjects.Enqueue(obj);
            }
        }

        /// <summary>
        /// Despawns all active objects back to the pool.
        /// Useful for level resets or cleanup.
        /// </summary>
        public void DespawnAll()
        {
            var objectsToDespawn = new List<PoolableObject>();

            foreach (var obj in _pooledObjects)
            {
                if (obj && obj.gameObject.activeSelf)
                {
                    objectsToDespawn.Add(obj);
                }
            }

            foreach (var obj in objectsToDespawn)
            {
                Despawn(obj);
            }
        }

        private void OnDestroy()
        {
            // Clean up temporary objects
            foreach (var obj in _pooledObjects)
            {
                if (obj && obj.IsTemporary)
                {
                    Destroy(obj.gameObject);
                }
            }
        }
    }
}