using Estqes.SaveLoadSystem;
using UnityEngine;

public class ExampleScene : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKey(KeyCode.F5))
        {
            SaveLoadManager.Instance.Save();
        }
        if (Input.GetKey(KeyCode.F6))
        {
            SaveLoadManager.Instance.Load();
        }
    }
}
