using Newtonsoft.Json;
using System.Collections.Generic;

namespace ConversationEditor
{
    /// <summary>
    /// Provides configured JSON serializer settings for Conversation data
    /// </summary>
    public static class ConversationJsonSettings
    {
        private static JsonSerializerSettings _settings;

        /// <summary>
        /// Gets the configured JSON serializer settings
        /// </summary>
        public static JsonSerializerSettings Settings
        {
            get
            {
                if (_settings == null)
                {
                    _settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                        NullValueHandling = NullValueHandling.Include,
                        Converters = new List<JsonConverter>
                        {
                            new UnityVector2Converter()
                        }
                    };
                }
                return _settings;
            }
        }

        /// <summary>
        /// Serializes an object to JSON with proper Unity type handling
        /// </summary>
        public static string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj, Settings);
        }

        /// <summary>
        /// Deserializes JSON to an object with proper Unity type handling
        /// </summary>
        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, Settings);
        }
    }
}
