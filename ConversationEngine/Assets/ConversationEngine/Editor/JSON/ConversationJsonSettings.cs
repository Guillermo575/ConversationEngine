using UnityEngine;
namespace ConversationEditor.JSON
{
    public static class ConversationJsonSettings
    {
        #region Deprecated Methods
        //public static string Serialize(object obj)
        //{
        //    if (obj is ConversationScheme.ConversationData conversationData)
        //    {
        //        return SimpleJsonSerializer.Serialize(conversationData);
        //    }
        //    return JsonUtility.ToJson(obj, true);
        //}
        //public static T Deserialize<T>(string json)
        //{
        //    if (typeof(T) == typeof(ConversationScheme.ConversationData))
        //    {
        //        return (T)(object)SimpleJsonSerializer.Deserialize(json);
        //    }
        //    return JsonUtility.FromJson<T>(json);
        //}
        //public static void DeserializeOverwrite<T>(string json, T targetObject) where T : class
        //{
        //    JsonUtility.FromJsonOverwrite(json, targetObject);
        //}
        #endregion
    }
}