using System;

namespace Estqes.SaveLoadSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : Attribute 
    {
        public string Name { get; }
        public SaveAttribute(string name) 
        {
            Name = name;
        }
        public SaveAttribute()
        {
            
        }
    }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class LoaderAttribute : Attribute
    {
        public string[] ParamSourceNames { get; }

        public LoaderAttribute(params string[] paramSourceNames)
        {
            ParamSourceNames = paramSourceNames;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class EntityTypeAttribute : Attribute
    {
        public string TypeName { get; }
        public EntityTypeAttribute(string name) => TypeName = name;
    }
}


