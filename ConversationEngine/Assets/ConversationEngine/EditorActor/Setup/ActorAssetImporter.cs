using System;
using System.IO;
using ConversationEditorActor.Helper;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Callbacks;
using UnityEngine;
namespace ConversationEditorActor.Setup
{
    [ScriptedImporter(1, "actor")]
    public class ActorAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            string jsonContent = File.ReadAllText(ctx.assetPath);
            try
            {
                var actor = ActorEditorHelpers.DeserializeActor(jsonContent);
                if (actor != null)
                {
                    var textAsset = new TextAsset(jsonContent);
                    ctx.AddObjectToAsset("actor", textAsset);
                    ctx.SetMainObject(textAsset);
                    return;
                }
            }
            catch
            {
            }
            var fallbackTextAsset = new TextAsset(jsonContent);
            ctx.AddObjectToAsset("text", fallbackTextAsset);
            ctx.SetMainObject(fallbackTextAsset);
        }
    }
    public class ActorAssetHandler
    {
        [OnOpenAsset(2)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (!assetPath.EndsWith(".actor", StringComparison.OrdinalIgnoreCase)) return false;
            try
            {
                string jsonContent = File.ReadAllText(assetPath);
                var actor = ActorEditorHelpers.DeserializeActor(jsonContent);
                if (actor == null) return false;
                ActorEditorWindow.OpenActorFile(assetPath);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
    [InitializeOnLoad]
    public static class ActorProjectIconHandler
    {
        private const string ActorIconPath = "Assets/ConversationEngine/Icons/actor.png";
        private static Texture2D actorIcon;
        static ActorProjectIconHandler()
        {
            EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
            LoadIcons();
        }
        private static void LoadIcons()
        {
            if (actorIcon == null) actorIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(ActorIconPath);
        }
        private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
        {
            LoadIcons();
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath)) return;
            if (!assetPath.EndsWith(".actor", StringComparison.OrdinalIgnoreCase)) return;
            if (actorIcon == null) return;
            Rect iconRect = GetProjectIconRect(selectionRect);
            if (iconRect.width <= 0f || iconRect.height <= 0f) return;
            GUI.DrawTexture(iconRect, actorIcon, ScaleMode.ScaleToFit, true);
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
