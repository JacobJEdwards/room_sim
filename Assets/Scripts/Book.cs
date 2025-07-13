using UnityEngine;
using System.Collections.Generic;

public class Book : MonoBehaviour
{
    [SerializeField] private List<GameObject> bookPrefabs;

    private void Start()
    {
        if (bookPrefabs == null || bookPrefabs.Count == 0)
        {
            Debug.LogError("No book prefabs assigned to the Book script.");
            return;
        }

        var randomIndex = Random.Range(0, bookPrefabs.Count);
        var selectedBook = Instantiate(bookPrefabs[randomIndex], transform.position, Quaternion.identity);
        selectedBook.transform.SetParent(transform);
    }
}
