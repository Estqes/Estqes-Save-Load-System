using Newtonsoft.Json;
using System;
using UnityEngine;

namespace Estqes.SaveLoadSystem 
{
    [Serializable]
    public struct TransformSaveData
    {
        public float[] P; // Position (x, y, z)
        public float[] R; // Rotation (x, y, z, w)
        public float[] S; // Scale (x, y, z)

        public TransformSaveData(Transform t)
        {
            P = new[] { t.localPosition.x, t.localPosition.y, t.localPosition.z };
            R = new[] { t.localRotation.x, t.localRotation.y, t.localRotation.z, t.localRotation.w };
            S = new[] { t.localScale.x, t.localScale.y, t.localScale.z };
        }

        public void ApplyTo(Transform t)
        {
            if (P != null) t.localPosition = new Vector3(P[0], P[1], P[2]);
            if (R != null) t.localRotation = new Quaternion(R[0], R[1], R[2], R[3]);
            if (S != null) t.localScale = new Vector3(S[0], S[1], S[2]);
        }
    }
    public class TransformConverter : JsonConverter<Transform>
    {
        public override void WriteJson(JsonWriter writer, Transform value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var data = new TransformSaveData(value);
            serializer.Serialize(writer, data);
        }

        public override Transform ReadJson(JsonReader reader, Type objectType, Transform existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var data = serializer.Deserialize<TransformSaveData>(reader);

            if (hasExistingValue && existingValue != null)
            {
                data.ApplyTo(existingValue);
                return existingValue;
            }

            return null;
        }
    }

}

