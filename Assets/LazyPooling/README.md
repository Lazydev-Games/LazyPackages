# LazyPooling for Unity

LazyPooling is a lightweight, efficient object pooling system designed for Unity projects. It helps manage object instantiation and destruction, reducing garbage collection and improving performance, especially in scenarios with frequently created and destroyed objects like projectiles, enemies, or visual effects.

## Key Features

*   **Zero Allocation (Normal Operation):** Once pools are initialized, spawning and despawning objects typically results in zero memory allocation, preventing GC spikes.
*   **Self-Managing Pools:** `Pool` components automatically register with the `PoolManager`, making setup straightforward.
*   **Event-Driven Initialization:** `PoolableObject` provides `OnSpawnEvent` and `OnDespawnEvent` for easy setup and cleanup logic.
*   **Type-Safe Spawning:** Get components directly from spawned objects with type safety.
*   **Overflow Handling:** Pools can temporarily exceed their configured size, with warnings to help identify when pool sizes need adjustment.
*   **Editor Friendly:** Configure pools directly in the Unity Inspector.

## Directory Structure

The LazyPooling system is organized as follows:

```
Assets/
└── LazyPooling/
    ├── README.md
    ├── Core/
    │   ├── IPoolable.cs
    │   └── PoolManager.cs
    ├── Components/
    │   ├── Pool.cs
    │   └── PoolableObject.cs
    └── Examples/
        ├── Bullet.cs
        ├── Enemy.cs
        ├── EnemySpawner.cs
        ├── Weapon.cs
        ├── Prefabs/
        │   ├── Bullet.prefab
        │   └── Enemy.prefab
        └── Scenes/
            └── (Example Scene - if applicable)
```

## 1. Quick Start Guide

### a. Importing LazyPooling

*   **Option 1 (Recommended):** Copy the entire `Assets/LazyPooling` folder into your Unity project's `Assets` directory.
*   **Option 2:** If you prefer a more customized setup, you can copy the `Core` and `Components` subfolders. The `Examples` folder is optional but recommended for understanding usage.

### b. Creating a Poolable Prefab

1.  Create your desired GameObject (e.g., a projectile, enemy).
2.  Add the `PoolableObject` component (from `Assets/LazyPooling/Components/PoolableObject.cs`) to the root of your prefab.
    *   This component handles the object's interaction with the pool.
3.  Save this GameObject as a Prefab.

### c. Creating a Pool in the Scene

1.  Create an empty GameObject in your scene (e.g., named "BulletPool").
2.  Add the `Pool` component (from `Assets/LazyPooling/Components/Pool.cs`) to this GameObject.
3.  Configure the `Pool` component in the Inspector:
    *   **Pool Id:** A unique string identifier for this pool (e.g., "Bullets", "Enemies"). This ID is used to retrieve the pool via the `PoolManager`.
    *   **Prefab:** Drag your poolable prefab (created in step b) here.
    *   **Pool Size:** The initial number of objects to pre-warm in the pool.
    *   **Allow Overflow:** If checked, the pool can temporarily create more objects than `Pool Size` if needed (generates a console warning). If unchecked, attempting to spawn from an empty pool will return `null`.
    *   **Auto Reparent:** If checked, spawned objects will be parented to the `Pool` GameObject in the hierarchy. This can be useful for organization.

### d. Using the Pool in Code

#### Option A: Direct Reference (Recommended)

The most efficient way to use a pool is by having a direct reference to the `Pool` component.

```csharp
using UnityEngine;
using LazyPooling; // Make sure to include the namespace

public class Weapon : MonoBehaviour
{
    public Pool bulletPool; // Assign this in the Inspector by dragging the Pool GameObject

    void Fire()
    {
        if (bulletPool == null)
        {
            Debug.LogError("Bullet Pool not assigned!");
            return;
        }

        // Spawn an object
        GameObject spawnedBulletObject = bulletPool.Spawn();

        if (spawnedBulletObject != null)
        {
            // Get a specific component and use it
            Bullet bulletScript = spawnedBulletObject.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(transform.forward * 10f); // Example method
            }
            // Position and orient the bullet
            spawnedBulletObject.transform.position = transform.position;
            spawnedBulletObject.transform.rotation = transform.rotation;
        }
        else
        {
            Debug.LogWarning("Bullet Pool is empty and overflow is not allowed or maxed out.");
        }
    }
}
```

#### Option B: `PoolManager` Lookup

You can also retrieve a pool by its ID using the static `PoolManager`. This is slightly less performant due to the dictionary lookup but useful for globally accessible pools.

```csharp
using UnityEngine;
using LazyPooling;

public class EnemySpawner : MonoBehaviour
{
    void SpawnEnemy()
    {
        Pool enemyPool = PoolManager.GetPool("Enemies");

        if (enemyPool != null)
        {
            GameObject spawnedEnemyObject = enemyPool.Spawn();
            if (spawnedEnemyObject != null)
            {
                spawnedEnemyObject.transform.position = transform.position;
                // Initialize enemy...
            }
        }
        else
        {
            Debug.LogError("Enemy Pool with ID 'Enemies' not found!");
        }
    }
}
```

## 2. Advanced Setup

### a. Event-Driven Initialization/Cleanup

The `PoolableObject` component provides events for managing state changes when objects are spawned or despawned. This is the recommended way to reset an object to its default state.

*   `OnSpawnEvent`: Invoked when the object is taken from the pool. Use this to reset its properties, activate components, etc.
*   `OnDespawnEvent`: Invoked when the object is returned to the pool. Use this to deactivate components, unsubscribe from events, etc.

**Example (`Enemy.cs` using `PoolableObject` events):**

```csharp
using UnityEngine;
using LazyPooling; // Assuming PoolableObject is in this namespace

public class Enemy : MonoBehaviour
{
    public float health = 100f;
    private PoolableObject poolable;

    void Awake()
    {
        poolable = GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Debug.LogError("Enemy script requires a PoolableObject component on the same GameObject.");
            return;
        }

        // Subscribe to the events
        poolable.OnSpawnEvent.AddListener(OnSpawned);
        poolable.OnDespawnEvent.AddListener(OnDespawned);
    }

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks if the object is destroyed outside the pool
        if (poolable != null)
        {
            poolable.OnSpawnEvent.RemoveListener(OnSpawned);
            poolable.OnDespawnEvent.RemoveListener(OnDespawned);
        }
    }

    public void OnSpawned()
    {
        // Reset state when spawned
        health = 100f;
        gameObject.SetActive(true);
        // Activate other components, start AI, etc.
        Debug.Log(gameObject.name + " has spawned!");
    }

    public void OnDespawned()
    {
        // Cleanup state when returned to pool
        gameObject.SetActive(false);
        // Stop AI, unsubscribe from game events, etc.
        Debug.Log(gameObject.name + " is returning to pool.");
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Instead of Destroy(gameObject), return it to the pool
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            // Fallback if not part of a pool (shouldn't happen with proper setup)
            Destroy(gameObject);
        }
    }
}
```

### b. Auto-Return Pattern (Self-Despawning Objects)

Objects can be responsible for returning themselves to the pool. This is common for projectiles that expire after a certain time or on impact.

**Example (`Projectile.cs`):**

```csharp
using UnityEngine;
using System.Collections;
using LazyPooling; // Assuming PoolableObject is in this namespace

public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;
    private PoolableObject poolable;

    void Awake()
    {
        poolable = GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Debug.LogError("Projectile script requires a PoolableObject component.");
        }
    }

    // Called by the spawner or OnSpawned event
    public void Initialize()
    {
        // If OnSpawned is used for this, StartCoroutine should be there
        StartCoroutine(AutoReturnRoutine());
    }
    
    void OnEnable()
    {
        // If Initialize is not explicitly called by spawner, 
        // OnEnable can be an alternative place to start the coroutine,
        // especially if OnSpawnEvent handles setting the object active.
        if (poolable != null && poolable.IsInPool == false) // Ensure it's actually spawned
        {
             StartCoroutine(AutoReturnRoutine());
        }
    }

    IEnumerator AutoReturnRoutine()
    {
        yield return new WaitForSeconds(lifeTime);
        ReturnToSender();
    }

    void OnCollisionEnter(Collision collision)
    {
        // Handle impact effects, damage, etc.
        // ...

        ReturnToSender();
    }

    void ReturnToSender()
    {
        // Make sure to stop the coroutine if returning due to collision before lifetime expires
        StopAllCoroutines(); 
        if (poolable != null)
        {
            poolable.ReturnToPool();
        }
        else
        {
            Destroy(gameObject); // Fallback
        }
    }
}
```

## 3. Best Practices

### a. Pool Sizing
*   Set `Pool Size` to accommodate the typical maximum number of active objects of that type you expect at any given time.
*   Enable `Allow Overflow` during development to get warnings in the console if pools are too small. This helps you identify bottlenecks.
*   In production, you might disable `Allow Overflow` for critical pools if you prefer to drop spawns rather than risk performance hits from on-demand instantiation, but generally, pre-warming appropriately is better.

### b. Performance Tips
*   **Prewarm:** Ensure your pools are adequately sized to pre-instantiate objects during loading screens or at game start to avoid runtime instantiation costs.
*   **Direct References:** Accessing `Pool` components directly (by assigning them in the Inspector) is faster than using `PoolManager.GetPool("PoolId")` repeatedly in performance-critical loops. Cache `PoolManager` lookups if used frequently.
*   **Event Subscription:** Remember to unsubscribe from `PoolableObject.OnSpawnEvent` and `PoolableObject.OnDespawnEvent` in `OnDestroy` if the listening component might be destroyed independently of the poolable object, to prevent errors or memory leaks.

### c. Memory Management
*   **Fixed-Size Nature:** Object pools are most effective when dealing with a known, relatively fixed number of objects.
*   **Temporary Overflow Objects:** If `Allow Overflow` is enabled, objects created beyond the initial `Pool Size` are temporary. When they are despawned, they are destroyed instead of being added to the pool if the pool is already at its configured capacity. This prevents the pool from growing indefinitely.

### d. Common Patterns

#### Timed Despawn
(Similar to the Projectile example's `AutoReturnRoutine`)
```csharp
// Inside your PoolableObject's script (e.g., TemporaryEffect.cs)
public class TemporaryEffect : MonoBehaviour
{
    public float duration = 2f;
    private PoolableObject poolable;

    void Awake() { poolable = GetComponent<PoolableObject>(); }

    void OnEnable() // Or use PoolableObject.OnSpawnEvent
    {
        if (poolable != null && !poolable.IsInPool) // Check if actually spawned
        {
            StartCoroutine(DespawnAfterTime(duration));
        }
    }

    IEnumerator DespawnAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        if (poolable != null) poolable.ReturnToPool();
    }
}
```

#### Distance-Based Despawn
```csharp
// Inside your PoolableObject's script (e.g., FarAwayParticle.cs)
public class FarAwayParticle : MonoBehaviour
{
    public Transform targetToFollow; // e.g., the player
    public float maxDistance = 50f;
    private PoolableObject poolable;

    void Awake() { poolable = GetComponent<PoolableObject>(); }
    
    void Update()
    {
        if (targetToFollow == null || poolable == null || poolable.IsInPool) return;

        if (Vector3.Distance(transform.position, targetToFollow.position) > maxDistance)
        {
            poolable.ReturnToPool();
        }
    }
}
```

## 4. Debugging

### a. Checking Pool Status
You can get statistics about all registered pools or a specific pool:
```csharp
using UnityEngine;
using LazyPooling;
using System.Text;

public class PoolDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Get stats for all pools
            string allStats = PoolManager.GetPoolStatistics();
            Debug.Log(allStats);

            // Get stats for a specific pool
            Pool specificPool = PoolManager.GetPool("Bullets");
            if (specificPool != null)
            {
                StringBuilder sb = new StringBuilder();
                specificPool.GetStatistics(sb);
                Debug.Log("Bullet Pool Stats:\n" + sb.ToString());
            }
        }
    }
}
```
The `PoolManager.GetPoolStatistics()` method returns a string detailing the ID, active count, inactive count, total size, and overflow status for each registered pool. The `Pool.GetStatistics(StringBuilder)` method appends similar information for a specific pool to a given StringBuilder.

### b. Monitoring Overflow Warnings
When a `Pool` with `Allow Overflow` enabled needs to spawn an object but has no inactive ones available, it will instantiate a new one and log a warning to the Unity console. Example:
`"[Pool] Pool 'Bullets' overflowed. Consider increasing its size. Current size: 10, Active: 10"`
Monitor these warnings during gameplay to identify pools that are too small.

### c. Common Issues and Solutions

*   **"Pool with ID 'X' not found!"**
    *   **Cause:** `PoolManager.GetPool("X")` was called, but no `Pool` component in an active scene has registered with that ID.
    *   **Solution:**
        1.  Ensure a GameObject with the `Pool` component exists in your scene.
        2.  Verify the `Pool Id` field in its Inspector matches "X" exactly (case-sensitive).
        3.  Make sure the `Pool` GameObject is active when the lookup occurs. Pools register themselves in `OnEnable`.

*   **"Overflow warnings" appearing frequently for a pool.**
    *   **Cause:** The `Pool Size` is too small for the number of concurrently active objects needed.
    *   **Solution:** Increase the `Pool Size` in the Inspector for the affected `Pool` component. Use the overflow warnings as a guide for how much to increase it.

*   **Objects not resetting properly when spawned.**
    *   **Cause:** State from a previous use is persisting (e.g., health, position, active status of child objects).
    *   **Solution:** Implement reset logic in a method subscribed to `PoolableObject.OnSpawnEvent`. This method should reset all relevant variables and component states to their defaults. Similarly, use `PoolableObject.OnDespawnEvent` for cleanup before an object is returned to the pool (e.g., stopping particle systems, deactivating certain behaviours).

*   **`NullReferenceException` when trying to use a spawned object.**
    *   **Cause A:** The pool had `Allow Overflow` disabled and was empty, so `pool.Spawn()` returned `null`.
    *   **Solution A:** Always check if the result of `Spawn()` is `null` before using it, especially if overflow is disallowed.
    *   **Cause B:** The component you are trying to access via `GetComponent<T>()` doesn't exist on the spawned prefab.
    *   **Solution B:** Ensure the prefab has the required component.

## 5. Architecture Benefits

*   **Zero Allocation (Typical Operation):** After initial prewarming, spawning and despawning objects from the pool usually involves no new memory allocations, significantly reducing garbage collection overhead.
*   **Self-Managing Pools:** `Pool` components automatically register with the central `PoolManager` on `OnEnable` and unregister on `OnDisable`, simplifying setup and teardown.
*   **Event-Driven Initialization/Cleanup:** `PoolableObject.OnSpawnEvent` and `PoolableObject.OnDespawnEvent` allow for clean separation of concerns, making it easy to reset and prepare objects.
*   **Type-Safe Spawning:** While `Spawn()` returns a `GameObject`, you typically use `GetComponent<T>()` immediately, and the system is designed with this pattern in mind.
*   **Overflow Handling:** Gracefully handles requests that exceed prewarmed capacity (if enabled), providing warnings to help developers adjust pool sizes.
*   **Centralized Management:** `PoolManager` provides a single point of access and overview for all pools.

## 6. Next Steps

*   **Explore the Examples:** Check out the scripts and prefabs in `Assets/LazyPooling/Examples/` to see practical implementations of pooling for projectiles, enemies, etc.
*   **Profile Your Game:** Use the Unity Profiler to monitor memory allocations and CPU usage. Observe the impact of pooling and adjust pool sizes for optimal performance in your specific game scenarios.
*   **Integrate and Customize:** Start integrating LazyPooling into your own game systems. Adapt the patterns and components as needed for your project's architecture.

---

Happy Pooling!
