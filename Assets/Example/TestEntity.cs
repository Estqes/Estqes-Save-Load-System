using Estqes.SaveLoadSystem;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

[EntityType("test_entity")]
public class TestEntity : MonoBehaviour, ISaveableEntity
{
    public Guid Id { get; private set; }

    private MeshRenderer _meshRenderer;
    [Save] public Transform Transform => this.transform;

    private void Start()
    {
        Id = Guid.NewGuid();
        _meshRenderer = GetComponent<MeshRenderer>();
        AllSaveableEntity.Register(this);
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.Q))
        {

        }
    }
}