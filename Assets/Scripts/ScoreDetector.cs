// Scripts/ScoreDetector.cs
using Managers;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ScoreDetector : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip scoreSound;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Basketball"))
        {
            BasketballManager.Instance.OnScore();

            if(audioSource && scoreSound)
            {
                AudioManager.Instance.PlaySound(audioSource, scoreSound);
            }
        }
    }
}