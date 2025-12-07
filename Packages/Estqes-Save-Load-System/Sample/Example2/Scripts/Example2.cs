using Estqes.SaveLoadSystem;
using System;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Example2 : MonoBehaviour
{
    public Inventory Inventory { get; private set; }
    public Action OnInventoryUpdated { get; set; }

    [SerializeField] private ItemData[] items;
    private void Awake()
    {
        Inventory = new Inventory(20);
        for (int i = 0; i < items.Length; i++)
        {
            Inventory.Slots[i].item = items[i];
            Inventory.Slots[i].count = (int)(Random.value * items[i].maxStackSize);
        }

        SaveLoadManager.Instance.OnLoadEnd += InventoryUpdate;
    }

    private void InventoryUpdate()
    {
        Inventory = AllSaveableEntity.GetAll<Inventory>().ElementAt(0);
        OnInventoryUpdated?.Invoke();
    }

    private void OnDestroy()
    {
        SaveLoadManager.Instance.OnLoadEnd -= InventoryUpdate;
    }
}
