using System.Collections.Generic;
using Interfaces;
using UnityEngine;

public class DuvetSwitcher : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject[] duvet;
    [SerializeField] private Texture[] duvetTextures;

    private readonly List<Renderer> _duvetRenderer = new ();

    private void Awake()
    {
        foreach (var duvetObject in duvet)
        {
            if (duvetObject.TryGetComponent<Renderer>(out var r))
            {
                _duvetRenderer.Add(r);
            }
        }
    }

    private void ChangeDuvetTexture(int textureIndex)
    {
        if (textureIndex < 0 || textureIndex >= duvetTextures.Length)
        {
            Debug.LogError("Invalid texture index");
            return;
        }

        foreach (var r in _duvetRenderer)
        {
            if (r && r.material)
            {
                r.material.mainTexture = duvetTextures[textureIndex];
            }
            else
            {
                Debug.LogWarning("Renderer or material is null");
            }
        }
    }

    public void OnInteract(GameObject interactor)
    {
        var nextTextureIndex = (System.Array.IndexOf(duvetTextures, _duvetRenderer[0].material.mainTexture) + 1) % duvetTextures.Length;
        ChangeDuvetTexture(nextTextureIndex);
    }

    public bool CanInteract(GameObject interactor)
    {
        return true;
    }

    public string GetInteractionPromptDesktop(GameObject interactor)
    {
        return "Press E to change duvet";
    }

    public string GetInteractionPromptMobile(GameObject interactor)
    {
        return "Tap to change duvet";
    }
}