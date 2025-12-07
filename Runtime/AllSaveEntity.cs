using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    public class AllSaveableEntity
    {
        private Dictionary<Guid, ISaveableEntity> _enities { get; set; }

        public static AllSaveableEntity Instance { get; private set; }

        public AllSaveableEntity()
        {
            if (Instance != null) return;

            _enities = new Dictionary<Guid, ISaveableEntity>();

            Instance = this;
        }

        public static void Register(ISaveableEntity enity)
        {
            if (enity.Id == default) enity.Id = GetNewId();
            if (Instance._enities.TryGetValue(enity.Id, out var item))
            {
                if (enity == item) return;
                else return;
            }

            Instance._enities.Add(enity.Id, enity);
            Debug.Log($"Regiset Enity type {enity.GetType()}, with id {enity.Id}");
        }

        public static ISaveableEntity GetEnity(Guid id)
        {
            if (Instance._enities.TryGetValue(id, out var enity)) return enity;
            Debug.LogError($"Enity done exists {id}");
            return null;
        }

        public static T GetEnity<T>(Guid id) where T : class, ISaveableEntity
        {
            if (Instance._enities.TryGetValue(id, out var enity)) return enity as T;
            Debug.LogError($"Enity done exists {id}");
            return null;
        }

        public static IEnumerable<T> GetAll<T>() where T : class, ISaveableEntity
        {
            return Instance._enities.Values.OfType<T>();

        }
        public static IEnumerable<ISaveableEntity> GetAll()
        {
            return Instance._enities.Values;

        }
        public static void Clear()
        {
            Instance._enities.Clear();
        }

        public static void RemoveEnity(Guid id)
        {
            Instance._enities.Remove(id);
        }

        public static Guid GetNewId()
        {
            return Guid.NewGuid();
        }
    }
}
