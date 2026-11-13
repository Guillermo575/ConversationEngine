using System.IO;
using ConversationEngine.EditorActor.Export;
using ConversationEngine.EditorActor.Helper;
using ConversationEngine.ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEngine.EditorActor.Setup
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
        [MenuItem("Assets/ConversationEngine/Export Actor To Scene", true, 82)]
        public static bool ValidateExportActorToScene()
        {
            string actorPath = GetSelectedActorPath();
            return !string.IsNullOrWhiteSpace(actorPath);
        }
        [MenuItem("Assets/ConversationEngine/Export Actor To Scene", false, 82)]
        public static void ExportActorToScene()
        {
            string actorPath = GetSelectedActorPath();
            if (string.IsNullOrWhiteSpace(actorPath))
            {
                EditorUtility.DisplayDialog("Actor Export", "Select a .actor file first.", "OK");
                return;
            }
            if (!File.Exists(actorPath))
            {
                EditorUtility.DisplayDialog("Actor Export", "The selected .actor file does not exist.", "OK");
                return;
            }
            Actor actorData;
            try
            {
                string json = File.ReadAllText(actorPath);
                actorData = ActorEditorHelpers.DeserializeActor(json);
            }
            catch
            {
                EditorUtility.DisplayDialog("Actor Export", "Could not read actor data from selected file.", "OK");
                return;
            }
            if (actorData == null)
            {
                EditorUtility.DisplayDialog("Actor Export", "Selected file is not a valid actor asset.", "OK");
                return;
            }
            ActorExportWindow.Open(actorData, options => ExecuteSceneExport(actorData, actorPath, options));
        }
        private static void ExecuteSceneExport(Actor actorData, string actorAssetPath, ActorExportRequest options)
        {
            GameObject actorRoot = ActorSceneExportService.ExportToScene(actorData, actorAssetPath, options);
            if (actorRoot == null)
            {
                EditorUtility.DisplayDialog("Actor Export", "Actor export failed. Verify actor resources and options.", "OK");
                return;
            }
            EditorUtility.DisplayDialog("Actor Export", "Actor exported to scene successfully.", "OK");
        }
        private static string GetSelectedActorPath()
        {
            Object selectedObject = Selection.activeObject;
            if (selectedObject == null) return "";
            string selectedAssetPath = AssetDatabase.GetAssetPath(selectedObject);
            if (string.IsNullOrWhiteSpace(selectedAssetPath)) return "";
            if (!selectedAssetPath.EndsWith(".actor", System.StringComparison.OrdinalIgnoreCase)) return "";
            return Path.GetFullPath(selectedAssetPath);
        }
    }
}