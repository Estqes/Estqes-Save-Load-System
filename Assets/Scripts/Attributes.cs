using System;

namespace Estqes.SaveLoadSystem
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SaveAttribute : Attribute { }
    public class LoadConstructorAttribute : Attribute
    {
        public string[] ParamSourceNames { get; }

        public LoadConstructorAttribute(params string[] paramSourceNames)
        {
            ParamSourceNames = paramSourceNames;
        }
    }


}


