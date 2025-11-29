using System;
using UnityEditor;
using UnityEngine;

namespace Estqes.SaveLoadSystem
{
    public interface ISaveableEntity
    {
        public Guid Id { get; set; }
    }

    public interface IContentEntity
    {
        public string Tag { get; }
    }
}


