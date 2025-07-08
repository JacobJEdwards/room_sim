using UnityEngine;
using System.Collections.Generic;

public class Book : MonoBehaviour
{
    [SerializeField] private List<GameObject> bookPrefabs;

    void Start()
    {
        if (bookPrefabs == null || bookPrefabs.Count == 0)
        {
            Debug.LogError("No book prefabs assigned to the Book script.");
            return;
        }

        var randomIndex = Random.Range(0, bookPrefabs.Count);
        var selectedBook = Instantiate(bookPrefabs[randomIndex], transform.position, Quaternion.identity);
        selectedBook.transform.SetParent(transform);

        // move colliders and rigidbodie and moveable object to this

        var colliders = selectedBook.GetComponentsInChildren<Collider>();
        foreach (var collider in colliders)
        {
            collider.transform.SetParent(transform);
        }
        var rigidbodies = selectedBook.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            rb.transform.SetParent(transform);
        }
        var moveableObjects = selectedBook.GetComponentsInChildren<MoveableObject>();
        foreach (var moveableObject in moveableObjects)
        {
            moveableObject.transform.SetParent(transform);
        }
    }
}
