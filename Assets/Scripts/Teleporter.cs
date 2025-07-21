#nullable enable

using UnityEngine;
using Managers;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager = null!;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerController? _))
        {
            roomManager.MovePlayerToRoom(0);
        }
    }
}
