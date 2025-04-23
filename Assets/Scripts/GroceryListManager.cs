using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GroceryListManager : MonoBehaviour
{
    public Transform listContainer; // Parent container for all item texts
    public GameObject itemTextPrefab;

    public List<Item> groceryList = new();
    public Dictionary<Item, bool> collectedItems = new Dictionary<Item, bool>(); 
    
    private Dictionary<Item, TMP_Text> itemUIElements  = new(); // Track UI by item name
    
    private bool isOrderComplete = false;
    
    private void OnEnable()
    {
        // Subscribe to the item collected event
        Item.OnItemCollected += HandleItemCollected;
    }
    
    private void OnDisable()
    {
        // Unsubscribe when this object is disabled to prevent memory leaks
        Item.OnItemCollected -= HandleItemCollected;
    }

    // Start is called before the first frame update
    void Start()
    {
    }
    
    private void HandleItemCollected(Item item)
    {
        if (groceryList.Contains(item) && !collectedItems[item])
        {
            Debug.Log($"Item collected: {item.itemName}");
            
            // Mark the item as collected
            MarkItemCollected(item);
        }
    }
    
    public void CreateAndShowList()
    {
        ClearUIElements();

        Debug.Log("CREATING AND SHOWING LIST!");
        
        // Initialize collection status for all items
        collectedItems.Clear();
        
        foreach (Item item in groceryList)
        {
            collectedItems[item] = false;
            CreateItemUIElement(item);
        }
    }
    
    public void MarkItemCollected(Item item)
    {
        if (groceryList.Contains(item) && !collectedItems[item])
        {
            collectedItems[item] = true;
            UpdateItemUI(item);
            
            if (IsOrderComplete())
            {
                AddCompletionMessage();
                GameManager.Instance.ChangeState(GameManager.GameState.EndGame);
            }
        }
    }
    
    private void CreateItemUIElement(Item item)
    {
        GameObject itemTextObject = Instantiate(itemTextPrefab, listContainer);
        TMP_Text textComponent = itemTextObject.GetComponent<TMP_Text>();
        textComponent.text = item.itemName;
        
        itemUIElements[item] = textComponent;
    }
    
    public bool IsOrderComplete()
    {
        foreach (var collected in collectedItems.Values)
        {
            if (!collected) return false;
        }

        isOrderComplete = true;
        return true;
    }

    private void AddCompletionMessage()
    {
        GameObject completionTextObject = Instantiate(itemTextPrefab, listContainer);
        TMP_Text textComponent = completionTextObject.GetComponent<TMP_Text>();
        textComponent.text = "PROCEED TO STAGING AREA";
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = Color.red;

        Debug.Log("All items collected! Return to the entrance.");
    }

    private void UpdateItemUI(Item item)
    {
        if (itemUIElements.ContainsKey(item))
        {
            TMP_Text textComponent = itemUIElements[item];
            textComponent.color = new Color(0f, 0.588f, 0.17f, 1); // Dark green
        }
    }

    private void ClearUIElements()
    {
        // Clear only UI elements
        foreach (var textComponent in itemUIElements.Values)
        {
            if (textComponent != null)
            {
                Destroy(textComponent.gameObject);
            }
        }
        
        itemUIElements.Clear();
    }

    public void ResetList()
    {
        // Clear data structures
        groceryList.Clear();
        collectedItems.Clear();
        
        ClearUIElements();
        isOrderComplete = false;
    }
}
