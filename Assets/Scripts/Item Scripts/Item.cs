using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName;
    public int amount;

    private GroceryListManager groceryListManager;

    public void Start()
    {
        groceryListManager = FindObjectOfType<GroceryListManager>();
    }

    void Update()
    {
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
