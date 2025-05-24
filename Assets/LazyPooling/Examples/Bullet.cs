using UnityEngine;
using System.Collections;
using LazyPooling.Components;

namespace LazyPooling.Examples
{
    /// <summary>
    /// Example projectile script showing automatic return to pool.
    /// Demonstrates lifetime management and collision-based despawning.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField]
        private float speed = 20f;
        
        [SerializeField]
        private int damage = 10;
        
        [SerializeField]
        private float lifetime = 5f;
        
        [Header("Components")]
        [SerializeField]
        private Rigidbody rb;
        
        [SerializeField]
        private TrailRenderer trailRenderer;
        
        private PoolableObject poolable;
        private Coroutine lifetimeCoroutine;
        
        private void Awake()
        {
            // Get components if not assigned
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            
            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();
            
            // Get poolable component
            poolable = GetComponent<PoolableObject>();
            
            // Subscribe to pool events
            if (poolable != null)
            {
                poolable.OnSpawnEvent += OnSpawnFromPool;
                poolable.OnDespawnEvent += OnReturnToPool;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (poolable != null)
            {
                poolable.OnSpawnEvent -= OnSpawnFromPool;
                poolable.OnDespawnEvent -= OnReturnToPool;
            }
        }
        
        /// <summary>
        /// Called when projectile is spawned from pool.
        /// </summary>
        private void OnSpawnFromPool()
        {
            // Reset trail renderer
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
            
            // Reset physics
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Start lifetime countdown
            lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
        }
        
        /// <summary>
        /// Called when projectile returns to pool.
        /// </summary>
        private void OnReturnToPool()
        {
            // Stop lifetime coroutine
            if (lifetimeCoroutine != null)
            {
                StopCoroutine(lifetimeCoroutine);
                lifetimeCoroutine = null;
            }
            
            // Clear any ongoing effects
            ClearEffects();
        }
        
        /// <summary>
        /// Launch the projectile in a direction.
        /// </summary>
        public void Launch(Vector3 direction)
        {
            if (rb != null)
            {
                rb.linearVelocity = direction.normalized * speed;
            }
        }
        
        /// <summary>
        /// Launch the projectile toward a target position.
        /// </summary>
        public void LaunchToward(Vector3 targetPosition)
        {
            var direction = (targetPosition - transform.position).normalized;
            Launch(direction);
        }
        
        /// <summary>
        /// Coroutine that returns projectile to pool after lifetime expires.
        /// </summary>
        private IEnumerator LifetimeCountdown()
        {
            yield return new WaitForSeconds(lifetime);
            ReturnToPool();
        }
        
        /// <summary>
        /// Handle collision with other objects.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            // Check for enemy layer
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Try to damage enemy
                var enemy = collision.gameObject.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                
                // Play hit effect
                PlayHitEffect(collision.contacts[0].point);
            }
            
            // Return to pool after hitting anything
            ReturnToPool();
        }
        
        /// <summary>
        /// Return this projectile to the pool.
        /// </summary>
        private void ReturnToPool()
        {
            poolable.ReturnToPool();
        }
        
        /// <summary>
        /// Play visual effect at hit location.
        /// </summary>
        private void PlayHitEffect(Vector3 position)
        {
            // Example: Spawn hit effect from another pool
            /*
            Pool effectPool = PoolManager.GetPool("HitEffectPool");
            if (effectPool != null)
            {
                effectPool.Spawn(position, Quaternion.identity);
            }
            */
        }
        
        /// <summary>
        /// Clear any ongoing visual effects.
        /// </summary>
        private void ClearEffects()
        {
            // Reset any particle systems, audio, etc.
        }
    }
}