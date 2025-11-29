using Estqes.SaveLoadSystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class SimpleSaveLoadManager : MonoBehaviour
{
    [SerializeField] private string _fileName;
    [SerializeField] private string _path;

    private void Awake()
    {
        new SaveLoadManager()
        {
            FileName = _fileName,
            FilePath = _path,
            SerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>()
                {
                    new TransformConverter()
                }
            }
        }.Init();
    }

    private void OnValidate()
    {
        if (SaveLoadManager.Instance == null) return;
        SaveLoadManager.Instance.FileName = _fileName;
        SaveLoadManager.Instance.FilePath = _path;
    }
}
