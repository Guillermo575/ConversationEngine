using System;
using System.IO;
using ConversationEditorActor.Graph;
using ConversationEditorActor.Helper;
using ConversationScheme;
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
    [CustomEditor(typeof(ActorAssetImporter))]
    public class ActorAssetImporterEditor : ScriptedImporterEditor
    {
        #region Constants
        private const float previewHeight = 560f;
        #endregion

        #region State
        private Actor actorData;
        private ActorEditorGraph actorPreviewGraph;
        private bool isValidActor;
        #endregion

        #region Unity
        public override bool showImportedObject => false;
        public override void OnEnable()
        {
            base.OnEnable();
            if (actorPreviewGraph == null) actorPreviewGraph = new ActorEditorGraph();
            LoadActorData();
        }
        public override void OnInspectorGUI()
        {
            DrawHeader();
            if (!isValidActor)
            {
                EditorGUILayout.HelpBox("The selected file could not be loaded as a valid actor asset.", MessageType.Warning);
                ApplyRevertGUI();
                return;
            }
            DrawActorReadOnlyInfo();
            DrawPreview();
            DrawActions();
            ApplyRevertGUI();
        }
        #endregion

        #region Draw
        private void DrawHeader()
        {
            var importer = target as ActorAssetImporter;
            EditorGUILayout.LabelField(new GUIContent("Actor Preview", "Read-only preview for the selected actor file."), EditorStyles.boldLabel);
            if (importer == null) return;
            EditorGUILayout.LabelField(new GUIContent(Path.GetFileName(importer.assetPath), "Selected actor file."));
            EditorGUILayout.Space(4f);
        }
        private void DrawActorReadOnlyInfo()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Actor", "Actor data read-only summary."), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(new GUIContent("Id", "Actor id."), new GUIContent(actorData.Id ?? ""));
            EditorGUILayout.LabelField(new GUIContent("Path", "Actor file path field."), new GUIContent(actorData.Path ?? ""));
            EditorGUILayout.LabelField(new GUIContent("Icon Path", "Actor icon resource path."), new GUIContent(actorData.IconPath ?? ""));
            EditorGUILayout.LabelField(new GUIContent("Canvas Size", "Actor canvas size."), new GUIContent($"X: {actorData.Size.x:0.##}  Y: {actorData.Size.y:0.##}"));
            int bodyPartCount = actorData.BodyParts == null ? 0 : actorData.BodyParts.Count;
            EditorGUILayout.LabelField(new GUIContent("BodyParts", "Total body parts in actor."), new GUIContent(bodyPartCount.ToString()));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6f);
        }
        private void DrawPreview()
        {
            var importer = target as ActorAssetImporter;
            if (importer == null) return;
            EditorGUILayout.LabelField(new GUIContent("Preview", "Rendered actor composition from body part resources."), EditorStyles.boldLabel);
            Rect previewRect = GUILayoutUtility.GetRect(10f, previewHeight, GUILayout.ExpandWidth(true));
            actorPreviewGraph.DrawReadOnlyPreview(previewRect, actorData, importer.assetPath);
        }
        private void DrawActions()
        {
            var importer = target as ActorAssetImporter;
            if (importer == null) return;
            EditorGUILayout.Space(6f);
            if (GUILayout.Button(new GUIContent("Open Actor Editor", "Open this actor file in the full editor."), GUILayout.Height(24f))) ActorEditorWindow.OpenActorFile(importer.assetPath);
        }
        #endregion

        #region Data
        private void LoadActorData()
        {
            isValidActor = false;
            actorData = null;
            var importer = target as ActorAssetImporter;
            if (importer == null) return;
            try
            {
                string json = File.ReadAllText(importer.assetPath);
                actorData = ActorEditorHelpers.DeserializeActor(json);
                isValidActor = actorData != null;
            }
            catch
            {
                actorData = null;
                isValidActor = false;
            }
        }
        #endregion
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
