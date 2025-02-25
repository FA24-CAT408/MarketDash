using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    private AudioSource _audioSource;

    public void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
    }

    public void Interact()
    {
        _audioSource.Play();
        GroceryListManager.Instance.MarkItemCollected(this);
        gameObject.SetActive(false);
    }
}