using Estqes.SaveLoadSystem;
using Newtonsoft.Json;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Example2/Item")]
public class ItemData : ScriptableObject, IContentEntity
{
    [JsonProperty("Name")] public string name;
    [JsonIgnore] public Sprite icon;
    [JsonProperty("MasStackSize")] public int maxStackSize = 100;

    //This property have only get or set in constructor or like this
    public string Tag => name;
    public ItemData() { }
}