using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    public int amount;
    public bool isSelected;

    public SpriteRenderer isSelectedMinimapSprite;

    private GroceryListManager groceryListManager;

    public void Start()
    {
        groceryListManager = FindObjectOfType<GroceryListManager>();
    }

    void Update()
    {
        isSelectedMinimapSprite.enabled = isSelected;

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
