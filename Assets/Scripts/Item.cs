using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour, IInteractable
{
    public string itemName;
    public int amount;
    public Color itemColor;
    public bool alreadyPickedUp = false;

    private GroceryListManager groceryListManager;

    public void Start()
    {
        groceryListManager = FindObjectOfType<GroceryListManager>();

        // Set the item color
        GetComponent<MeshRenderer>().material.color = itemColor;

        //turn off emission
        GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
    }

    public void Interact()
    {
        if (!alreadyPickedUp)
        {
            groceryListManager.RemoveItem(this);
        }
    }
}
