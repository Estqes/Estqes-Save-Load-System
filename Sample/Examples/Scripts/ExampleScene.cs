using Estqes.SaveLoadSystem;
using UnityEngine;

public class ExampleScene : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Load();
        }
    }

    public void Save()
    {
        SaveLoadManager.Instance.Save();
    }

    public void Load()
    {
        SaveLoadManager.Instance.Load();
    }
}
