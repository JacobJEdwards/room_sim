using UnityEngine;
using System.Collections.Generic;

public class Room : MonoBehaviour
{
    [Header("Room Settings")]
    [SerializeField]
    private Transform teleportDestination;

    public Transform TeleportDestination => teleportDestination;
    private List<(GameObject, Vector3)> _objectPositions = new();

    private void Start()
    {
        // Store initial positions of objects in the room
        foreach (Transform child in transform)
        {
            if (child == transform) continue;

            _objectPositions.Add((child.gameObject, child.position));
        }
    }

    public void ActivateRoom()
    {
        gameObject.SetActive(true);
    }

    public void DeactivateRoom()
    {
        gameObject.SetActive(false);
    }
}
