using Estqes.SaveLoadSystem;
using Newtonsoft.Json;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

[EntityType("test_entity")]
public class TestEntity : ISaveableEntity
{
    public Guid Id { get; set; }
    [Save]public int a;
    [Save]public int b;
    [Save]public int c;
    [Save]public Vector3 Positon { get; set; }

    [Save]public SomeClass SomeClass { get; set; }

    [Loader(nameof(SomeClass))]
    public TestEntity(SomeClass some)
    {
        Debug.Log("Constructor 1");
        a = some.a; 
        b = some.b; 
        c = some.c;
        SomeClass = some;
    }

    [Loader(nameof(a), nameof(b), nameof(c))]
    public TestEntity(int value1, int value2, int value3)
    {
        Debug.Log("Constructor 2");
        a = value1;
        b = value2;
        c = value3;
    }

    public TestEntity()
    {
        a = (int)(Random.value * 100);
        b = (int)(Random.value * 100);
        c = (int)(Random.value * 100);

        Id = Guid.NewGuid();
        AllSaveableEntity.Register(this);

        SomeClass = new SomeClass()
        {
            a = a,
            b = b,
            c = c,
            d = a + b + c,
        };
    }

    public TestEntity(int a)
    {
        a = (int)(Random.value * 100);
        b = (int)(Random.value * 100);
        c = (int)(Random.value * 100);

        Id = Guid.NewGuid();
        AllSaveableEntity.Register(this);

    }
}

// If the relationship between classes is not important, you can do it like this
public class SomeClass 
{
    // [JsonProperty] is optional. But if you change the variable name, saving will break
    [JsonProperty("A")] public int a;
    [JsonProperty("B")] public int b;
    [JsonProperty("C")] public int c;

    // If you need to ignore some field, use [JsonIgnore]
    [JsonIgnore] public int d;
}
