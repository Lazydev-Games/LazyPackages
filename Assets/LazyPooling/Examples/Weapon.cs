using LazyPooling.Components;
using UnityEngine;

namespace LazyPooling.Examples
{
    /// <summary>
    /// Example weapon system showing how to spawn projectiles from pools.
    /// Demonstrates high-frequency spawning patterns.
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        [Header("Weapon Settings")]
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
            
            // Add spread
            var spread = Random.Range(-spreadAngle, spreadAngle);
            spawnRotation *= Quaternion.Euler(0, spread, 0);
            
            // Spawn projectile from pool
            var projectile = projectilePool.Spawn<Projectile>(spawnPosition, spawnRotation);
            
            if (projectile)
            {
                // Launch the projectile
                var fireDirection = spawnRotation * Vector3.forward;
                projectile.Launch(fireDirection * muzzleVelocity);
                
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
                var spawnRotation = muzzlePoint.rotation * Quaternion.Euler(0, angle, 0);
                
                // Spawn projectile
                var projectile = projectilePool.Spawn<Projectile>(spawnPosition, spawnRotation);
                
                if (projectile)
                {
                    var fireDirection = spawnRotation * Vector3.forward;
                    projectile.Launch(fireDirection * muzzleVelocity);
                }
            }
            
            PlayMuzzleEffects();
        }
        
        /// <summary>
        /// Fire projectile at a specific target.
        /// </summary>
        public void FireAtTarget(Transform target)
        {
            if (!projectilePool || !target) return;
            
            var spawnPosition = muzzlePoint.position;
            
            // Calculate the direction to target
            var directionToTarget = (target.position - spawnPosition).normalized;
            var spawnRotation = Quaternion.LookRotation(directionToTarget);
            
            // Spawn and launch
            var projectile = projectilePool.Spawn<Projectile>(spawnPosition, spawnRotation);
            if (projectile)
            {
                projectile.LaunchToward(target.position);
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
        /// Example of prewarming the projectile pool.
        /// Call this during loading screens or setup.
        /// </summary>
        [ContextMenu("Prewarm Projectile Pool")]
        public void PrewarmPool()
        {
            if (!projectilePool) return;
            
            // Spawn and immediately despawn to ensure pool is filled
            for (var i = 0; i < 10; i++)
            {
                var obj = projectilePool.Spawn();
                if (obj)
                {
                    obj.ReturnToPool();
                }
            }
            
            Debug.Log("Projectile pool prewarmed");
        }
    }
}