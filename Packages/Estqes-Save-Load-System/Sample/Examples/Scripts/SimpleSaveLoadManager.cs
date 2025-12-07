using Estqes.SaveLoadSystem;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimpleSaveLoadManager : MonoBehaviour
{
    [SerializeField] private string _fileName = "save_data.json";

    [SerializeField] private string _customPath = "";

    private void Awake()
    {
        string actualPath = string.IsNullOrWhiteSpace(_customPath)
            ? Application.persistentDataPath
            : _customPath;

        Debug.Log($"[SaveSystem] Path: {Path.Combine(actualPath, _fileName)}");

        new SaveLoadManager()
        {
            FileName = _fileName,
            FilePath = actualPath,
            SerializerSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented,
                Converters = new List<JsonConverter>()
                {
                    //Place convertors to here
                }
            }
        }.Init();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (SaveLoadManager.Instance == null) return;

        string actualPath = string.IsNullOrWhiteSpace(_customPath)
            ? Application.persistentDataPath
            : _customPath;

        SaveLoadManager.Instance.FileName = _fileName;
        SaveLoadManager.Instance.FilePath = actualPath;
    }
#endif

}
