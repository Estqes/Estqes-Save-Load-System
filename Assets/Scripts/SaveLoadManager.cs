using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.VisualScripting.YamlDotNet.Serialization;
using UnityEditor;
using UnityEngine;

namespace Estqes.SaveLoadSystem
{
    public class SaveLoadManager
    {
        public const string RegistryPath = "Assets/Resources/SaveTypeRegistry.asset";
        public static SaveLoadManager Instance { get; private set; }
        public string SavePath => Path.Combine(FilePath, FileName);
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public JsonSerializerSettings SerializerSettings { get; set; }

        private JsonSerializer _serializer;
        private SaveTypeRegistry _registry;
        private static readonly Dictionary<Type, MemberInfo[]> _membersCache = new();

        public virtual void Init()
        {
            if (Instance != null) return;

            Instance = this;

            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
            };

            _serializer = JsonSerializer.Create(SerializerSettings);

            new AllSaveableEntity();

            _registry = LoadTypesRegistry();
        }

        public string Serialize()
        {
            var enites = AllSaveableEntity.GetAll();
            var saveData = new SaveData();
            foreach (var item in enites)
            {
                SaveEntity(item, saveData);
            }

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            Debug.Log(json);
            return json;

        }

        public void Save()
        {
            var json = Serialize();
            File.WriteAllText(SavePath, json);
        }

        public void SaveEntity(ISaveableEntity entity, SaveData saveData)
        {
            var type = entity.GetType();
            var id = _registry.GetId(entity.GetType());
            var entityData = new EntityData()
            {
                Data = new(),
                Id = entity.Id,
                Type = id,
            };

            if(!_membersCache.TryGetValue(type, out var info))
            {
                info = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.IsDefined(typeof(SaveAttribute), false))
                    .ToArray();
                _membersCache.Add(type, info);
            }

            foreach (var member in info)
            {
                object memberValue = GetMemberValue(member, entity);
                string memberName = member.Name;
                JToken token;

                token = JToken.FromObject(memberValue, _serializer);

                entityData.Data.Add(memberName, token);
            }
            saveData.Entities.Add(entityData);
        }

        public void Load()
        {
            string json = File.ReadAllText(SavePath);
            var saveData = JsonConvert.DeserializeObject<SaveData>(json, SerializerSettings);
            AllSaveableEntity.Clear();

            List<EntityData> sortedEntities;
            try
            {
                sortedEntities = GetTopologicallySortedEntities(saveData);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"Failed to determine load order: {ex.Message}");
                return;
            }

            foreach (var item in sortedEntities)
            {
                var type = _registry.GetType(item.Type);
                if(type.IsAssignableFrom(typeof(MonoBehaviour)))
                {
                    Debug.Log("add");
                }
                else 
                { 

                }
            }
        }
        #region Topological Sort Logic

        private List<EntityData> GetTopologicallySortedEntities(SaveData saveData)
        {
            var entitiesById = saveData.Entities.ToDictionary(e => e.Id);

            var dependencies = new Dictionary<Guid, List<Guid>>();
            var dependents = new Dictionary<Guid, List<Guid>>();
            var inDegree = new Dictionary<Guid, int>();

            // build dependecy graph
            foreach (var entityData in saveData.Entities)
            {
                var entityId = entityData.Id;
                inDegree[entityId] = 0;
                dependencies[entityId] = new List<Guid>();
                dependents[entityId] = new List<Guid>();
            }

            foreach (var entityData in saveData.Entities)
            {
                var entityId = entityData.Id;
                var entityType = _registry.GetType(entityData.Type);
                Debug.Log(entityType);

                if (entityType.IsSubclassOf(typeof(MonoBehaviour)))
                {

                }
                else
                {
                    var constructor = GetSaveConstructor(entityType);
                    var constructorParams = constructor.GetParameters();

                    // find all dependecy in constructor or method IGameEnity in constructor or method
                    foreach (var param in constructor.GetParameters())
                    {
                        if (typeof(ISaveableEntity).IsAssignableFrom(param.ParameterType))
                        {
                            var paramName = GetConstructorParamSourceName(param, constructor);
                            if (entityData.Data.TryGetValue(paramName, out var jToken))
                            {
                                var dependencyId = jToken.ToObject<Guid>();
                                if (entitiesById.ContainsKey(dependencyId))
                                {
                                    //build egde entityId -> dependencyId
                                    dependencies[entityId].Add(dependencyId);
                                    dependents[dependencyId].Add(entityId);
                                    inDegree[entityId]++;
                                }
                            }
                        }
                    }
                }      
            }

            //Kahn's algorithm
            var sortedList = new List<EntityData>();
            var queue = new Queue<Guid>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                sortedList.Add(entitiesById[currentId]);

                // "Удаляем" ребра, идущие от текущей вершины
                foreach (var dependentId in dependents[currentId])
                {
                    inDegree[dependentId]--;
                    if (inDegree[dependentId] == 0)
                    {
                        queue.Enqueue(dependentId);
                    }
                }
            }

            // Checking for cyclic dependencies
            if (sortedList.Count != saveData.Entities.Count)
            {
                // Find nodes in cylic
                var cycleNodes = inDegree.Where(kvp => kvp.Value > 0)
                                         .Select(kvp => Type.GetType(entitiesById[kvp.Key].Type).Name);
                throw new InvalidOperationException($"Cyclic dependency detected in save data. Involved types might be: {string.Join(", ", cycleNodes)}");
            }

            return sortedList;
        }

        private static ConstructorInfo GetSaveConstructor(Type entityType)
        {
            throw new NotImplementedException();
        }

        private static string GetConstructorParamSourceName(ParameterInfo param, ConstructorInfo constructor)
        {
            var attribute = constructor.GetCustomAttribute<LoadMethodAttribute>();
            if (attribute.ParamSourceNames != null && attribute.ParamSourceNames.Length > 0)
            {
                int paramIndex = Array.IndexOf(constructor.GetParameters(), param);
                return attribute.ParamSourceNames[paramIndex];
            }

            return "_" + param.Name;
        }

        #endregion
        private static SaveTypeRegistry LoadTypesRegistry()
        {
            var reg = AssetDatabase.LoadAssetAtPath<SaveTypeRegistry>(RegistryPath);
            if (reg == null)
            {
                throw new Exception($"You may not have SaveTypeRegistry in the folder {RegistryPath} . Try creating it manually: Estqes.SaveLoadSystem -> Create SaveTypeRegistry");
            }
            return reg;
        }

        private static object GetMemberValue(MemberInfo member, object source)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).GetValue(source),
                MemberTypes.Property => ((PropertyInfo)member).GetValue(source),
                _ => null
            };
        }
    }
}

