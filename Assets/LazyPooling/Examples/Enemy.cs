using LazyPooling.Components;
using UnityEngine;

namespace LazyPooling.Examples
{
    /// <summary>
    /// Example enemy script showing how to subscribe to pooling events.
    /// Demonstrates proper initialization and cleanup patterns.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [SerializeField]
        private int maxHealth = 100;
        
        [SerializeField]
        private float moveSpeed = 5f;
        
        private int _currentHealth;
        private PoolableObject _poolable;
        
        private void Awake()
        {
            // Get reference to the PoolableObject component
            _poolable = GetComponent<PoolableObject>();
            
            if (_poolable)
            {
                // Subscribe to pooling events
                _poolable.OnSpawnEvent += OnSpawnFromPool;
                _poolable.OnDespawnEvent += OnReturnToPool;
            }
        }
        
        private void OnDestroy()
        {
            //unsubscribe from events to prevent memory leaks
            if (_poolable)
            {
                _poolable.OnSpawnEvent -= OnSpawnFromPool;
                _poolable.OnDespawnEvent -= OnReturnToPool;
            }
        }
        
        /// <summary>
        /// Called when this enemy is spawned from the pool.
        /// Initialize or reset all necessary states here.
        /// </summary>
        private void OnSpawnFromPool()
        {
            // Reset health
            _currentHealth = maxHealth;
            
            // Reset position if needed
            // transform.position = Vector3.zero;
            
            // Apply random variations
            ApplyRandomModifiers();
            
            // Start AI behavior
            StartCoroutine(AIBehavior());
            
            Debug.Log($"Enemy spawned with {_currentHealth} health");
        }
        
        /// <summary>
        /// Called when this enemy is returned to the pool.
        /// Clean up any ongoing processes or external references.
        /// </summary>
        private void OnReturnToPool()
        {
            // Stop all coroutines
            StopAllCoroutines();
            
            // Clear any target references
            ClearTargets();
            
            // Reset visual effects
            ResetVisuals();
            
            Debug.Log("Enemy returned to pool");
        }
        
        /// <summary>
        /// Apply random modifiers to make each spawn unique.
        /// </summary>
        private void ApplyRandomModifiers()
        {
            // Random health modifier
            var healthModifier = Random.Range(0.8f, 1.2f);
            _currentHealth = Mathf.RoundToInt(maxHealth * healthModifier);
            
            // Random speed modifier
            var speedModifier = Random.Range(0.9f, 1.1f);
            moveSpeed = 5f * speedModifier;
            
            // Random scale
            var scaleModifier = Random.Range(0.9f, 1.1f);
            transform.localScale = Vector3.one * scaleModifier;
        }
        
        /// <summary>
        /// Take damage and return to the pool if health reaches zero.
        /// </summary>
        public void TakeDamage(int damage)
        {
            _currentHealth -= damage;
            
            if (_currentHealth <= 0)
            {
                Die();
            }
        }
        
        /// <summary>
        /// Handle enemy death by returning to the pool.
        /// </summary>
        private void Die()
        {
            // Play death effects before returning
            // PlayDeathEffect();
            
            // Return to the pool instead of destroying
            _poolable.ReturnToPool();
        }
        
        private System.Collections.IEnumerator AIBehavior()
        {
            while (true)
            {
                // Simple AI behavior
                yield return new WaitForSeconds(1f);
                // Move, attack, etc.
            }
        }
        
        private void ClearTargets()
        {
            // Clear any references to other game objects
        }
        
        private void ResetVisuals()
        {
            // Reset any visual modifications
        }
    }
}