using UnityEngine;

/// <summary>
/// Null-safe VFX / SFX helpers. Audio goes through SoundManager (pool + cooldowns).
/// If a prefab or clip is not assigned — does nothing.
/// </summary>
public static class FxPlayer
{
    public static GameObject PlayVfx(GameObject prefab, Vector3 position, Transform parent = null, float autoDestroyAfter = 3f)
    {
        if (prefab == null)
            return null;

        var instance = Object.Instantiate(prefab, position, Quaternion.identity, parent);

        if (autoDestroyAfter > 0f)
        {
            float lifetime = autoDestroyAfter;
            var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
            if (particles != null && particles.Length > 0)
            {
                lifetime = 0f;
                foreach (var ps in particles)
                {
                    var main = ps.main;
                    float cycle = main.duration + main.startLifetime.constantMax;
                    if (cycle > lifetime)
                        lifetime = cycle;
                }

                lifetime = Mathf.Max(lifetime, 0.5f);
            }

            Object.Destroy(instance, lifetime);
        }

        return instance;
    }

    public static void PlaySfx(AudioClip clip, Vector3 position, float volume = 1f)
    {
        // Position kept for API compatibility; playback is 2D via SoundManager.
        if (clip == null)
            return;

        SoundManager.Play(clip, volume);
    }

    public static void PlaySfx2D(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        SoundManager.PlayUi(clip);
    }

    public static void Play(GameObject vfxPrefab, AudioClip sfx, Vector3 position, Transform parent = null)
    {
        PlayVfx(vfxPrefab, position, parent);
        PlaySfx(sfx, position);
    }
}
