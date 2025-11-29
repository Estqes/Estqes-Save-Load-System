using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    [CreateAssetMenu(menuName = "Estqes.SaveLoadSystem/Create SaveTypeRegistry")]
    public class SaveTypeRegistry : ScriptableObject
    {
        [field:SerializeField] public List<TypeMapping> EntityMappings { get; set; }
        private Dictionary<string, Type> _idToTypeEntity;
        private Dictionary<Type, string> _typeToIdEntity;

        [field: SerializeField] public List<TypeMapping> ContentMappings { get; set; }
        private Dictionary<string, Type> _idToTypeContent;
        private Dictionary<Type, string> _typeToIdContent;

        public void Initialize()
        {
            _idToTypeEntity = new Dictionary<string, Type>();
            _typeToIdEntity = new Dictionary<Type, string>();

            _idToTypeContent = new Dictionary<string, Type>();
            _typeToIdContent = new Dictionary<Type, string>();

            foreach (var map in EntityMappings)
            {
                var type = Type.GetType(map.fullTypeName);
                if (type != null)
                {
                    _idToTypeEntity[map.shortId] = type;
                    _typeToIdEntity[type] = map.shortId;
                }
            }

            foreach (var map in ContentMappings)
            {
                var type = Type.GetType(map.fullTypeName);
                if (type != null)
                {
                    _idToTypeContent[map.shortId] = type;
                    _typeToIdContent[type] = map.shortId;
                }
            }
        }

        public string GetIdEntity(Type type)
        {
            if (_typeToIdEntity == null) Initialize();
            return _typeToIdEntity.TryGetValue(type, out var id) ? id : null;
        }

        public Type GetTypeEntity(string id)
        {
            if (_idToTypeEntity == null) Initialize();
            return _idToTypeEntity.TryGetValue(id, out var type) ? type : null;
        }

        [Serializable]
        public class TypeMapping
        {
            public string fullTypeName;
            public string shortId;
        }
    }
}
