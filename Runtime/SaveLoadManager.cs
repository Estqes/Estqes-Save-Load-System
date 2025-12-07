using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        private JToken SerializeValue(object value, SaveData saveData)
        {
            if (value == null)
            {
                return JValue.CreateNull();
            }

            var type = value.GetType();


            if (value is IContentEntity content)
            {
                return JToken.FromObject(content.Tag);
            }
            else if(value is ISaveableEntity entity)
            {
                Debug.Log("Good");
                return JToken.FromObject(entity.Id);
            }

            else if (value is IEnumerable collection && value is not string)
            {
                var jArray = new JArray();
                foreach (var item in collection)
                {
                    jArray.Add(SerializeValue(item, saveData));
                }
                return jArray;
            }

            else if (type.IsDefined(typeof(EntityTypeAttribute), false))
            {
                var id = _registry.GetIdEntity(type);
                if (string.IsNullOrEmpty(id)) id = type.Name;

                var nestedData = new SaveableData()
                {
                    type = id
                };

                SaveObject("", value, saveData, nestedData);

                return JToken.FromObject(nestedData);
            }

            return JToken.FromObject(value, _serializer);
        }

        public string Serialize(IEnumerable<ISaveableEntity> entities)
        {
            var saveData = new SaveData();
            foreach (var item in entities)
            {
                SaveEntity(item, saveData);
            }

            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            Debug.Log(json);
            return json;

        }

        public void Save()
        {
            Save(AllSaveableEntity.GetAll());
        }

        public void Save(IEnumerable<ISaveableEntity> entities)
        {
            OnSaveStart?.Invoke();
            var json = Serialize(entities);
            File.WriteAllText(SavePath, json);
            OnSaveEnd?.Invoke();
        }

        private void SaveObject(string propertyName, object obj, SaveData saveData, SaveableData saveableData)
        {
            var type = obj.GetType();
            var saveMembers = GetSaveMembers(type);

            foreach (var member in saveMembers)
            {
                object memberValue = GetMemberValue(member, obj);
                string memberName = member.Name;

                JToken token = SerializeValue(memberValue, saveData);

                saveableData.Data.Add(memberName, token);
            }
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

            SaveObject("", entity, saveData, entityData);

            saveData.Entities.Add(entityData);
        }

        public List<ISaveableEntity> Load(string json, bool regiserInAllsaveable = true)
        {
            OnLoadStart?.Invoke();

            var saveData = JsonConvert.DeserializeObject<SaveData>(json, SerializerSettings);
            AllSaveableEntity.Clear();

            var listEntites = new List<ISaveableEntity>();

            List<EntityData> sortedEntities;
            try
            {
                sortedEntities = GetTopologicallySortedEntities(saveData);
            }
            catch (InvalidOperationException ex)
            {
                Debug.LogError($"Failed to determine load order: {ex.Message}");
                return null;
            }

            foreach (var entityData in sortedEntities)
            {
                var type = _registry.GetTypeEntity(entityData.type);

                var loader = FindLoader(entityData);
                if (loader == null)
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

                listEntites.Add(newEntity);
                if(regiserInAllsaveable) AllSaveableEntity.Register(newEntity);

                FillProperties(entityData.Data, newEntity, type, constructorParams);
            }

            OnLoadEnd?.Invoke();
            return listEntites;
        }

        public void Load()
        {
            string json = File.ReadAllText(SavePath);
            Load(json);
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

        private void FillProperties(Dictionary<string, JToken> dataDict, object entity, Type entityType, ParameterInfo[] constructorParams = null)
        {
            var members = GetSaveMembers(entityType);

            foreach (var member in members)
            {
                if (constructorParams != null && constructorParams.Any(x => x.Name == member.Name)) continue;

                if (!dataDict.TryGetValue(member.Name, out var token)) continue;

                var memberType = GetMemberType(member);

                var valueToSet = DeserializeValue(token, memberType);

                if (member is FieldInfo field) field.SetValue(entity, valueToSet);
                if (member is PropertyInfo property && property.CanWrite) property.SetValue(entity, valueToSet);
            }
        }

        private object DeserializeValue(JToken jToken, Type targetType)
        {
            if (jToken == null || jToken.Type == JTokenType.Null) return null;

            if (typeof(IContentEntity).IsAssignableFrom(targetType) && jToken.Type == JTokenType.String)
            {
                string tag = jToken.ToString();
                // Берем объект из реестра
                return _registry.GetContentEntity(tag);
            }
            if (typeof(ISaveableEntity).IsAssignableFrom(targetType))
            {
                if (jToken.Type == JTokenType.String && Guid.TryParse(jToken.ToString(), out Guid refId))
                {
                    var existingEntity = AllSaveableEntity.GetEnity(refId);
                    if (existingEntity == null)
                    {
                        Debug.LogWarning($"Entity with ID {refId} not found for field of type {targetType.Name}. It might not be loaded yet or was destroyed.");
                    }
                    return existingEntity;
                }
            }

            if (jToken is JObject jObj && jObj.ContainsKey("type") && jObj.ContainsKey("Data"))
            {
                string typeName = jObj["type"].ToString();
                Type actualType = _registry.GetTypeEntity(typeName) ?? targetType;

                if (actualType != null)
                {
                    object instance = Activator.CreateInstance(actualType);
                    var dataDict = jObj["Data"].ToObject<Dictionary<string, JToken>>(_serializer);

                    FillProperties(dataDict, instance, actualType);
                    return instance;
                }
            }

            if (jToken is JArray jArray && typeof(IEnumerable).IsAssignableFrom(targetType) && targetType != typeof(string))
            {
                Type elementType = null;
                if (targetType.IsArray)
                {
                    elementType = targetType.GetElementType();
                }
                else if (targetType.IsGenericType)
                {
                    elementType = targetType.GetGenericArguments()[0];
                }

                if (elementType != null)
                {
                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                    foreach (var childToken in jArray)
                    {
                        list.Add(DeserializeValue(childToken, elementType));
                    }

                    if (targetType.IsArray)
                    {
                        var array = Array.CreateInstance(elementType, list.Count);
                        list.CopyTo(array, 0);
                        return array;
                    }
                    return list;
                }
            }

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
            const string registryFileName = "SaveTypeRegistry";

            var reg = Resources.Load<SaveTypeRegistry>(registryFileName);

            if (reg == null)
            {
                throw new Exception($"SaveTypeRegistry not found! Please create a folder named 'Resources' and put the 'SaveTypeRegistry' asset there.");
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

