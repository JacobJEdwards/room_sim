using System;
using UnityEngine;
using UnityEngine.UI;
using Application = UnityEngine.Application;

namespace Managers
{
    public class SettingsManager : MonoBehaviour
    {
        private GameManager _gameManager;
        private AudioManager _audioManager;

        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider sensitivitySlider;

        private void Start()
        {
            _gameManager = GameManager.Instance;
            _audioManager = AudioManager.Instance;

            if (Application.isMobilePlatform)
            {
                sensitivitySlider.maxValue = 200f;
            }

            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            sensitivitySlider.value = PlayerPrefs.GetFloat("MouseSensitivity", Application.isMobilePlatform ? 100f : 50f);
            SetSensitivity(sensitivitySlider.value);

            volumeSlider.onValueChanged.AddListener(SetVolume);
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 100f);
            SetVolume(volumeSlider.value);
        }

        private void SetVolume(float volume)
        {
            _audioManager.SetSoundVolume(volume);
            _audioManager.SetMusicVolume(volume);
        }

        private void SetSensitivity(float sensitivity)
        {
            _gameManager.SetMouseSensitivity(sensitivity);
        }
    }
}