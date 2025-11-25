using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    public class AllSaveableEntity
    {
        public Dictionary<Guid, ISaveableEntity> Enities { get; private set; }
        public static AllSaveableEntity Instance { get; private set; }

        public AllSaveableEntity()
        {
            if (Instance != null) return;

            Enities = new Dictionary<Guid, ISaveableEntity>();
            Instance = this;
        }

        public static void Register(ISaveableEntity enity)
        {
            if (Instance.Enities.TryGetValue(enity.Id, out var item))
            {
                if (enity == item) return;
                else return;
            }

            Instance.Enities.Add(enity.Id, enity);
            Debug.Log($"Regiset Enity type {enity.GetType()}, with id {enity.Id}");
        }

        public static ISaveableEntity GetEnity(Guid id)
        {
            if (Instance.Enities.TryGetValue(id, out var enity)) return enity;
            Debug.LogError($"Enity done exists {id}");
            return null;
        }

        public static T GetEnity<T>(Guid id) where T : class, ISaveableEntity
        {
            if (Instance.Enities.TryGetValue(id, out var enity)) return enity as T;
            Debug.LogError($"Enity done exists {id}");
            return null;
        }

        public static IEnumerable<T> GetAll<T>() where T : class, ISaveableEntity
        {
            return Instance.Enities.Values.OfType<T>();

        }
        public static IEnumerable<ISaveableEntity> GetAll()
        {
            return Instance.Enities.Values;

        }
        public static void Clear()
        {
            Instance.Enities.Clear();
        }

        public static void RemoveEnity(Guid id)
        {
            Instance.Enities.Remove(id);
        }
    }
}
