using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// Provides JSON serialization utilities for Conversation data using Unity's JsonUtility
    /// </summary>
    public static class ConversationJsonSettings
    {
        /// <summary>
        /// Serializes an object to JSON using Unity's JsonUtility with pretty print
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonUtility.ToJson(obj, true);
        }

        /// <summary>
        /// Deserializes JSON to an object using Unity's JsonUtility
        /// </summary>
        public static T Deserialize<T>(string json)
        {
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
