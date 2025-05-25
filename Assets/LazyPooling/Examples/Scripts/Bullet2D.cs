using UnityEngine;
using System.Collections;
using LazyPooling.Components;

namespace LazyPooling.Examples.Scripts
{
    /// <summary>
    /// Example 2D bullet script showing automatic return to pool.
    /// Demonstrates lifetime management and collision-based despawning in a 2D context.
    /// </summary>
    public class Bullet2D : MonoBehaviour // Renamed class
    {
        [Header("Bullet Settings")] // Renamed header
        [SerializeField]
        private float speed = 20f;
        
        [SerializeField]
        private int damage = 10;
        
        [SerializeField]
        private float lifetime = 5f;
        
        [Header("Components")]
        [SerializeField]
        private Rigidbody2D rb; // Changed to Rigidbody2D
        
        [SerializeField]
        private TrailRenderer trailRenderer; // TrailRenderer can work in 2D with correct setup
        
        private PoolableObject poolable;
        private Coroutine lifetimeCoroutine;
        
        private void Awake()
        {
            // Get components if not assigned
            if (rb == null)
                rb = GetComponent<Rigidbody2D>(); // Changed to GetComponent<Rigidbody2D>()
            
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
        /// Called when bullet is spawned from pool.
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
                rb.velocity = Vector2.zero; // Changed to Vector2.zero for Rigidbody2D
                rb.angularVelocity = 0f; // Rigidbody2D angularVelocity is a float
            }
            
            // Start lifetime countdown
            lifetimeCoroutine = StartCoroutine(LifetimeCountdown());
        }
        
        /// <summary>
        /// Called when bullet returns to pool.
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
        /// Launch the bullet in a 2D direction.
        /// </summary>
        public void Launch(Vector2 direction) // Changed parameter to Vector2
        {
            if (rb != null)
            {
                rb.velocity = direction.normalized * speed; // Rigidbody2D uses velocity
            }
        }
        
        /// <summary>
        /// Launch the bullet toward a 2D target position.
        /// </summary>
        public void LaunchToward(Vector2 targetPosition) // Changed parameter to Vector2
        {
            var direction = (targetPosition - (Vector2)transform.position).normalized; // Ensure 2D context
            Launch(direction);
        }
        
        /// <summary>
        /// Coroutine that returns bullet to pool after lifetime expires.
        /// </summary>
        private IEnumerator LifetimeCountdown()
        {
            yield return new WaitForSeconds(lifetime);
            ReturnToPool();
        }
        
        /// <summary>
        /// Handle 2D collision with other objects.
        /// </summary>
        private void OnCollisionEnter2D(Collision2D collision) // Changed to OnCollisionEnter2D
        {
            // Check for enemy layer
            // Ensure your 2D Enemy prefab is on the "Enemy" layer
            if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                // Try to damage enemy
                var enemy = collision.gameObject.GetComponent<Enemy2D>(); // Changed to Enemy2D
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                }
                
                // Play hit effect
                PlayHitEffect(collision.contacts[0].point); // contacts[0].point is Vector2 in 2D
            }
            
            // Return to pool after hitting anything
            ReturnToPool();
        }
        
        /// <summary>
        /// Return this bullet to the pool.
        /// </summary>
        private void ReturnToPool()
        {
            if (poolable != null) // Add null check for safety
            {
                poolable.ReturnToPool();
            }
        }
        
        /// <summary>
        /// Play visual effect at 2D hit location.
        /// </summary>
        private void PlayHitEffect(Vector2 position) // Changed parameter to Vector2
        {
            // Example: Spawn hit effect from another pool
            /*
            Pool effectPool = PoolManager.GetPool("HitEffectPool2D"); // Consider a 2D specific effect pool
            if (effectPool != null)
            {
                effectPool.Spawn(position, Quaternion.identity); // Spawn at Vector2 position
            }
            */
        }
        
        /// <summary>
        /// Clear any ongoing visual effects.
        /// </summary>
        private void ClearEffects()
        {
            // Reset any particle systems, audio, etc.
            // For TrailRenderer, ensure it's reset if it persists across pool uses.
            if (trailRenderer != null)
            {
                trailRenderer.Clear();
            }
        }
    }
}