using UnityEngine;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Example2 inventoryHolder;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform slotsContainer;
    [SerializeField] private Canvas mainCanvas;

    private List<InventorySlotUI> _uiSlots = new List<InventorySlotUI>();

    private void Start()
    {
        inventoryHolder.Inventory.OnInventoryUpdated += RefreshUI;

        InitializeSlots();

        RefreshUI();
    }

    private void InitializeSlots()
    {
        var slots = inventoryHolder.Inventory.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            GameObject obj = Instantiate(slotPrefab, slotsContainer);
            var slotUI = obj.GetComponent<InventorySlotUI>();
            slotUI.Init(i, inventoryHolder.Inventory, mainCanvas);
            _uiSlots.Add(slotUI);
        }
    }

    private void RefreshUI()
    {
        var slots = inventoryHolder.Inventory.Slots;
        for (int i = 0; i < slots.Length; i++)
        {
            _uiSlots[i].UpdateSlot(slots[i]);
        }
    }

    private void OnDestroy()
    {
        if (inventoryHolder != null && inventoryHolder.Inventory != null)
            inventoryHolder.Inventory.OnInventoryUpdated -= RefreshUI;
    }
}