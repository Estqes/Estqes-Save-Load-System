using Estqes.SaveLoadSystem;
using UnityEngine;

public class Example1 : MonoBehaviour
{
    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            new TestEntity()
            {
                Positon = Random.onUnitSphere * 10
            };
            new TestEntity(1)
            {
                Positon = Random.onUnitSphere * 10
            };
        }
        CreateObjects();
        SaveLoadManager.Instance.OnLoadEnd += CreateObjects;
    }
    void CreateObjects()
    {
        foreach (Transform item in transform)
        {
            Destroy(item.gameObject);
        }
        foreach (var item in AllSaveableEntity.GetAll<TestEntity>())
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = item.Positon;
            cube.transform.SetParent(transform);
            if (item.SomeClass != null)
            Debug.Log($"a {item.SomeClass.a}, b {item.SomeClass.b}, c {item.SomeClass.c}, d {item.SomeClass.d}");
        }
    }

    private void OnDestroy()
    {
        SaveLoadManager.Instance.OnLoadEnd -= CreateObjects;
    }
}
