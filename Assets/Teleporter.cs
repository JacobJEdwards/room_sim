#nullable enable

using UnityEngine;
using Managers;

public class Teleporter : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager = null!;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out PlayerController? player))
        {
            roomManager.MovePlayerToRoom(0);
        }
    }
}
