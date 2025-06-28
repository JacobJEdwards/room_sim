#nullable enable

using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Managers
{
    public class RoomManager : MonoBehaviour
    {
        
        [FormerlySerializedAs("GameManager")] public GameManager gameManager = null!;
        private UIManager _uiManager = null!;
        [Header("Player Settings")] [SerializeField] [Tooltip("The player GameObject that will be moved.")]
        private GameObject? player;

        [Header("Room Settings")]
        [SerializeField]
        [Tooltip("A list of Transforms representing the teleport destination for each room.")]
        private List<Transform> roomDestinations = new();
        
        private void Start()
        {
            _uiManager = UIManager.Instance;
            
            if (player) return;
            player = GameObject.FindGameObjectWithTag("Player");
            if (player) return;
            Debug.LogError("Player object is not assigned and could not be found by tag 'Player'.", this);
            enabled = false;
            gameManager = GameManager.Instance;
        }
        
        public void MovePlayerToRoom(int roomIndex)
        {
            if (!player)
            {
                Debug.LogError("Cannot move player, the player object is not assigned!", this);
                return;
            }

            if (roomIndex >= 0 && roomIndex < roomDestinations.Count)
            {
                var destination = roomDestinations[roomIndex];
                if (destination)
                {
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

                    _uiManager.ToggleRoomPanel();

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
}