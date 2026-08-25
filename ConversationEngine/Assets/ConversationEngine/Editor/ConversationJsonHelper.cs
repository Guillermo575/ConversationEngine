using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// JSON serialization helper for conversation data
    /// Uses Unity's JsonUtility with custom handling for dictionaries
    /// </summary>
    public static class ConversationJsonHelper
    {
        /// <summary>
        /// Serializes conversation data to JSON string
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonUtility.ToJson(obj, true);
        }

        /// <summary>
        /// Deserializes JSON string to conversation data
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonUtility.FromJson<T>(json);
        }

        /// <summary>
        /// Attempts to deserialize, returns null if fails
        /// </summary>
        public static bool TryDeserialize<T>(string json, out T result)
        {
            try
            {
                result = JsonUtility.FromJson<T>(json);
                return result != null;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }
    }
}
