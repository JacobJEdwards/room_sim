using System;
using UnityEngine;
using System.Collections.Generic;
using Interfaces;

public class Room : MonoBehaviour
{
    [Header("Room Settings")]
    [SerializeField]
    private Transform teleportDestination;

    public Transform TeleportDestination => teleportDestination;
    private readonly List<IInteractable> _objectPositions = new();
    private readonly List<GameObject> _placedObjects = new();

    private void Start()
    {
        foreach (var child in GetComponentsInChildren<Transform>())
        {
            if (child.TryGetComponent<IInteractable>(out var interactable))
            {
                _objectPositions.Add(interactable);
            }
        }
    }

    public void ResetRoom()
    {
        foreach (var obj in _objectPositions)
        {
            obj.ResetObject();
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

    public void AddPlacedObject(GameObject obj)
    {
        _placedObjects.Add(obj);
    }

    public void ClearPlacedObjects()
    {
        foreach (var obj in _placedObjects)
        {
            Destroy(obj);
        }
        _placedObjects.Clear();
    }
}
