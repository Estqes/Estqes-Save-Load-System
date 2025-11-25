using System;
using System.Collections.Generic;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    [CreateAssetMenu(menuName = "Estqes.SaveLoadSystem/Create SaveTypeRegistry")]
    public class SaveTypeRegistry : ScriptableObject
    {
        [field:SerializeField] public List<TypeMapping> Mappings { get; set; }
        private Dictionary<string, Type> _idToType;
        private Dictionary<Type, string> _typeToId;

        public void Initialize()
        {
            _idToType = new Dictionary<string, Type>();
            _typeToId = new Dictionary<Type, string>();

            foreach (var map in Mappings)
            {
                var type = Type.GetType(map.fullTypeName);
                if (type != null)
                {
                    _idToType[map.shortId] = type;
                    _typeToId[type] = map.shortId;
                }
            }
        }
        public string GetId(Type type)
        {
            if (_typeToId == null) Initialize();
            return _typeToId.TryGetValue(type, out var id) ? id : null;
        }

        public Type GetType(string id)
        {
            if (_idToType == null) Initialize();
            return _idToType.TryGetValue(id, out var type) ? type : null;
        }

        [Serializable]
        public class TypeMapping
        {
            public string fullTypeName;
            public string shortId;
        }
    }
}
