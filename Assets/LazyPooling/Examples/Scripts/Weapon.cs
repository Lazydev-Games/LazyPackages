using LazyPooling.Components;
using UnityEngine;

namespace LazyPooling.Examples.Scripts
{
    /// <summary>
    /// Example 2D weapon system showing how to spawn 2D bullets from pools.
    /// Demonstrates high-frequency spawning patterns in a 2D context.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Settings (2D)")]
        [SerializeField]
        private float fireRate = 0.1f; // 10 shots per second
        
        [SerializeField]
        private int burstCount = 3;
        
        [SerializeField]
        private float burstDelay = 0.05f;
        
        [Header("Pool Reference")]
        [SerializeField]
        private Pool projectilePool;
        
        [Header("Spawn Settings")]
        [SerializeField]
        private Transform muzzlePoint;
        
        [SerializeField]
        private float muzzleVelocity = 50f;
        
        [SerializeField]
        private float spreadAngle = 5f;
        
        private float _nextFireTime;
        private bool _isFiring;
        
        private void Start()
        {
            // Validate setup
            if (!projectilePool)
            {
                Debug.LogError("Projectile pool not assigned to weapon!", this);
            }
            
            if (!muzzlePoint)
            {
                Debug.LogWarning("Muzzle point not assigned, using weapon transform.", this);
                muzzlePoint = transform;
            }
        }
        
        private void Update()
        {
            // Example input handling
            if (Input.GetButton("Fire1"))
            {
                TryFire();
            }
            
            if (Input.GetButtonDown("Fire2"))
            {
                FireBurst();
            }
        }
        
        /// <summary>
        /// Attempt to fire the weapon if fire rate allows.
        /// </summary>
        private void TryFire()
        {
            if (!(Time.time >= _nextFireTime) || !projectilePool) return;
            Fire();
            _nextFireTime = Time.time + fireRate;
        }
        
        /// <summary>
        /// Fire a single projectile.
        /// </summary>
        public void Fire()
        {
            if (projectilePool == null) return;
            
            // Calculate spawn position and rotation
            var spawnPosition = muzzlePoint.position;
            var spawnRotation = muzzlePoint.rotation;
            
            // Add spread (2D rotation is around Z-axis)
            var spread = Random.Range(-spreadAngle, spreadAngle);
            // Assuming muzzlePoint.rotation is already set for 2D (i.e., Z rotation is meaningful)
            Quaternion finalRotation = spawnRotation * Quaternion.Euler(0, 0, spread);
            
            // Spawn Bullet2D from pool
            var bullet = projectilePool.Spawn<Bullet2D>(spawnPosition, finalRotation);
            
            if (bullet)
            {
                // Launch the Bullet2D
                // In 2D, 'right' is often used as the forward vector if sprites are oriented that way.
                // Or, use muzzlePoint.transform.right if the muzzlePoint is rotated to aim.
                Vector2 fireDirection = finalRotation * Vector2.right; // Use Vector2.right for 2D forward
                bullet.Launch(fireDirection * muzzleVelocity);
                
                // Play muzzle effects
                PlayMuzzleEffects();
            }
        }
        
        /// <summary>
        /// Fire a burst of projectiles.
        /// </summary>
        public void FireBurst()
        {
            if (!_isFiring)
            {
                StartCoroutine(BurstFire());
            }
        }
        
        /// <summary>
        /// Coroutine for burst fire mode.
        /// </summary>
        private System.Collections.IEnumerator BurstFire()
        {
            _isFiring = true;
            
            for (var i = 0; i < burstCount; i++)
            {
                Fire();
                yield return new WaitForSeconds(burstDelay);
            }
            
            _isFiring = false;
            _nextFireTime = Time.time + fireRate;
        }
        
        /// <summary>
        /// Fire a spread of projectiles in a cone.
        /// </summary>
        public void FireShotgun(int pelletCount = 8)
        {
            if (!projectilePool) return;
            
            var angleStep = spreadAngle * 2f / (pelletCount - 1);
            var startAngle = -spreadAngle;
            
            for (var i = 0; i < pelletCount; i++)
            {
                var spawnPosition = muzzlePoint.position;
                var angle = startAngle + (angleStep * i);
                // Assuming muzzlePoint.rotation is set for 2D base direction
                var finalRotation = muzzlePoint.rotation * Quaternion.Euler(0, 0, angle); // 2D spread
                
                // Spawn Bullet2D
                var bullet = projectilePool.Spawn<Bullet2D>(spawnPosition, finalRotation);
                
                if (bullet)
                {
                    Vector2 fireDirection = finalRotation * Vector2.right; // Use Vector2.right for 2D forward
                    bullet.Launch(fireDirection * muzzleVelocity);
                }
            }
            
            PlayMuzzleEffects();
        }
        
        /// <summary>
        /// Fire bullet at a specific 2D target.
        /// </summary>
        public void FireAtTarget(Transform target)
        {
            if (!projectilePool || !target) return;
            
            Vector2 spawnPosition = muzzlePoint.position; // Using Vector2 for positions
            
            // Calculate the 2D direction to target
            Vector2 directionToTarget = ((Vector2)target.position - spawnPosition).normalized;
            
            // For 2D, rotation is typically around the Z-axis
            // Angle calculation:
            float angle = Mathf.Atan2(directionToTarget.y, directionToTarget.x) * Mathf.Rad2Deg;
            Quaternion spawnRotation = Quaternion.Euler(0, 0, angle);
            
            // Spawn and launch Bullet2D
            var bullet = projectilePool.Spawn<Bullet2D>(spawnPosition, spawnRotation);
            if (bullet)
            {
                // LaunchToward expects a Vector2 target position for Bullet2D
                bullet.LaunchToward((Vector2)target.position);
            }
        }
        
        /// <summary>
        /// Play visual and audio effects at the muzzle.
        /// </summary>
        private void PlayMuzzleEffects()
        {
            // Example: Spawn muzzle flash from another pool
            /*
            Pool muzzleFlashPool = PoolManager.GetPool("MuzzleFlashPool");
            if (muzzleFlashPool != null)
            {
                muzzleFlashPool.Spawn(muzzlePoint.position, muzzlePoint.rotation);
            }
            */
            
            // Play audio, recoil animation, etc.
        }
        
        /// <summary>
        /// Example of prewarming the bullet pool.
        /// Call this during loading screens or setup.
        /// </summary>
        [ContextMenu("Prewarm Bullet Pool")] // Renamed ContextMenu
        public void PrewarmPool()
        {
            if (!projectilePool) return;
            
            // Spawn and immediately despawn to ensure pool is filled
            // Spawning a generic GameObject and getting PoolableObject is okay for prewarming.
            for (var i = 0; i < 10; i++)
            {
                var poolableObj = projectilePool.Spawn().GetComponent<PoolableObject>();
                if (poolableObj)
                {
                    poolableObj.ReturnToPool();
                }
            }
            
            Debug.Log("Bullet pool prewarmed");
        }
    }
}