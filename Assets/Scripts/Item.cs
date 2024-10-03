using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
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

        // Rotate the item
        transform.Rotate(new Vector3(0, 30, 0) * Time.deltaTime);

        if (amount <= 0)
        {
            Destroy(gameObject);
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
