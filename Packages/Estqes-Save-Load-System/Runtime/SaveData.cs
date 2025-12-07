using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Estqes.SaveLoadSystem
{
    [Serializable]
    public class SaveData
    {
        public List<EntityData> Entities { get; set; } = new List<EntityData>();
    }

    [Serializable]
    public class EntityData : SaveableData
    {
        public Guid id;
    }

    [Serializable]
    public class SaveableData
    {
        public string type;
        public Dictionary<string, JToken> Data = new Dictionary<string, JToken>();
    }
}


