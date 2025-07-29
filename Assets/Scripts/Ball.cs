using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Ball : MonoBehaviour
{
    private void Awake()
    {
        gameObject.tag = "Basketball";
    }
}