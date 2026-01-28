using UnityEngine;
using System.Collections;

namespace HorizonMini.Build
{
    /// <summary>
    /// Handles visual effects when objects are placed, moved, or rotated
    /// - Particle effect on transform changes
    /// - Scale shake with noise
    /// </summary>
    public class ObjectPlacementEffect : MonoBehaviour
    {
        [Header("Particle Effect")]
        public GameObject particleEffectPrefab;
        public bool spawnParticlesOnPlace = true;
        public bool spawnParticlesOnMove = true;
        public bool spawnParticlesOnRotate = true;

        [Header("Scale Shake")]
        public bool enableScaleShake = true;
        [Range(0f, 0.2f)]
        public float shakeIntensity = 0.05f; // 5% scale variation
        [Range(0.1f, 1f)]
        public float shakeDuration = 0.3f;
        [Range(1f, 50f)]
        public float shakeFrequency = 20f; // Noise frequency

        [Header("Audio Feedback")]
        public AudioClip rotationSnapSound;
        public AudioClip moveSnapSound;
        public AudioClip placementCompleteSound;
        [Range(0f, 1f)]
        public float snapSoundVolume = 0.5f;

        private bool isShaking = false;
        private Vector3 originalScale;
        private AudioSource audioSource;

        private void Awake()
        {
            // Ensure AudioSource exists
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 1f; // 3D sound
                audioSource.maxDistance = 20f;
            }
        }

        public void PlayRotationSnapSound()
        {
            if (audioSource == null) return;

            if (rotationSnapSound != null)
            {
                audioSource.PlayOneShot(rotationSnapSound, snapSoundVolume);
            }
            else
            {
                // Generate procedural click sound
                StartCoroutine(PlayProceduralClick(800f)); // Higher pitch for rotation
            }
        }

        public void PlayMoveSnapSound()
        {
            if (audioSource == null) return;

            if (moveSnapSound != null)
            {
                audioSource.PlayOneShot(moveSnapSound, snapSoundVolume);
            }
            else
            {
                // Generate procedural click sound
                StartCoroutine(PlayProceduralClick(600f)); // Lower pitch for movement
            }
        }

        private System.Collections.IEnumerator PlayProceduralClick(float frequency)
        {
            // Simple procedural click sound
            float duration = 0.05f; // 50ms click
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            AudioClip clip = AudioClip.Create("ProceduralClick", sampleCount, 1, sampleRate, false);

            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Sine wave with quick decay envelope
                float envelope = Mathf.Exp(-t * 50f); // Fast decay
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
            }

            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, snapSoundVolume * 0.3f); // Lower volume for generated sound

            yield return new WaitForSeconds(duration);
            Destroy(clip);
        }

        public void PlayPlacementCompleteSound()
        {
            if (audioSource == null) return;

            if (placementCompleteSound != null)
            {
                // Use custom sound
                audioSource.PlayOneShot(placementCompleteSound, snapSoundVolume);
            }
            else
            {
                // Generate procedural chord sound
                StartCoroutine(GeneratePlacementCompleteSound());
            }
        }

        private System.Collections.IEnumerator GeneratePlacementCompleteSound()
        {
            // Play a pleasant "success" chord (3 notes)
            float duration = 0.3f;
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(duration * sampleRate);
            AudioClip clip = AudioClip.Create("PlacementComplete", sampleCount, 1, sampleRate, false);

            float[] samples = new float[sampleCount];

            // Create a chord: root (C), third (E), fifth (G)
            float[] frequencies = new float[] { 523.25f, 659.25f, 783.99f }; // C5, E5, G5

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                // Envelope: quick attack, slow decay
                float envelope = Mathf.Exp(-t * 3f) * (1f - Mathf.Exp(-t * 30f));

                // Mix three frequencies (chord)
                float sample = 0f;
                foreach (float freq in frequencies)
                {
                    sample += Mathf.Sin(2f * Mathf.PI * freq * t);
                }
                sample /= frequencies.Length; // Average

                samples[i] = sample * envelope * 0.5f;
            }

            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip, snapSoundVolume * 0.6f);

            yield return new WaitForSeconds(duration);
            Destroy(clip);
        }

        public void PlayPlacementEffect(Vector3 position)
        {
            if (spawnParticlesOnPlace)
            {
                SpawnParticles(position);
            }

            if (enableScaleShake)
            {
                StartCoroutine(ScaleShake());
            }

            // Play completion sound
            PlayPlacementCompleteSound();
        }

        public void PlayMoveEffect(Vector3 position)
        {
            if (spawnParticlesOnMove)
            {
                SpawnParticles(position);
            }

            if (enableScaleShake)
            {
                StartCoroutine(ScaleShake());
            }
        }

        public void PlayRotateEffect(Vector3 position)
        {
            if (spawnParticlesOnRotate)
            {
                SpawnParticles(position);
            }

            if (enableScaleShake)
            {
                StartCoroutine(ScaleShake());
            }
        }

        private void SpawnParticles(Vector3 position)
        {
            GameObject particles = null;

            if (particleEffectPrefab != null)
            {
                // Use provided prefab
                particles = Instantiate(particleEffectPrefab, position, Quaternion.identity);
            }
            else
            {
                // Create default particle system
                particles = new GameObject("PlacementParticles");
                particles.transform.position = position;

                ParticleSystem ps = particles.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 0.5f;
                main.startSpeed = 2f;
                main.startSize = 0.1f;
                main.startColor = new Color(1f, 0.8f, 0.3f, 1f); // Orange/yellow
                main.maxParticles = 20;
                main.duration = 0.3f;
                main.loop = false;

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                renderer.material.SetColor("_BaseColor", new Color(1f, 0.8f, 0.3f, 1f));
            }

            // Auto-destroy after particle system finishes
            if (particles != null)
            {
                Destroy(particles, 2f);
            }
        }

        private IEnumerator ScaleShake()
        {
            if (isShaking) yield break;

            isShaking = true;
            originalScale = transform.localScale;

            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / shakeDuration;

                // Use Perlin noise for smooth random shake
                float noiseX = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * 2f;
                float noiseY = (Mathf.PerlinNoise(Time.time * shakeFrequency, 100f) - 0.5f) * 2f;
                float noiseZ = (Mathf.PerlinNoise(Time.time * shakeFrequency, 200f) - 0.5f) * 2f;

                // Apply intensity and ease out
                float intensity = shakeIntensity * (1f - t);
                Vector3 shake = new Vector3(noiseX, noiseY, noiseZ) * intensity;

                // Element-wise multiplication
                Vector3 scaleOffset = new Vector3(
                    originalScale.x * shake.x,
                    originalScale.y * shake.y,
                    originalScale.z * shake.z
                );

                transform.localScale = originalScale + scaleOffset;

                yield return null;
            }

            // Restore original scale
            transform.localScale = originalScale;
            isShaking = false;
        }

        private void OnDestroy()
        {
            // Restore scale if destroyed during shake
            if (isShaking && originalScale != Vector3.zero)
            {
                transform.localScale = originalScale;
            }
        }
    }
}
