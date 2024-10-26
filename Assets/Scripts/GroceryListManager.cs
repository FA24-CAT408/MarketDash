using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;

public class GroceryListManager : MonoBehaviour
{
    [Header("Input")]
    public PlayerControls playerInput;

    public static GroceryListManager Instance { get; private set; }

    [Header("Items")]
    public List<Item> allAvailableItems;

    public Transform listContainer; // Parent container for all item texts
    public GameObject itemTextPrefab;

    [Header("Target Item")]
    public int targetItemIndex = 0;
    public Item targetItem;

    // Add input buffering to prevent too rapid cycling
    private float lastInputTime = 0f;
    private float inputCooldown = 0.05f; // Adjust this value to control cycling speed


    private List<Item> groceryList = new();
    private Dictionary<string, TMP_Text> _itemUITexts = new(); // Track UI by item name
    private Dictionary<string, int> _itemQuantities = new();  // Track total counts of each item

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

        playerInput = new PlayerControls();

        playerInput.Player.SwapTargets.performed += OnSwapItemInput;
        playerInput.Player.SwapTargets.canceled += OnSwapItemInput;
        playerInput.Player.SwapTargets.started += OnSwapItemInput;
    }

    // Start is called before the first frame update
    void Start()
    {
        // _itemUITexts = new Dictionary<string, TMP_Text>();
        // _itemQuantities = new Dictionary<string, int>();
        // groceryList = new List<Item>();

        foreach (Item item in allAvailableItems)
        {
            item.gameObject.SetActive(false);
        }

        UpdateTargetItem();
    }

    public List<Item> GetNewOrder(int numOfUniqueItems = 5)
    {
        List<Item> newOrder = new List<Item>();

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

        Debug.Log("New order created!");
        Debug.Log("Grocery List:");
        Debug.Log(string.Join(", ", groceryList));


        return newOrder;
    }

    void Update()
    {
        UpdateItemHighlighting();
    }

    private void UpdateItemHighlighting()
    {
        // Reset all items to normal style
        foreach (var item in _itemUITexts)
        {
            item.Value.fontStyle = FontStyles.Normal;
        }

        // Bold the current target item if it exists
        if (targetItem != null && _itemUITexts.ContainsKey(targetItem.itemName))
        {
            _itemUITexts[targetItem.itemName].fontStyle = FontStyles.Bold;
        }
    }

    private void UpdateTargetItem()
    {
        if (groceryList.Count > 0)
        {
            // Ensure targetItemIndex stays within bounds
            targetItemIndex = Mathf.Clamp(targetItemIndex, 0, groceryList.Count - 1);
            targetItem = groceryList[targetItemIndex];
        }
        else
        {
            targetItemIndex = 0;
            targetItem = null;
        }
    }

    void OnSwapItemInput(InputAction.CallbackContext ctx)
    {
        // Only process input if we have items and enough time has passed since last input
        if (groceryList.Count > 0 && Time.time >= lastInputTime + inputCooldown)
        {
            float axisValue = ctx.ReadValue<float>();

            // Only process if there's actual input
            if (Mathf.Abs(axisValue) > 0.1f)
            {
                // Positive value (Q key) cycles forward
                if (axisValue > 0)
                {
                    targetItemIndex++;
                    if (targetItemIndex >= groceryList.Count)
                    {
                        targetItemIndex = 0;
                    }
                }
                // Negative value (E key) cycles backward
                else if (axisValue < 0)
                {
                    targetItemIndex--;
                    if (targetItemIndex < 0)
                    {
                        targetItemIndex = groceryList.Count - 1;
                    }
                }

                // Update the target item
                UpdateTargetItem();

                // Update the input buffer time
                lastInputTime = Time.time;

                // Debug log to verify cycling
                Debug.Log($"Switched to item: {targetItem.itemName} at index: {targetItemIndex}");
            }
        }
    }

    public Item AddRandomItemToOrder()
    {
        int randomIndex = Random.Range(0, allAvailableItems.Count);
        groceryList.Add(allAvailableItems[randomIndex]);

        allAvailableItems[randomIndex].gameObject.SetActive(true);

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

    public void ResetLists()
    {
        groceryList.Clear();
        _itemUITexts.Clear();
        _itemQuantities.Clear();
    }

    void OnEnable()
    {
        playerInput.Player.Enable();
    }

    void OnDisable()
    {
        playerInput.Player.Disable();
    }
}
