using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    [CreateAssetMenu(menuName = "Estqes.SaveLoadSystem/Create SaveTypeRegistry")]
    public class SaveTypeRegistry : ScriptableObject
    {
        [field: SerializeField] public List<TypeMapping> EntityMappings { get; set; } = new();
        private Dictionary<string, Type> _idToTypeEntity;
        private Dictionary<Type, string> _typeToIdEntity;

        [field: SerializeField] public List<IContentEntity> ContentMappings { get; set; } = new();
        private Dictionary<string, IContentEntity> _idToContent;

        public void Initialize()
        {
            _idToTypeEntity = new Dictionary<string, Type>();
            _typeToIdEntity = new Dictionary<Type, string>();

            _idToContent = new Dictionary<string, IContentEntity>();

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
                _idToContent[map.Tag] = map;
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

        public IContentEntity GetContentEntity(string tag)
        {
            return _idToContent[tag];
        }

        [Serializable]
        public class TypeMapping
        {
            public string fullTypeName;
            public string shortId;
        }

        [Serializable]
        public class ContentMapping
        {
            public string shortId;
            public IContentEntity content;
        }
    }
}
