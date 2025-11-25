using System;

namespace Estqes.SaveLoadSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class LoadMethodAttribute : Attribute
    {
        public string[] ParamSourceNames { get; }

        public LoadMethodAttribute(params string[] paramSourceNames)
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


