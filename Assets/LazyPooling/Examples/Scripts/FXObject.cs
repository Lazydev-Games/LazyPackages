using UnityEngine;
using System.Collections;
using LazyPooling.Components; // For PoolableObject
using LazyPooling.Core; // For Pool (if used directly, though FXSpawner will use it)

namespace LazyPooling.Examples.Scripts
{
    /// <summary>
    /// Manages a pooled FX object that combines ParticleSystem and AudioSource.
    /// Automatically returns to the pool when its effects have finished playing.
    /// </summary>
    [RequireComponent(typeof(PoolableObject))]
    public class FXObject : MonoBehaviour
    {
        [Header("Effect Components")]
        [Tooltip("The main Particle System for this effect.")]
        public ParticleSystem mainParticleSystem;

        [Tooltip("The main Audio Source for this effect.")]
        public AudioSource mainAudioSource;

        private PoolableObject _poolable;
        private Coroutine _checkIfFinishedCoroutine;

        private void Awake()
        {
            _poolable = GetComponent<PoolableObject>();

            if (_poolable != null)
            {
                _poolable.OnSpawnEvent += HandleSpawn;
                _poolable.OnDespawnEvent += HandleDespawn;
            }
            else
            {
                Debug.LogError("FXObject requires a PoolableObject component on the same GameObject.", this);
            }
        }

        private void OnDestroy()
        {
            if (_poolable != null)
            {
                _poolable.OnSpawnEvent -= HandleSpawn;
                _poolable.OnDespawnEvent -= HandleDespawn;
            }
        }

        /// <summary>
        /// Called when the FX object is spawned from the pool.
        /// Plays the assigned particle and audio effects.
        /// </summary>
        private void HandleSpawn()
        {
            if (mainParticleSystem != null)
            {
                mainParticleSystem.Play(true); // Play with children
            }

            if (mainAudioSource != null && mainAudioSource.clip != null)
            {
                mainAudioSource.Play();
            }

            // Start monitoring if effects are finished
            if (_checkIfFinishedCoroutine != null)
            {
                StopCoroutine(_checkIfFinishedCoroutine);
            }
            _checkIfFinishedCoroutine = StartCoroutine(CheckIfFinishedCoroutine());
        }

        /// <summary>
        /// Called when the FX object is returned to the pool.
        /// Stops any playing particle and audio effects.
        /// </summary>
        private void HandleDespawn()
        {
            if (mainParticleSystem != null && mainParticleSystem.isPlaying)
            {
                // Stop emitting new particles and clear existing ones immediately
                mainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (mainAudioSource != null && mainAudioSource.isPlaying)
            {
                mainAudioSource.Stop();
            }

            // Ensure the monitoring coroutine is stopped
            if (_checkIfFinishedCoroutine != null)
            {
                StopCoroutine(_checkIfFinishedCoroutine);
                _checkIfFinishedCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine that checks if the particle system and audio source have finished playing.
        /// If so, it returns the object to the pool.
        /// </summary>
        private IEnumerator CheckIfFinishedCoroutine()
        {
            // Wait one frame to ensure Play() has taken effect if called on the same frame
            yield return null;

            while (true)
            {
                bool particlesPlaying = mainParticleSystem != null && mainParticleSystem.IsAlive(true);
                bool audioPlaying = mainAudioSource != null && mainAudioSource.isPlaying;

                if (!particlesPlaying && !audioPlaying)
                {
                    // Both effects have finished
                    if (_poolable != null)
                    {
                        _poolable.ReturnToPool();
                    }
                    _checkIfFinishedCoroutine = null; // Clear the coroutine reference
                    yield break; // Exit the coroutine
                }
                yield return null; // Wait for the next frame before checking again
            }
        }
    }
}
