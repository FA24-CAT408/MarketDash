using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GroceryListManager : MonoBehaviour
{
    public List<Item> groceryList;
    public Transform listContainer; // Parent container for all item texts
    public GameObject itemTextPrefab;

    private Dictionary<string, TMP_Text> itemUITexts = new Dictionary<string, TMP_Text>(); // Track UI by item name
    private Dictionary<string, int> itemCounts = new Dictionary<string, int>(); // Track total counts of each item

    // Start is called before the first frame update
    void Start()
    {
        foreach (Item item in groceryList)
        {
            if (!itemCounts.ContainsKey(item.itemName))
            {
                itemCounts[item.itemName] = item.amount;
                TMP_Text itemText = InstantiateItemText(item);
                itemUITexts[item.itemName] = itemText;
            }
            else
            {
                itemCounts[item.itemName] += item.amount;
                UpdateItemText(item);
            }
        }
    }

    TMP_Text InstantiateItemText(Item item)
    {
        GameObject itemUIText = Instantiate(itemTextPrefab, listContainer);
        TMP_Text textComponent = itemUIText.GetComponent<TMP_Text>();
        textComponent.text = $"{item.itemName} x {item.amount}";
        return textComponent;
    }

    public void RemoveItem(Item item)
    {
        if (itemCounts.ContainsKey(item.itemName) && itemCounts[item.itemName] > 0)
        {
            itemCounts[item.itemName]--;
            UpdateItemText(item);

            // If item count reaches 0, mark it as completed (turn the text green)
            if (itemCounts[item.itemName] == 0)
            {
                item.alreadyPickedUp = true;
                itemUITexts[item.itemName].color = Color.green;
            }
        }
    }

    void UpdateItemText(Item item)
    {
        if (itemUITexts.ContainsKey(item.itemName))
        {
            TMP_Text itemText = itemUITexts[item.itemName];
            itemText.text = $"{item.itemName} x {itemCounts[item.itemName]}";
        }
    }
}
