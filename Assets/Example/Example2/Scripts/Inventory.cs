using Estqes.SaveLoadSystem;
using Newtonsoft.Json;
using NUnit.Framework.Interfaces;
using System;
using UnityEngine;

[EntityType("inventory")]
public class Inventory : ISaveableEntity
{
    [Save] public InventorySlot[] Slots { get; private set; }
    public Guid Id { get; set; }

    public event Action OnInventoryUpdated;

    public Inventory(int size)
    {
        Slots = new InventorySlot[size];
        for (int i = 0; i < size; i++)
        {
            Slots[i] = new InventorySlot();
        }

        AllSaveableEntity.Register(this);
    }

    public bool AddItem(ItemData item, int amount)
    {

        for (int i = 0; i < Slots.Length; i++)
        {
            if (!Slots[i].IsEmpty && Slots[i].item == item)
            {
                amount = Slots[i].Add(item, amount);
                if (amount <= 0)
                {
                    OnInventoryUpdated?.Invoke();
                    return true;
                }
            }
        }

        for (int i = 0; i < Slots.Length; i++)
        {
            if (Slots[i].IsEmpty)
            {
                amount = Slots[i].Add(item, amount);
                if (amount <= 0)
                {
                    OnInventoryUpdated?.Invoke();
                    return true;
                }
            }
        }

        OnInventoryUpdated?.Invoke();
        return amount <= 0;
    }

    public void MoveItem(int fromIndex, int toIndex)
    {
        var fromSlot = Slots[fromIndex];
        var toSlot = Slots[toIndex];

        if (fromSlot.IsEmpty) return;

        if (toSlot.IsEmpty)
        {
            toSlot.item = fromSlot.item;
            toSlot.count = fromSlot.count;
            fromSlot.Clear();
        }

        else if (toSlot.item == fromSlot.item)
        {
            int remaining = toSlot.Add(fromSlot.item, fromSlot.count);
            if (remaining == 0)
            {
                fromSlot.Clear();
            }
            else
            {
                fromSlot.count = remaining;
            }
        }

        else
        {
            var tempItem = toSlot.item;
            var tempCount = toSlot.count;

            toSlot.item = fromSlot.item;
            toSlot.count = fromSlot.count;

            fromSlot.item = tempItem;
            fromSlot.count = tempCount;
        }

        OnInventoryUpdated?.Invoke();
    }
}

[Serializable]
public class InventorySlot 
{
    [JsonProperty("Item")] public ItemData item;
    [JsonProperty("Count")] public int count;
    [JsonIgnore] public bool IsEmpty => item == null;

    public InventorySlot()
    {
        item = null;
        count = 0;
    }

    public void Clear()
    {
        item = null;
        count = 0;
    }

    public int Add(ItemData item, int amount)
    {
        if (IsEmpty)
        {
            this.item = item;
        }

        if (this.item != item) return amount;

        int spaceLeft = this.item.maxStackSize - count;

        if (spaceLeft >= amount)
        {
            count += amount;
            return 0; 
        }
        else
        {
            count = this.item.maxStackSize;
            return amount - spaceLeft;
        }
    }
}