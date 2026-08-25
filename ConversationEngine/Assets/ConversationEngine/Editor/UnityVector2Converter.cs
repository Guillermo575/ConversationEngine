using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// Custom JSON converter for Unity's Vector2 to avoid circular reference issues
    /// </summary>
    public class UnityVector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("X");
            writer.WriteValue(value.x);
            writer.WritePropertyName("Y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);
            float x = obj["X"]?.Value<float>() ?? 0f;
            float y = obj["Y"]?.Value<float>() ?? 0f;
            return new Vector2(x, y);
        }
    }
}
