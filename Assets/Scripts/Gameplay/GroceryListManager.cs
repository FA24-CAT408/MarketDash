using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Tracks the player's grocery order and mirrors its completion state onto the on-screen list UI.
/// </summary>
public class GroceryListManager : MonoBehaviour
{
    private const string OrderCompleteMessage = "PROCEED TO STAGING AREA";

    private static readonly Color CollectedItemColor = new Color(0f, 0.588f, 0.17f, 1f);

    [Tooltip("Parent container that all item text elements are instantiated under.")]
    [FormerlySerializedAs("listContainer")]
    [SerializeField] private Transform _listContainer;

    [Tooltip("Prefab holding the TMP_Text component used for a single list entry.")]
    [FormerlySerializedAs("itemTextPrefab")]
    [SerializeField] private GameObject _itemTextPrefab;

    [Tooltip("Items required to complete the current order.")]
    [FormerlySerializedAs("groceryList")]
    [SerializeField] private List<Item> _groceryList = new();

    private readonly Dictionary<Item, bool> _collectedItems = new();
    private readonly Dictionary<Item, TMP_Text> _itemUIElements = new();

    private bool _isOrderComplete;

    /// <summary>
    /// Raised once when every item in the current order has been collected.
    /// </summary>
    public event Action OrderCompleted;

    /// <summary>
    /// Items required to complete the current order.
    /// </summary>
    public IReadOnlyList<Item> GroceryList => _groceryList;

    private void OnEnable()
    {
        Item.OnItemCollected += HandleItemCollected;
    }

    private void OnDisable()
    {
        Item.OnItemCollected -= HandleItemCollected;
    }

    /// <summary>
    /// Rebuilds the list UI and resets the collection state for every item in the order.
    /// </summary>
    public void CreateAndShowList()
    {
        ClearUIElements();

        _collectedItems.Clear();
        _isOrderComplete = false;

        foreach (Item item in _groceryList)
        {
            if (item == null)
            {
                Debug.LogWarning($"{nameof(GroceryListManager)}: skipping a null entry in the grocery list.", this);
                continue;
            }

            _collectedItems[item] = false;
            CreateItemUIElement(item);
        }
    }

    /// <summary>
    /// Marks the given item as collected, updates its UI entry and completes the order when it was the last one.
    /// </summary>
    /// <param name="item">Item that has just been collected.</param>
    public void MarkItemCollected(Item item)
    {
        if (item == null)
        {
            return;
        }

        if (!_collectedItems.TryGetValue(item, out bool isCollected) || isCollected)
        {
            return;
        }

        _collectedItems[item] = true;
        UpdateItemUI(item);

        if (!IsOrderComplete())
        {
            return;
        }

        AddCompletionMessage();
        OrderCompleted?.Invoke();
    }

    /// <summary>
    /// Returns true the first time every item in the order has been collected.
    /// </summary>
    public bool IsOrderComplete()
    {
        if (_collectedItems.Count == 0 || _isOrderComplete)
        {
            return false;
        }

        foreach (bool isCollected in _collectedItems.Values)
        {
            if (!isCollected)
            {
                return false;
            }
        }

        _isOrderComplete = true;
        return true;
    }

    /// <summary>
    /// Clears the order, its collection state and the list UI.
    /// </summary>
    public void ResetList()
    {
        _groceryList.Clear();
        _collectedItems.Clear();

        ClearUIElements();
        _isOrderComplete = false;
    }

    private void HandleItemCollected(Item item)
    {
        MarkItemCollected(item);
    }

    private void CreateItemUIElement(Item item)
    {
        TMP_Text textComponent = InstantiateListEntry();
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = item.itemName;
        _itemUIElements[item] = textComponent;
    }

    private void AddCompletionMessage()
    {
        TMP_Text textComponent = InstantiateListEntry();
        if (textComponent == null)
        {
            return;
        }

        textComponent.text = OrderCompleteMessage;
        textComponent.fontStyle = FontStyles.Bold;
        textComponent.color = Color.red;
    }

    private TMP_Text InstantiateListEntry()
    {
        if (_itemTextPrefab == null || _listContainer == null)
        {
            Debug.LogError(
                $"{nameof(GroceryListManager)}: '{nameof(_itemTextPrefab)}' and '{nameof(_listContainer)}' must both be assigned.",
                this);
            return null;
        }

        GameObject entry = Instantiate(_itemTextPrefab, _listContainer);
        if (entry.TryGetComponent(out TMP_Text textComponent))
        {
            return textComponent;
        }

        Debug.LogError(
            $"{nameof(GroceryListManager)}: '{_itemTextPrefab.name}' is missing a {nameof(TMP_Text)} component.",
            this);
        Destroy(entry);
        return null;
    }

    private void UpdateItemUI(Item item)
    {
        if (_itemUIElements.TryGetValue(item, out TMP_Text textComponent) && textComponent != null)
        {
            textComponent.color = CollectedItemColor;
        }
    }

    private void ClearUIElements()
    {
        foreach (TMP_Text textComponent in _itemUIElements.Values)
        {
            if (textComponent != null)
            {
                Destroy(textComponent.gameObject);
            }
        }

        _itemUIElements.Clear();
    }
}
