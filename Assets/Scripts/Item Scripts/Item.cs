using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    public int amount;
    public bool isSelected;

    public float iconSelectedScaleMultiplier = 2f;
    public SpriteRenderer minimapSprite;
    public SpriteRenderer isSelectedMinimapSprite;

    private GroceryListManager groceryListManager;
    private Vector3 _orignalIconScale;

    public void Start()
    {
        groceryListManager = FindObjectOfType<GroceryListManager>();
        
        _orignalIconScale = minimapSprite.transform.localScale;
    }

    void Update()
    {
        if (isSelected)
        {
            isSelectedMinimapSprite.enabled = true;
            minimapSprite.transform.localScale = Vector3.Lerp(minimapSprite.transform.localScale, _orignalIconScale * iconSelectedScaleMultiplier, 0.1f);
        } else
        {
            isSelectedMinimapSprite.enabled = false;
            minimapSprite.transform.localScale = Vector3.Lerp(minimapSprite.transform.localScale, _orignalIconScale, 0.1f);
        }

        if (amount <= 0)
        {
            // Destroy(gameObject);
            gameObject.SetActive(false);
        }
    }

    public void Interact()
    {
        if (amount > 0)
        {
            amount--;
            groceryListManager.RemoveItem(this);
        }
    }
}
