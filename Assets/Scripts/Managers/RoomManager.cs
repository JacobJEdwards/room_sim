#nullable enable

using UnityEngine;
using System.Collections.Generic;

namespace Managers
{
    public class RoomManager : MonoBehaviour
    {
        
        private UIManager _uiManager = null!;
        [Header("Player Settings")] [SerializeField] [Tooltip("The player GameObject that will be moved.")]
        private GameObject? player;

        [Header("Room Settings")]
        [SerializeField]
        [Tooltip("A list of Transforms representing the teleport destination for each room.")]
        private List<Transform> roomDestinations = new();

        [SerializeField]
        [Tooltip("A list of Transforms representing each room.")]
        private List<GameObject> rooms = new();

        [SerializeField] [Tooltip("The room object.")]
        private List<Room> roomObjects = new();

        private int _currentRoomIndex = -1;

        public Room CurrentRoom => roomObjects[_currentRoomIndex];

        private void Start()
        {
            _uiManager = UIManager.Instance;
            
            if (player) return;
            player = GameObject.FindGameObjectWithTag("Player");
            if (player) return;
            Debug.LogError("Player object is not assigned and could not be found by tag 'Player'.", this);
            enabled = false;
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
                if (roomIndex != _currentRoomIndex)
                {
                    rooms[roomIndex].SetActive(true);
                    if (_currentRoomIndex >= 0 && _currentRoomIndex < rooms.Count)
                        rooms[_currentRoomIndex].SetActive(false);
                    _currentRoomIndex = roomIndex;
                }

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

        public void ResetCurrentRoom()
        {
            if (_currentRoomIndex >= 0 && _currentRoomIndex < roomObjects.Count)
            {
                roomObjects[_currentRoomIndex].ResetRoom();
            }
            else
            {
                Debug.LogError("Current room index is out of bounds.", this);
            }
        }
    }
}