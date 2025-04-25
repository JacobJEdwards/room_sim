using UnityEngine;

public class Bin : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out MoveableObject _))
        {
            Destroy(other.gameObject);
        }
    }
}
