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
    public class EntityData
    {
        public Guid id;
        public string type;
        public Dictionary<string, JToken> Data = new Dictionary<string, JToken>();
    }
}


