using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Item : MonoBehaviour
{
    public string itemName;
    public GameObject prefab;
    public bool glow;
    public bool isCollectable;
    public static event Action<Item> OnItemCollected;
    
    private AudioSource _audioSource;
    private VisualEffect _visualEffect;
    private bool _isInitialized = false;
    
    public void Start()
    {
        Initialize();
    }
    
    void OnEnable()
    {
        // Handle case where object is re-enabled
        if (_isInitialized && _visualEffect != null)
        {
            UpdateVfxState();
        }
    }

    private void Initialize()
    {
        _audioSource = GetComponent<AudioSource>();
        _visualEffect = GetComponentInChildren<VisualEffect>();
        _isInitialized = true;
        UpdateVfxState();
    }
    
    private void UpdateVfxState()
    {
        if (_visualEffect != null)
        {
            if (glow)
                _visualEffect.Play();
            else
                _visualEffect.Stop();
        }
    }

    void Update()
    {
        // Initialize in Update if not done in Start (for dynamically spawned objects)
        if (!_isInitialized)
        {
            Initialize();
            return;
        }
        
        // Only update VFX if the state changed
        if (_visualEffect != null && ((glow && !_visualEffect.isActiveAndEnabled) || 
                                     (!glow && _visualEffect.isActiveAndEnabled)))
        {
            UpdateVfxState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isCollectable)
        {
            Interact();
        }
    }

    public void Interact()
    {
        // Stop VFX before deactivating
        if (_visualEffect != null)
        {
            _visualEffect.Stop();
        }
        
        OnItemCollected?.Invoke(this);
        gameObject.SetActive(false);
        // _audioSource.Play();
    }
    
    private void OnDisable()
    {
        // Make sure VFX is stopped when object is disabled
        if (_visualEffect != null)
        {
            _visualEffect.Stop();
        }
    }
}
