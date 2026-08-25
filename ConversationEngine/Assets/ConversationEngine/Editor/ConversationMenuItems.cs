using UnityEditor;
using UnityEngine;
using System.IO;
using ConversationScheme;
using Newtonsoft.Json;
using BodyPartType = ConversationScheme.BodyPart;
using System.Xml;

namespace ConversationEditor
{
    /// <summary>
    /// Menu items for creating conversation engine assets
    /// </summary>
    public static class ConversationMenuItems
    {
        [MenuItem("Assets/Create/ConversationEngine/Conversation File", false, 80)]
        public static void CreateConversationFile()
        {
            // Get the selected folder path
            string path = GetSelectedPathOrFallback();

            // Create a new conversation data with Start and End nodes
            ConversationData conversationData = new ConversationData();

            // Create Start node
            var startNode = new ConversationNode
            {
                Id = 1,
                NodeType = ConversationNodeType.Start,
                EditorPosition = new Vector2(0, 0),
                EditorSize = new Vector2(150, 80)
            };

            // Create End node
            var endNode = new ConversationNode
            {
                Id = 2,
                NodeType = ConversationNodeType.End,
                EditorPosition = new Vector2(400, 0),
                EditorSize = new Vector2(150, 80)
            };

            startNode.NextNodeId = endNode.Id;

            conversationData.ConversationManager.Nodes.Add(startNode);
            conversationData.ConversationManager.Nodes.Add(endNode);

            // Serialize to JSON
            string json = ConversationJsonSettings.Serialize(conversationData);

            // Find unique filename
            string fileName = "NewConversation.json";
            string fullPath = Path.Combine(path, fileName);
            int counter = 1;

            while (File.Exists(fullPath))
            {
                fileName = $"NewConversation{counter}.json";
                fullPath = Path.Combine(path, fileName);
                counter++;
            }

            // Write file
            File.WriteAllText(fullPath, json);

            // Refresh and select the asset
            AssetDatabase.Refresh();

            // Convert to relative path for Unity
            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);

            // Select the newly created asset
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"Created conversation file at: {relativePath}");
        }

        [MenuItem("Assets/Create/ConversationEngine/Actor File", false, 81)]
        public static void CreateActorFile()
        {
            // Get the selected folder path
            string path = GetSelectedPathOrFallback();

            // Create a new actor
            Actor actor = new Actor
            {
                Id = "new_actor",
                Path = "",
                IconPath = "",
                SoundEffectPaths = new System.Collections.Generic.List<string>(),
                BodyParts = new System.Collections.Generic.List<BodyPartType>()
            };

            // Add default body parts
            actor.BodyParts.Add(new BodyPartType
            {
                Id = "body",
                AttachToPivotId = "",
                CurrentResourceId = "",
                NestedResources = new System.Collections.Generic.List<BodyPartResource>(),
                PivotPoints = new System.Collections.Generic.List<PivotPoint>()
            });

            // Serialize to JSON
            string json = ConversationJsonSettings.Serialize(actor);

            // Find unique filename
            string fileName = "NewActor.json";
            string fullPath = Path.Combine(path, fileName);
            int counter = 1;

            while (File.Exists(fullPath))
            {
                fileName = $"NewActor{counter}.json";
                fullPath = Path.Combine(path, fileName);
                counter++;
            }

            // Write file
            File.WriteAllText(fullPath, json);

            // Refresh and select the asset
            AssetDatabase.Refresh();

            // Convert to relative path for Unity
            string relativePath = "Assets" + fullPath.Substring(Application.dataPath.Length);

            // Select the newly created asset
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"Created actor file at: {relativePath}");
        }

        /// <summary>
        /// Gets the selected folder path in the Project window, or falls back to Assets folder
        /// </summary>
        private static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }

            return path;
        }
    }
}
