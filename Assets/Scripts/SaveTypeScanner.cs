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

        var domain = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes());

        var allSaveableTypes = domain
            .Where(t => t.IsDefined(typeof(EntityTypeAttribute), false) && !t.IsAbstract);

        var allContentTypes = domain.Where(t => t.GetInterface(nameof(IContentEntity)) != null);

        HashSet<string> usedIds = new HashSet<string>(registry.EntityMappings.Select(m => m.shortId));

        foreach (var type in allSaveableTypes)
        {
            var existingMap = registry.EntityMappings.Find(m => m.fullTypeName == type.AssemblyQualifiedName);
            if (existingMap != null) continue;

            var attribute = type.GetCustomAttribute<EntityTypeAttribute>();
            string newId = attribute.TypeName;

            registry.EntityMappings.Add(new SaveTypeRegistry.TypeMapping
            {
                fullTypeName = type.AssemblyQualifiedName,
                shortId = attribute.TypeName
            });

            usedIds.Add(newId);
            dirty = true;
        }

        foreach (var type in allContentTypes)
        {
            var existingMap = registry.ContentMappings.Find(m => m.fullTypeName == type.AssemblyQualifiedName);
            if (existingMap != null) continue;
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