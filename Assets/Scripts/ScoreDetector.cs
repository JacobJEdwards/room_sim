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
        // Check if the object that entered the trigger is a basketball
        if (other.CompareTag("Basketball"))
        {
            // Tell the manager we scored!
            BasketballManager.Instance.OnScore();

            // Play a satisfying sound
            if(audioSource && scoreSound)
            {
                AudioManager.Instance.PlaySound(audioSource, scoreSound);
            }
        }
    }
}