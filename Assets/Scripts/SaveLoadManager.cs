using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
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
        private static readonly Dictionary<Type, MemberInfo[]> _saveCache = new();
        private static readonly Dictionary<Type, Loader[]> _loadersCache = new();
        public Action OnLoadStart { get; set; }
        public Action OnLoadEnd { get; set; }
        public Action OnSaveStart { get; set; }
        public Action OnSaveEnd { get; set; }

        public virtual void Init()
        {
            if (Instance != null) return;

            Instance = this;


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
            OnSaveStart?.Invoke();
            var json = Serialize();
            File.WriteAllText(SavePath, json);
            OnSaveEnd?.Invoke();
        }

        public void SaveEntity(ISaveableEntity entity, SaveData saveData)
        {
            var type = entity.GetType();
            var id = _registry.GetIdEntity(entity.GetType());
            var entityData = new EntityData()
            {
                Data = new(),
                id = entity.Id,
                type = id,
            };

            var saveMembers = GetSaveMembers(type);

            foreach (var member in saveMembers)
            {
                object memberValue = GetMemberValue(member, entity);
                string memberName = member.Name;
                JToken token;
                
                if(memberValue == null)
                {
                    token = JValue.CreateNull();
                }
                else if(memberValue is IEnumerable collection && memberValue is not string)
                {
                    token = JToken.FromObject(collection, _serializer);
                }
                else
                {
                    token = JToken.FromObject(memberValue, _serializer);
                }


                entityData.Data.Add(memberName, token);
            }
            saveData.Entities.Add(entityData);
        }

        public void Load()
        {
            OnLoadStart?.Invoke();
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

            foreach (var entityData in sortedEntities)
            {
                var type = _registry.GetTypeEntity(entityData.type);
                
                var loader = FindLoader(entityData);
                if(loader == null)
                {
                    Debug.LogWarning($"For object {entityData.id}, type:{type} dont find loader");
                    continue;
                }

                var constructorAttribute = loader.loaderAttribute;
                var constructor = loader.constructorInfo;
                var constructorParams = loader.parameterInfo;

                if (constructorAttribute.ParamSourceNames.Length > 0 && constructorAttribute.ParamSourceNames.Length != constructorParams.Length)
                    throw new InvalidOperationException($"In type '{type.FullName}', mismatch between [Loader] names and parameters count.");

                var args = new object[constructorParams.Length];

                for (int i = 0; i < constructorParams.Length; i++)
                {
                    args[i] = ResolveConstructorParameter(constructorParams[i], i, entityData, loader.loaderAttribute);
                }

                var newEntity = (ISaveableEntity)constructor.Invoke(args);
                newEntity.Id = entityData.id;
                AllSaveableEntity.Register(newEntity);

                FillProperties(entityData, newEntity, type, constructorParams);
            }

            OnLoadEnd?.Invoke();
        }

        private object ResolveConstructorParameter(ParameterInfo param, int paramIndex, EntityData entityData, LoaderAttribute attribute)
        {
            string sourceName = (attribute.ParamSourceNames?.Length > 0)
                ? attribute.ParamSourceNames[paramIndex]
                : param.Name;

            if (entityData.Data.TryGetValue(sourceName, out var jToken) ||
                entityData.Data.TryGetValue("_" + sourceName, out jToken))
            {
                return DeserializeValue(jToken, param.ParameterType);
            }

            if (sourceName.Equals(nameof(ISaveableEntity.Id), StringComparison.OrdinalIgnoreCase) && param.ParameterType == typeof(Guid))
                return entityData.id;

            throw new KeyNotFoundException($"Could not find saved value for constructor parameter '{param.Name}' (source: '{sourceName}') in type '{param.Member.DeclaringType.Name}'.");
        }

        private void FillProperties(EntityData data, object entity, Type entityType, ParameterInfo[] constructorParams)
        {
            var members = GetSaveMembers(entityType);

            foreach (var member in members)
            {
                if (constructorParams.Any(x => x.Name == member.Name)) continue;

                var memberType = GetMemberType(member);
                var valueToSet = DeserializeValue(data.Data[member.Name], memberType);

                if (member is FieldInfo field) field.SetValue(entity, valueToSet);
                if (member is PropertyInfo property && property.CanWrite) property.SetValue(entity, valueToSet);
            }

        }
        private object DeserializeValue(JToken jToken, Type targetType)
        {
            if (jToken == null || jToken.Type == JTokenType.Null) return null;
            return jToken.ToObject(targetType, _serializer);
        }
        private Loader FindLoader(EntityData entity)
        {
            var type = _registry.GetTypeEntity(entity.type);
            var loaders = GetLoaders(type);

            foreach (var loader in loaders)
            {
                if (loader.loaderAttribute.ParamSourceNames.All(x =>
                {
                    return !(entity.Data[x].Type == JTokenType.Null || entity.Data[x].Type == JTokenType.Undefined);
                    
                })) return loader;
            }

            return null;
        }

        #region Topological Sort Logic

        private List<EntityData> GetTopologicallySortedEntities(SaveData saveData)
        {
            var entitiesById = saveData.Entities.ToDictionary(e => e.id);

            var dependencies = new Dictionary<Guid, List<Guid>>();
            var dependents = new Dictionary<Guid, List<Guid>>();
            var inDegree = new Dictionary<Guid, int>();

            // build dependecy graph
            foreach (var entityData in saveData.Entities)
            {
                var entityId = entityData.id;
                inDegree[entityId] = 0;
                dependencies[entityId] = new List<Guid>();
                dependents[entityId] = new List<Guid>();
            }

            foreach (var entityData in saveData.Entities)
            {
                var entityId = entityData.id;
                var entityType = _registry.GetTypeEntity(entityData.type);
                Debug.Log(entityType);

                var constructor = FindLoader(entityData);
                var constructorParams = constructor.parameterInfo;

                // find all dependecy in constructor or method ISaveableEntity in constructor or method
                foreach (var param in constructor.parameterInfo)
                {
                    if (typeof(ISaveableEntity).IsAssignableFrom(param.ParameterType))
                    {
                        var paramName = GetConstructorParamSourceName(param, constructor.constructorInfo);
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

            //Kahn's algorithm
            var sortedList = new List<EntityData>();
            var queue = new Queue<Guid>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));

            while (queue.Count > 0)
            {
                var currentId = queue.Dequeue();
                sortedList.Add(entitiesById[currentId]);


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
                                         .Select(kvp => Type.GetType(entitiesById[kvp.Key].type).Name);
                throw new InvalidOperationException($"Cyclic dependency detected in save data. Involved types might be: {string.Join(", ", cycleNodes)}");
            }

            return sortedList;
        }

        private static string GetConstructorParamSourceName(ParameterInfo param, ConstructorInfo constructor)
        {
            var attribute = constructor.GetCustomAttribute<LoaderAttribute>();
            if (attribute.ParamSourceNames != null && attribute.ParamSourceNames.Length > 0)
            {
                int paramIndex = Array.IndexOf(constructor.GetParameters(), param);
                return attribute.ParamSourceNames[paramIndex];
            }

            return "_" + param.Name;
        }

        #endregion
        private SaveTypeRegistry LoadTypesRegistry()
        {
            var reg = AssetDatabase.LoadAssetAtPath<SaveTypeRegistry>(RegistryPath);
            if (reg == null)
            {
                throw new Exception($"You may not have SaveTypeRegistry in the folder {RegistryPath} . Try creating it manually: Estqes.SaveLoadSystem -> Create SaveTypeRegistry");
            }
            return reg;
        }
        private static Type GetMemberType(MemberInfo member) => member switch
        {
            FieldInfo f => f.FieldType,
            PropertyInfo p => p.PropertyType,
            _ => throw new ArgumentException("Member must be a FieldInfo or PropertyInfo")
        };
        private object GetMemberValue(MemberInfo member, object source)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).GetValue(source),
                MemberTypes.Property => ((PropertyInfo)member).GetValue(source),
                _ => null
            };
        }

        private MemberInfo[] GetSaveMembers(Type type)
        {
            if (!_saveCache.TryGetValue(type, out var info))
            {
                info = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(x => x.IsDefined(typeof(SaveAttribute), false))
                    .ToArray();
                _saveCache.Add(type, info);
            }

            return info;
        }

        private Loader[] GetLoaders(Type type)
        {
            if (!_loadersCache.TryGetValue(type, out var methods))
            {
                var constructors =
                type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(x => x.IsDefined(typeof(LoaderAttribute), false))
                .ToArray();

                var count = constructors.Length;
                methods = new Loader[count];

                for (int i = 0; i < count; i++)
                {
                    var item = constructors[i];
                    var attr = item.GetCustomAttribute(typeof(LoaderAttribute));

                    methods[i] = new Loader()
                    {
                        constructorInfo = item,
                        loaderAttribute = attr as LoaderAttribute,
                        parameterInfo = item.GetParameters()
                    };
                }
            }

            return methods;
        }

        private class Loader
        {
            public ConstructorInfo constructorInfo;
            public LoaderAttribute loaderAttribute;
            public ParameterInfo[] parameterInfo;

        }
    }
}

