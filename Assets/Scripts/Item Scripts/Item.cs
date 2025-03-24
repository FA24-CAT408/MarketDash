using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    public static event Action<Item> OnItemCollected;
    
    private AudioSource _audioSource;
    
    public void Start()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Interact();
        }
    }

    public void Interact()
    {
        OnItemCollected?.Invoke(this);
        gameObject.SetActive(false);
        _audioSource.Play();
    }
}