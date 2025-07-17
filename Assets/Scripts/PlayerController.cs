using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    private float _mouseSensitivity = 50f;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        _mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 50f);

        playerMovement.SetMouseSensitivity(_mouseSensitivity);
    }

    public void SetMouseSensitivity(float sensitivity)
    {
        _mouseSensitivity = sensitivity;
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        playerMovement.SetMouseSensitivity(sensitivity);
    }
}
