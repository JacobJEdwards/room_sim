using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    private float _mouseSensitivity = 100f;

    private void Start()
    {
        _mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 100f);

        playerMovement.SetMouseSensitivity(_mouseSensitivity);
    }
}
