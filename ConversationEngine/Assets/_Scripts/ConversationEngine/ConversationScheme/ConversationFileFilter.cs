using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
namespace ConversationEngine.ConversationScheme
{
    public class ConversationFileFilter : PropertyAttribute { }
    [CustomPropertyDrawer(typeof(ConversationFileFilter))]
    public class ConversationFileFilterDrawer : PropertyDrawer
    {
        #region Unity Methods
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            string[] conversationPaths = AssetDatabase.FindAssets("t:TextAsset")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(x => x.EndsWith(".conversation"))
                .OrderBy(x => x)
                .ToArray();
            string[] options = new string[conversationPaths.Length + 1];
            options[0] = "None";
            for (int i = 0; i < conversationPaths.Length; i++) options[i + 1] = Path.GetFileNameWithoutExtension(conversationPaths[i]);
            string currentPath = property.objectReferenceValue == null ? string.Empty : AssetDatabase.GetAssetPath(property.objectReferenceValue);
            int currentIndex = System.Array.IndexOf(conversationPaths, currentPath);
            int popupIndex = currentIndex + 1;
            int nextIndex = EditorGUI.Popup(position, label.text, popupIndex, options);
            if (nextIndex == popupIndex) return;
            if (nextIndex == 0)
            {
                property.objectReferenceValue = null;
                return;
            }
            string selectedPath = conversationPaths[nextIndex - 1];
            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(selectedPath);
        }
        #endregion
    }
}