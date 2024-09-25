using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class GroceryListManager : MonoBehaviour
{
    public List<Item> groceryList;
    public List<GameObject> itemUITexts;

    [Header("UI")]
    public TMP_Text groceryListText;
    public GameObject itemTextPrefab;

    // Start is called before the first frame update
    void Start()
    {
        groceryListText.text = "Items:\n";

        foreach (Item item in groceryList)
        {
            // Instantiate the item text
            InstantiateItemText(item);
        }
    }

    void InstantiateItemText(Item item)
    {
        GameObject itemUIText = Instantiate(itemTextPrefab, new Vector3(groceryListText.transform.position.x + 50, groceryListText.transform.position.y - 30 * groceryList.Count, groceryListText.transform.position.z), Quaternion.identity);
        itemUIText.transform.SetParent(groceryListText.transform.parent);
        itemUIText.GetComponent<TMP_Text>().text = item.itemName + " x " + item.amount;
        itemUITexts.Add(itemUIText);
    }

    public void RemoveItem(Item item)
    {
        // Subtract 1 from the amount of the item, until it reaches 0 then turn that item text green

        if (item.amount > 0)
        {
            item.amount--;
            itemUITexts.Find(x => x.GetComponent<TMP_Text>().text.Contains(item.itemName)).GetComponent<TMP_Text>().text = item.itemName + " x" + item.amount;
        }

        if (item.amount == 0)
        {
            item.alreadyPickedUp = true;
            item.GetComponent<Renderer>().material.color = Color.green;
            itemUITexts.Find(x => x.GetComponent<TMP_Text>().text.Contains(item.itemName)).GetComponent<TMP_Text>().color = Color.green;
        }
    }
}
