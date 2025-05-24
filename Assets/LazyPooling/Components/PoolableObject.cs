using System;
using LazyPooling.Core;
using UnityEngine;

namespace LazyPooling.Components
{
    /// <summary>
    /// MonoBehaviour component that makes any GameObject poolable.
    /// Attach this to prefabs you want to use with the pooling system.
    /// Provides event-driven initialization and cleanup through C# events.
    /// </summary>
    public class PoolableObject : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// Event fired when this object is spawned from the pool.
        /// Subscribe to this event to perform initialization logic.
        /// </summary>
        public event Action OnSpawnEvent;
        
        /// <summary>
        /// Event fired when this object is returned to the pool.
        /// Subscribe to this event to perform cleanup logic.
        /// </summary>
        public event Action OnDespawnEvent;
        
        /// <summary>
        /// Reference to the parent pool that manages this object.
        /// Set automatically by the pool during creation.
        /// </summary>
        private Pool _parentPool;
        
        /// <summary>
        /// Indicates whether this object is temporary (created due to pool overflow).
        /// Temporary objects are destroyed instead of returned to the pool.
        /// </summary>
        private bool _isTemporary;
        
        /// <summary>
        /// Gets whether this object is a temporary overflow instance.
        /// </summary>
        public bool IsTemporary => _isTemporary;
        
        /// <summary>
        /// Sets the parent pool reference. Called internally by the Pool.
        /// </summary>
        /// <param name="pool">The pool that manages this object</param>
        internal void SetParentPool(Pool pool)
        {
            _parentPool = pool;
        }
        
        /// <summary>
        /// Called when the object is spawned from the pool.
        /// Invokes the OnSpawnEvent for all subscribers.
        /// </summary>
        public void OnSpawn()
        {
            OnSpawnEvent?.Invoke();
        }
        
        /// <summary>
        /// Called when the object is returned to the pool.
        /// Invokes the OnDespawnEvent for all subscribers.
        /// </summary>
        public void OnDespawn()
        {
            OnDespawnEvent?.Invoke();
        }
        
        /// <summary>
        /// Returns this object to its parent pool.
        /// Convenience method that calls Despawn on the parent pool.
        /// </summary>
        public void ReturnToPool()
        {
            if (_parentPool != null)
            {
                _parentPool.Despawn(this);
            }
            else
            {
                Debug.LogWarning($"[LazyPooling] PoolableObject on {name} has no parent pool reference. Destroying object.", this);
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Marks this object as temporary (overflow instance).
        /// Temporary objects are destroyed when returned instead of being reused.
        /// </summary>
        internal void MarkAsTemporary()
        {
            _isTemporary = true;
        }
    }
}
