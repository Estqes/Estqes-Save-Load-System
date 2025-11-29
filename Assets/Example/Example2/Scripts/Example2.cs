using UnityEngine;

public class Example2 : MonoBehaviour
{
    public Inventory Inventory { get; private set; }
    [SerializeField] private ItemData[] items;
    private void Awake()
    {
        Inventory = new Inventory(20);
        for (int i = 0; i < items.Length; i++)
        {
            Inventory.Slots[i].item = items[i];
            Inventory.Slots[i].count = (int)(Random.value * items[i].maxStackSize);
        }
    }
}
