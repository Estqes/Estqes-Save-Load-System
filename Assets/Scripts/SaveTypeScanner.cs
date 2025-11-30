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

    [MenuItem("Tools/SaveSystem/Force Rescan Types")]
    public static void ScanAndRegisterTypes()
    {
        var registry = LoadOrCreateRegistry();
        bool dirty = false;

        var domainTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes());

        var allSaveableTypes = domainTypes
            .Where(t => t.IsDefined(typeof(EntityTypeAttribute), false) && !t.IsAbstract);

        foreach (var type in allSaveableTypes)
        {

            var existingMap = registry.EntityMappings.Find(m => m.fullTypeName == type.AssemblyQualifiedName);
            if (existingMap != null) continue;

            var attribute = type.GetCustomAttribute<EntityTypeAttribute>();
            
            registry.EntityMappings.Add(new SaveTypeRegistry.TypeMapping
            {
                fullTypeName = type.AssemblyQualifiedName,
                shortId = attribute.TypeName
            });

            dirty = true;
        }

        LoadContent(registry);

        if (dirty)
        {
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log("[SaveTypeScanner] Registry updated successfully.");
        }
    }

    

    private static void LoadContent(SaveTypeRegistry registry)
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject t:Prefab");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);

            if (asset == null) continue;

            IContentEntity contentEntity = null;

            if (asset is IContentEntity contentEntitySO)
            {
                contentEntity = contentEntitySO; ;
            }

            else if (asset is GameObject go)
            {
                contentEntity = go.GetComponentInChildren<IContentEntity>(true);
            }

            if (contentEntity == null) continue;
            var existingMap = registry.ContentMappings.Find(m => m.Tag == contentEntity.Tag);
            if (existingMap != null) continue;

            registry.ContentMappings.Add(contentEntity);
        }
    }

    static SaveTypeRegistry LoadOrCreateRegistry()
    {
        string path = SaveLoadManager.RegistryPath; 

        var reg = AssetDatabase.LoadAssetAtPath<SaveTypeRegistry>(path);
        if (reg == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            reg = ScriptableObject.CreateInstance<SaveTypeRegistry>();
            AssetDatabase.CreateAsset(reg, path);
        }
        return reg;
    }
}
#endif