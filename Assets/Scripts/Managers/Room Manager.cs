#nullable enable

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class RoomManager : MonoBehaviour
{
    [Header("Player Settings")]
    [SerializeField]
    [Tooltip("The player GameObject that will be moved.")]
    private GameObject? player;

    [Header("Room Settings")]
    [SerializeField]
    [Tooltip("A list of Transforms representing the teleport destination for each room.")]
    private List<Transform> roomDestinations = new List<Transform>();

    [Header("UI Settings")]
    [SerializeField]
    [Tooltip("The UI panel that contains the buttons to select a room.")]
    private GameObject? roomSelectionPanel;


    private void Start()
    {
        if (roomSelectionPanel != null)
        {
            roomSelectionPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("Room Selection Panel is not assigned in the RoomManager.", this);
            enabled = false;
        }

        if (player == null)
        {
            // Try to find the player by tag if not assigned
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogError("Player object is not assigned and could not be found by tag 'Player'.", this);
                enabled = false;
            }
        }
    }

    private void Update()
    {
        // Toggle the room selection panel when 'R' is pressed.
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ToggleRoomPanel();
        }
    }

    /// <summary>
    /// Opens or closes the room selection panel and manages the cursor state.
    /// </summary>
    private void ToggleRoomPanel()
    {
        if (roomSelectionPanel == null) return;

        bool isPanelActive = !roomSelectionPanel.activeSelf;
        roomSelectionPanel.SetActive(isPanelActive);

        if (isPanelActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Moves the player to the selected room's destination. Called by UI buttons.
    /// </summary>
    /// <param name="roomIndex">The index of the room destination in the list.</param>
    public void MovePlayerToRoom(int roomIndex)
    {
        if (player == null)
        {
            Debug.LogError("Cannot move player, the player object is not assigned!", this);
            return;
        }

        if (roomIndex >= 0 && roomIndex < roomDestinations.Count)
        {
            Transform destination = roomDestinations[roomIndex];
            if (destination != null)
            {
                // If the player has a CharacterController, we must disable it to teleport them.
                if (player.TryGetComponent<CharacterController>(out var controller))
                {
                    controller.enabled = false;
                    player.transform.position = destination.position;
                    controller.enabled = true;
                }
                else
                {
                    player.transform.position = destination.position;
                }

                Debug.Log($"Player moved to room '{destination.name}' at {destination.position}");

                // Hide panel and lock cursor to return to gameplay
                if (roomSelectionPanel != null)
                {
                    roomSelectionPanel.SetActive(false);
                }
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Debug.LogWarning($"Room destination at index {roomIndex} is not assigned.", this);
            }
        }
        else
        {
            Debug.LogError($"Invalid room index: {roomIndex}. List size is {roomDestinations.Count}.", this);
        }
    }
}