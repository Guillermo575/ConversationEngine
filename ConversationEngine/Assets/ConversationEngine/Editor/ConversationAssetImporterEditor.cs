using System.IO;
using ConversationScheme;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;
namespace ConversationEditor
{
    [CustomEditor(typeof(ConversationAssetImporter))]
    public class ConversationAssetImporterEditor : ScriptedImporterEditor
    {
        #region Constants
        private const float graphPreviewHeight = 420f;
        #endregion
        #region State
        private ConversationData conversationData;
        private ConversationGraphView graphView;
        private bool shouldFrameGraph = true;
        private bool isValidConversation;
        #endregion
        #region Unity Lifecycle
        public override bool showImportedObject => false;
        public override void OnEnable()
        {
            base.OnEnable();
            EnsureGraphView();
            LoadConversationData();
        }
        public override void OnDisable()
        {
            base.OnDisable();
        }
        public override void OnInspectorGUI()
        {
            DrawHeader();
            if (!isValidConversation)
            {
                DrawInvalidState();
                return;
            }
            DrawSummary();
            DrawGraphPreview();
            DrawActions();
        }
        #endregion
        #region Setup
        private void EnsureGraphView()
        {
            if (graphView != null) return;
            graphView = new ConversationGraphView(null, true);
            graphView.SetReadOnlyMode(true);
        }
        private void LoadConversationData()
        {
            isValidConversation = false;
            conversationData = null;
            shouldFrameGraph = true;
            var importer = target as ConversationAssetImporter;
            if (importer == null) return;
            try
            {
                string jsonContent = File.ReadAllText(importer.assetPath);
                conversationData = ConversationJsonSettings.Deserialize<ConversationData>(jsonContent);
                isValidConversation = conversationData != null && conversationData.ConversationManager != null && conversationData.ResourceManager != null;
                if (!isValidConversation) return;
                graphView.SetConversationData(conversationData);
                graphView.SetReadOnlyMode(true);
            }
            catch
            {
                conversationData = null;
                isValidConversation = false;
            }
        }
        #endregion
        #region Drawing
        private void DrawHeader()
        {
            var importer = target as ConversationAssetImporter;
            EditorGUILayout.LabelField(new GUIContent("Conversation Graph Preview", "Read-only graph preview for the selected conversation file."), EditorStyles.boldLabel);
            if (importer == null) return;
            EditorGUILayout.LabelField(new GUIContent(Path.GetFileName(importer.assetPath), "Selected conversation file."), EditorStyles.miniLabel);
            EditorGUILayout.Space();
        }
        private void DrawSummary()
        {
            int nodeCount = conversationData?.ConversationManager?.Nodes?.Count ?? 0;
            EditorGUILayout.LabelField(new GUIContent("Nodes", "Total number of nodes in this conversation."), nodeCount.ToString());
            EditorGUILayout.HelpBox("This preview is read-only. You can pan, zoom, and select nodes to inspect the graph safely.", MessageType.Info);
        }
        private void DrawGraphPreview()
        {
            Rect graphRect = GUILayoutUtility.GetRect(10f, graphPreviewHeight, GUILayout.ExpandWidth(true));
            if (shouldFrameGraph && Event.current.type != EventType.Layout)
            {
                graphView.FrameAllNodes(graphRect);
                shouldFrameGraph = false;
            }
            graphView.Draw(graphRect);
        }
        private void DrawActions()
        {
            var importer = target as ConversationAssetImporter;
            if (importer == null) return;
            EditorGUILayout.Space();
            if (GUILayout.Button(new GUIContent("Open Editor", "Open the selected conversation file in the full editor window."), GUILayout.Height(24f))) ConversationEditorWindow.OpenConversationFile(importer.assetPath);
        }
        private void DrawInvalidState()
        {
            EditorGUILayout.HelpBox("The selected file could not be loaded as a valid conversation asset.", MessageType.Warning);
        }
        #endregion
    }
}
