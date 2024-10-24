using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using DG.Tweening;

public class GroceryListManager : MonoBehaviour
{
    public static GroceryListManager Instance { get; private set; }

    public List<Item> allAvailableItems;
    public List<Item> groceryList;
    public Transform listContainer; // Parent container for all item texts
    public GameObject itemTextPrefab;

    private Dictionary<string, TMP_Text> _itemUITexts = new Dictionary<string, TMP_Text>(); // Track UI by item name
    private Dictionary<string, int> _itemQuantities = new Dictionary<string, int>(); // Track total counts of each item

    private void Awake()
    {
        // Check if an instance already exists and destroy this one if it does
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        allAvailableItems = FindObjectsOfType<Item>().ToList();

        // foreach (Item item in groceryList)
        // {
        //     if (!itemCounts.ContainsKey(item.itemName))
        //     {
        //         itemCounts[item.itemName] = item.amount;
        //         TMP_Text itemText = InstantiateItemText(item);
        //         itemUITexts[item.itemName] = itemText;
        //     }
        //     else
        //     {
        //         itemCounts[item.itemName] += item.amount;
        //         UpdateItemText(item);
        //     }
        // }
    }

    public List<Item> GetNewOrder(int numOfUniqueItems = 5)
    {
        List<Item> newOrder = new List<Item>();

        Sequence sequence = DOTween.Sequence();

        for (int i = 0; i < numOfUniqueItems; i++)
        {
            // int randomIndex = Random.Range(0, allAvailableItems.Count);
            // newOrder.Add(allAvailableItems[randomIndex]);

            Item item = AddRandomItemToOrder();
            AddNewItemToUI(item);

            // Broadcast a new unity event to show the new item in the UI (maybe in a UI Manager?)

            // sequence.Join(AddNewItemToUI(item));
        }

        // sequence.Play();

        return newOrder;
    }

    void Update()
    {

    }

    public Item AddRandomItemToOrder()
    {
        int randomIndex = Random.Range(0, allAvailableItems.Count);
        groceryList.Add(allAvailableItems[randomIndex]);

        return allAvailableItems[randomIndex];
    }

    void AddNewItemToUI(Item item)
    {
        if (!_itemQuantities.ContainsKey(item.itemName))
        {
            _itemQuantities[item.itemName] = item.amount;
            TMP_Text itemText = InstantiateItemText(item);
            // itemText.gameObject.SetActive(false);
            _itemUITexts[item.itemName] = itemText;
        }
        else
        {
            _itemQuantities[item.itemName] += item.amount;
            UpdateItemText(item);
        }

        // // Animate the new item text
        // TMP_Text newItemText = _itemUITexts[item.itemName];
        // newItemText.gameObject.SetActive(true);
        // DOTweenTMPAnimator animator = new DOTweenTMPAnimator(newItemText);
        // Sequence sequence = DOTween.Sequence();

        // for (int i = 0; i < animator.textInfo.characterCount; ++i)
        // {
        //     if (!animator.textInfo.characterInfo[i].isVisible) continue;

        //     Vector3 currCharOffset = animator.GetCharOffset(i);
        //     // First, fade in and bounce the character
        //     sequence.Append(animator
        //         .DOOffsetChar(i, currCharOffset + new Vector3(0, 5, 0), 0.1f)
        //         .SetEase(Ease.OutBounce) // Bounce effect
        //         .From(new Vector3(0, -50, 0)) // Start from lower position for smooth entrance
        //         .SetDelay(i * 0.05f) // Delay each character's appearance
        //     );

        //     // Then bring the character back to its original position
        //     sequence.Append(animator
        //         .DOOffsetChar(i, currCharOffset, 0.1f)
        //         .SetEase(Ease.OutElastic)
        //     );
        // }

        // return sequence;
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
        if (_itemQuantities.ContainsKey(item.itemName) && _itemQuantities[item.itemName] > 0)
        {
            _itemQuantities[item.itemName]--;
            UpdateItemText(item);

            // If item count reaches 0, mark it as completed (turn the text green)
            if (_itemQuantities[item.itemName] == 0)
            {
                _itemUITexts[item.itemName].color = Color.green;
            }
        }
    }

    void UpdateItemText(Item item)
    {
        if (_itemUITexts.ContainsKey(item.itemName))
        {
            TMP_Text itemText = _itemUITexts[item.itemName];
            itemText.text = $"{item.itemName} x {_itemQuantities[item.itemName]}";
        }
    }
}
