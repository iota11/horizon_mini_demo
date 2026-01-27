using UnityEngine;

namespace HorizonMini.Build
{
    /// <summary>
    /// Global settings for object placement effects
    /// Attach this to BuildController to configure effects in Inspector
    /// </summary>
    [CreateAssetMenu(fileName = "PlacementEffectSettings", menuName = "HorizonMini/Placement Effect Settings")]
    public class PlacementEffectSettings : ScriptableObject
    {
        [Header("Particle Effect")]
        [Tooltip("Custom particle effect prefab (optional). If null, uses default particles.")]
        public GameObject particleEffectPrefab;

        [Tooltip("Spawn particles when placing object from UI")]
        public bool spawnParticlesOnPlace = true;

        [Tooltip("Spawn particles when moving object")]
        public bool spawnParticlesOnMove = true;

        [Tooltip("Spawn particles when rotating object")]
        public bool spawnParticlesOnRotate = true;

        [Header("Scale Shake")]
        [Tooltip("Enable scale shake effect")]
        public bool enableScaleShake = true;

        [Tooltip("Shake intensity (0.05 = 5% scale variation)")]
        [Range(0f, 0.2f)]
        public float shakeIntensity = 0.05f;

        [Tooltip("Shake duration in seconds")]
        [Range(0.1f, 1f)]
        public float shakeDuration = 0.3f;

        [Tooltip("Noise frequency (higher = faster shake)")]
        [Range(1f, 50f)]
        public float shakeFrequency = 20f;

        [Header("Audio Feedback")]
        [Tooltip("Audio clip for rotation snap (like a tick sound)")]
        public AudioClip rotationSnapSound;

        [Tooltip("Audio clip for movement snap (like a tick sound)")]
        public AudioClip moveSnapSound;

        [Tooltip("Audio clip for picking up object from UI")]
        public AudioClip pickupSound;

        [Tooltip("Audio clip for placement complete")]
        public AudioClip placementCompleteSound;

        [Tooltip("Volume for snap sounds")]
        [Range(0f, 1f)]
        public float snapSoundVolume = 0.5f;

        /// <summary>
        /// Apply these settings to an ObjectPlacementEffect component
        /// </summary>
        public void ApplyToEffect(ObjectPlacementEffect effect)
        {
            if (effect == null) return;

            effect.particleEffectPrefab = particleEffectPrefab;
            effect.spawnParticlesOnPlace = spawnParticlesOnPlace;
            effect.spawnParticlesOnMove = spawnParticlesOnMove;
            effect.spawnParticlesOnRotate = spawnParticlesOnRotate;
            effect.enableScaleShake = enableScaleShake;
            effect.shakeIntensity = shakeIntensity;
            effect.shakeDuration = shakeDuration;
            effect.shakeFrequency = shakeFrequency;
            effect.rotationSnapSound = rotationSnapSound;
            effect.moveSnapSound = moveSnapSound;
            effect.placementCompleteSound = placementCompleteSound;
            effect.snapSoundVolume = snapSoundVolume;
        }

        /// <summary>
        /// Apply pickup sound to PlacementSystem
        /// </summary>
        public void ApplyPickupSoundToPlacementSystem(PlacementSystem placementSystem)
        {
            if (placementSystem == null) return;
            placementSystem.pickupSound = pickupSound;
        }
    }
}
