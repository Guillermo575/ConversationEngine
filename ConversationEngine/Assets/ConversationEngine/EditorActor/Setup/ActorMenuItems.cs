using System.IO;
using ConversationEditorActor.Helper;
using UnityEditor;
using UnityEngine;
namespace ConversationEditorActor.Setup
{
    public static class ActorMenuItems
    {
        [MenuItem("Assets/Create/ConversationEngine/Actor File", false, 81)]
        public static void CreateActorFile()
        {
            string folderPath = ActorEditorHelpers.GetSelectedPathOrFallback();
            string fullPath = ActorEditorHelpers.GetUniqueFilePath(folderPath, "NewActor", ".actor");
            string json = ActorEditorHelpers.SerializeActor(ActorEditorHelpers.CreateDefaultActor());
            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();
            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log("Created actor file at: " + relativePath);
        }
    }
}
