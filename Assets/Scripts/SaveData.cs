using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Estqes.SaveLoadSystem
{
    [Serializable]
    public class SaveData
    {
        public List<EntityData> Entities = new List<EntityData>();
    }

    [Serializable]
    public class EntityData
    {
        public Guid Id;
        public string Type;
        public Dictionary<string, JToken> Data = new Dictionary<string, JToken>();
    }
}


