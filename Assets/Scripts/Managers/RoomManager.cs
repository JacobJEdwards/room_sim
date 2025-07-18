#nullable enable

using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace Managers
{
    public class RoomManager : MonoBehaviour
    {
        private UIManager _uiManager = null!;
        private GameManager _gameManager = null!;
        [Header("Player Settings")]
        [SerializeField]
        [Tooltip("The player GameObject that will be moved.")]
        private GameObject player = null!;
        private CharacterController? _playerController;

        [Header("Room Settings")]
        [SerializeField]
        [Tooltip("The list of room objects.")]
        private List<Room> roomObjects = new();

        [Header("UI References")]
        [SerializeField]
        [Tooltip("The TextMeshPro UI element to display the room name on Desktop.")]
        private TMP_Text roomNameTextDesktop = null!;

        [SerializeField]
        [Tooltip("The TextMeshPro UI element to display the room name on Mobile.")]
        private TMP_Text roomNameTextMobile = null!;


        private int _currentRoomIndex = -1;

        public Room CurrentRoom => roomObjects[_currentRoomIndex];

        private void Start()
        {
            _uiManager = UIManager.Instance;
            _gameManager = GameManager.Instance;

            if (player)
            {
                if (player.TryGetComponent<CharacterController>(out var controller))
                {
                    _playerController = controller;
                }
            }
            else
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player && player.TryGetComponent<CharacterController>(out var controller))
                {
                    _playerController = controller;
                }
                else
                {
                   Debug.LogError("Player object is not assigned and could not be found by tag 'Player'.", this);
                   enabled = false;
                   return;
                }
            }

            // Initially disable both text objects. The correct one will be enabled in MovePlayerToRoom.
            if(roomNameTextDesktop) roomNameTextDesktop.gameObject.SetActive(false);
            if(roomNameTextMobile) roomNameTextMobile.gameObject.SetActive(false);
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

            // Activate/Deactivate rooms
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
                // Move the player
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

                if (_uiManager) _uiManager.CloseAllPanels();

                if (_gameManager.IsMobilePlatform)
                {
                    if (roomNameTextMobile)
                    {
                        roomNameTextMobile.text = destination.RoomName;
                        roomNameTextMobile.gameObject.SetActive(true);
                        if(roomNameTextDesktop) roomNameTextDesktop.gameObject.SetActive(false); // Ensure other is off
                    }
                }
                else
                {
                    if (roomNameTextDesktop)
                    {
                        roomNameTextDesktop.text = destination.RoomName;
                        roomNameTextDesktop.gameObject.SetActive(true);
                        if(roomNameTextMobile) roomNameTextMobile.gameObject.SetActive(false); // Ensure other is off
                    }
                }

                if (GameManager.Instance) GameManager.Instance.SetMode(GameManager.ControlMode.Camera);
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