using UnityEngine;
using LazyPooling.Core; // For Pool
using LazyPooling.Components; // For PoolableObject (though not directly used, good for context)

namespace LazyPooling.Examples.Scripts
{
    /// <summary>
    /// Spawns FX objects from a pre-configured pool when the spacebar is pressed.
    /// </summary>
    public class FXSpawner : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField]
        [Tooltip("The pool configured to spawn FX objects. Assign this in the Inspector.")]
        private Pool fxPool;

        [Header("Spawn Settings")]
        [SerializeField]
        [Tooltip("Optional: If set, FX will spawn at this point. Otherwise, spawns at FXSpawner's position.")]
        private Transform spawnPoint;

        private void Start()
        {
            if (fxPool == null)
            {
                Debug.LogError("FX Pool is not assigned in the FXSpawner component. Please assign it in the Inspector.", this);
            }

            if (spawnPoint == null)
            {
                // Default to this spawner's transform if no specific spawn point is set
                spawnPoint = transform;
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (fxPool != null)
                {
                    // Spawn the FX object from the pool at the specified spawn point's position and rotation
                    // The FXObject script on the prefab will handle playing its effects via OnSpawnEvent
                    GameObject spawnedFX = fxPool.Spawn(spawnPoint.position, spawnPoint.rotation);

                    if (spawnedFX == null)
                    {
                        Debug.LogWarning("FX Pool returned null. Pool might be empty and overflow is disabled, or max overflow reached.", this);
                    }
                }
                // The else case (fxPool == null) is already handled by the Start method's error log.
            }
        }
    }
}
