using System.Collections.Generic;
using UnityEngine;
namespace ConversationEngine.GameHelper
{
    public static class DictionaryHelper
    {
        [SerializeField] private static Dictionary<string, string> myDictionary;
        public static string getValueDict(string name)
        {
            if (myDictionary != null && myDictionary.ContainsKey(name))
            {
                return myDictionary[name];
            }
            return null;
        }
        public static void setValueDict(string name, string value)
        {
            if (myDictionary != null)
            {
                myDictionary[name] = value;
            }
        }
    }
}