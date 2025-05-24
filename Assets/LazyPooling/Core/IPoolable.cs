namespace LazyPooling.Core
{
    /// <summary>
    /// Core interface that defines the contract for all poolable objects.
    /// Implement this interface to receive spawn and despawn callbacks.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called when the object is spawned from the pool.
        /// Use this to initialize or reset the object's state.
        /// </summary>
        void OnSpawn();
        
        /// <summary>
        /// Called when the object is returned to the pool.
        /// Use this to clean up resources or reset the state.
        /// </summary>
        void OnDespawn();
    }
}
