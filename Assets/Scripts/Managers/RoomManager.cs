#nullable enable

using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Managers
{
    public class RoomManager : MonoBehaviour
    {
        
        private UIManager _uiManager = null!;
        [Header("Player Settings")] [SerializeField] [Tooltip("The player GameObject that will be moved.")]
        private GameObject player = null!;
        private CharacterController? _playerController;

        [Header("Room Settings")]
        [SerializeField] [Tooltip("The room object.")]
        private List<Room> roomObjects = new();

        [SerializeField]
        private TMP_Text roomNameText = null!;

        private int _currentRoomIndex = -1;

        public Room CurrentRoom => roomObjects[_currentRoomIndex];

        private void Start()
        {
            _uiManager = UIManager.Instance;


            if (player)
            {
                if (player.TryGetComponent<CharacterController>(out var controller))
                {
                    _playerController = controller;
                }

                return;
            }
            player = GameObject.FindGameObjectWithTag("Player");

            if (player)
            {
                if (player.TryGetComponent<CharacterController>(out var controller))
                {
                    _playerController = controller;
                }
            }
            Debug.LogError("Player object is not assigned and could not be found by tag 'Player'.", this);
            enabled = false;
        }

        public void DisableAllRooms()
        {
            foreach (var room in roomObjects)
            {
                room.DeactivateRoom();
            }
        }
        
        public void MovePlayerToRoom(int roomIndex)
        {
            if (roomIndex < 0 || roomIndex >= roomObjects.Count)
            {
                Debug.LogError($"Invalid room index: {roomIndex}. List size is {roomObjects.Count}.", this);
                return;
            }

            if (roomIndex != _currentRoomIndex)
            {
                roomObjects[roomIndex].ActivateRoom();
                if (_currentRoomIndex >= 0 && _currentRoomIndex < roomObjects.Count)
                    roomObjects[_currentRoomIndex].DeactivateRoom();
                _currentRoomIndex = roomIndex;
            }

            var destination = roomObjects[roomIndex];
            if (destination)
            {
                if (_playerController)
                {
                    _playerController.enabled = false;
                    player.transform.position = destination.TeleportDestination.position;
                    _playerController.enabled = true;
                }
                else
                {
                    player.transform.position = destination.TeleportDestination.position;
                }

                _uiManager.ToggleRoomPanel();
                roomNameText.text = destination.RoomName;

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Debug.LogWarning($"Room destination at index {roomIndex} is not assigned.", this);
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

        public void ClearPlacedObjects()
        {
            CurrentRoom.ClearPlacedObjects();
        }
    }
}