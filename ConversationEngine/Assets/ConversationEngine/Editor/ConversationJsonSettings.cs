using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// Provides JSON serialization utilities for Conversation data using SimpleJsonSerializer
    /// </summary>
    public static class ConversationJsonSettings
    {
        /// <summary>
        /// Serializes an object to JSON using SimpleJsonSerializer with pretty print
        /// </summary>
        public static string Serialize(object obj)
        {
            if (obj is ConversationScheme.ConversationData conversationData)
            {
                return SimpleJsonSerializer.Serialize(conversationData);
            }
            // Fallback to Unity's JsonUtility for other types
            return JsonUtility.ToJson(obj, true);
        }

        /// <summary>
        /// Deserializes JSON to an object using SimpleJsonSerializer
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            if (typeof(T) == typeof(ConversationScheme.ConversationData))
            {
                return (T)(object)SimpleJsonSerializer.Deserialize(json);
            }
            // Fallback to Unity's JsonUtility for other types
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// Deserializes JSON to an existing object using Unity's JsonUtility
        /// </summary>
        public static void DeserializeOverwrite<T>(string json, T targetObject) where T : class
        {
            JsonUtility.FromJsonOverwrite(json, targetObject);
        }
    }
}
