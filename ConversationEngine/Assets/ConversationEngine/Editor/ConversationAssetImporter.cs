using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEngine;
using System;
using System.IO;
using ConversationScheme;

namespace ConversationEditor
{
    /// <summary>
    /// Custom asset importer for conversation JSON files
    /// Opens the conversation editor when double-clicking a valid conversation file
    /// </summary>
    [ScriptedImporter(1, "conversation")]
    public class ConversationAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // Read the JSON file
            string jsonContent = File.ReadAllText(ctx.assetPath);

            try
            {
                // Try to deserialize as ConversationData
                var conversationData = ConversationJsonSettings.Deserialize<ConversationData>(jsonContent);

                // Check if it's a valid conversation file
                if (conversationData != null && 
                    conversationData.ConversationManager != null && 
                    conversationData.ResourceManager != null)
                {
                    // Mark this as a conversation asset
                    // We'll use a TextAsset to represent it in Unity
                    var textAsset = new UnityEngine.TextAsset(jsonContent);
                    ctx.AddObjectToAsset("conversation", textAsset);
                    ctx.SetMainObject(textAsset);
                }
            }
            catch
            {
                // If deserialization fails, it's not a conversation file
                // Import as regular text asset
                var textAsset = new UnityEngine.TextAsset(jsonContent);
                ctx.AddObjectToAsset("text", textAsset);
                ctx.SetMainObject(textAsset);
            }
        }
    }

    /// <summary>
    /// Custom callback handler for opening conversation files
    /// </summary>
    public class ConversationAssetHandler
    {
        [OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);

            if (!assetPath.EndsWith(".conversation"))
                return false;

            // Try to load as conversation file
            try
            {
                string jsonContent = File.ReadAllText(assetPath);
                var conversationData = ConversationJsonSettings.Deserialize<ConversationData>(jsonContent);

                // Check if it's a valid conversation file
                if (conversationData != null && 
                    conversationData.ConversationManager != null && 
                    conversationData.ResourceManager != null)
                {
                    // Open in conversation editor
                    ConversationEditorWindow.OpenConversationFile(assetPath);
                    return true;
                }
            }
            catch
            {
                // Not a conversation file or invalid format
            }

            return false;
        }
    }

    [InitializeOnLoad]
    public static class ConversationProjectIconHandler
    {
        private const string ConversationIconPath = "Assets/ConversationEngine/Icons/conversation.png";
        private const string ActorIconPath = "Assets/ConversationEngine/Icons/actor.png";
        private static Texture2D conversationIcon;
        private static Texture2D actorIcon;
        static ConversationProjectIconHandler()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            LoadIcons();
        }
        private static void LoadIcons()
        {
            if (conversationIcon == null) conversationIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ConversationIconPath);
            if (actorIcon == null) actorIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ActorIconPath);
        }
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            LoadIcons();
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return;
            Texture2D icon = GetIconForAssetPath(assetPath);
            if (icon == null) return;
            Rect iconRect = GetProjectIconRect(selectionRect);
            if (iconRect.width <= 0f || iconRect.height <= 0f) return;
            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
        }
        private static Texture2D GetIconForAssetPath(string assetPath)
        {
            if (assetPath.EndsWith(".conversation", StringComparison.OrdinalIgnoreCase)) return conversationIcon;
            if (assetPath.EndsWith(".actor", StringComparison.OrdinalIgnoreCase)) return actorIcon;
            return null;
        }
        private static Rect GetProjectIconRect(Rect selectionRect)
        {
            bool isListMode = selectionRect.height <= 20f;
            if (isListMode) return new Rect(selectionRect.x, selectionRect.y, 16f, 16f);
            float size = Mathf.Min(32f, selectionRect.width - 6f);
            float x = selectionRect.x + (selectionRect.width - size) * 0.5f;
            float y = selectionRect.y + 2f;
            return new Rect(x, y, size, size);
        }
    }
}
