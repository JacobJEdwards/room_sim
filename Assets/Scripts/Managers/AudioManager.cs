#nullable enable

using UnityEngine;

namespace Managers
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; } = null!;

        private AudioSource? _musicSource;

        [SerializeField][Range(0, 100)] private float musicVolume = 100;
        [SerializeField][Range(0, 100)] private float soundVolume = 100;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 100);
            soundVolume = PlayerPrefs.GetFloat("SFXVolume", 100);
        }

        public void PlayMusic(AudioSource audioSource, AudioClip clip)
        {
            if (audioSource.isPlaying) return;

            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.volume = musicVolume / 100;
            audioSource.Play();
        }

        public void PlaySound(AudioSource audioSource, AudioClip clip)
        {
            if (audioSource.isPlaying) return;

            audioSource.volume = soundVolume / 100;
            audioSource.clip = clip;
            audioSource.gameObject.SetActive(true);
            audioSource.Play();
        }

        public void StopSound(AudioSource audioSource)
        {
            audioSource.Stop();
        }

        public void PauseSound(AudioSource audioSource)
        {
            audioSource.Pause();
        }

        public void UnPauseSound(AudioSource audioSource)
        {
            audioSource.UnPause();
        }
    }
}