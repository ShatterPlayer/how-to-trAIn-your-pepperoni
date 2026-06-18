using UnityEngine;
using PizzaGame.Managers;

namespace PizzaGame.Infrastructure
{
    public class AudioManager : MonoBehaviour
    {
        // Singleton, ¿ebyœ mog³a ³atwo odtworzyæ dŸwiêk z ka¿dego innego skryptu
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip backgroundMusic;
        [SerializeField] private AudioClip yaySound;
        [SerializeField] private AudioClip failSound;

        [Header("Trim Settings")]
        [SerializeField] private float maxSfxDuration = 0.4f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Odpalamy muzykê w tle na starcie sceny
            if (musicSource != null && backgroundMusic != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.loop = true;
                musicSource.playOnAwake = true;
                musicSource.Play();
            }
        }

        public void PlayYay()
        {
            if (sfxSource != null && yaySound != null)
            {
                CancelInvoke(nameof(StopSFX));

                sfxSource.clip = yaySound;
                sfxSource.pitch = 3.0f;
                sfxSource.Play();

                Invoke(nameof(StopSFX), maxSfxDuration);
            }
        }

        public void PlayFail()
        {
            if (sfxSource != null && failSound != null)
            {
                CancelInvoke(nameof(StopSFX));

                sfxSource.clip = failSound;
                sfxSource.pitch = 1.0f;
                sfxSource.Play();

                Invoke(nameof(StopSFX), maxSfxDuration);
            }
        }

        private void StopSFX()
        {
            if (sfxSource != null)
            {
                sfxSource.Stop();
            }
        }
    }
}