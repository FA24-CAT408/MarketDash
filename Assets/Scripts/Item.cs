using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    public string itemName;
    public int amount;
    public bool alreadyPickedUp = false;

    private GroceryListManager groceryListManager;

    public void Start()
    {
        groceryListManager = FindObjectOfType<GroceryListManager>();

        // Set the item color
        // GetComponent<Renderer>().material.color = itemColor;
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
        if (!alreadyPickedUp)
        {
            amount--;
            groceryListManager.RemoveItem(this);
        }
    }
}
