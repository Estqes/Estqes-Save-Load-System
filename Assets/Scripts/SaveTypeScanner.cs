#if UNITY_EDITOR
using Estqes.SaveLoadSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class SaveTypeScanner
{
    static SaveTypeScanner()
    {
        EditorApplication.delayCall += ScanAndRegisterTypes;
    }

    static void ScanAndRegisterTypes()
    {
        var registry = LoadOrCreateRegistry();
        bool dirty = false;

        var allSaveableTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsDefined(typeof(EntityTypeAttribute), false) && !t.IsAbstract)
            .ToList();

        HashSet<string> usedIds = new HashSet<string>(registry.Mappings.Select(m => m.shortId));

        foreach (var type in allSaveableTypes)
        {
            var existingMap = registry.Mappings.Find(m => m.fullTypeName == type.FullName);


            if (existingMap == null)
            {
                var attribute = type.GetCustomAttribute<EntityTypeAttribute>();            
                string newId = attribute.TypeName;

                registry.Mappings.Add(new SaveTypeRegistry.TypeMapping
                {
                    fullTypeName = type.AssemblyQualifiedName,
                    shortId = attribute.TypeName
                });

                usedIds.Add(newId);
                dirty = true;

            }
        }

        if (dirty)
        {
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
        }
    }

    static SaveTypeRegistry LoadOrCreateRegistry()
    {
        var reg = AssetDatabase.LoadAssetAtPath<SaveTypeRegistry>(SaveLoadManager.RegistryPath);
        if (reg == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            reg = ScriptableObject.CreateInstance<SaveTypeRegistry>();
            AssetDatabase.CreateAsset(reg, SaveLoadManager.RegistryPath);
        }
        return reg;
    }
}
#endif