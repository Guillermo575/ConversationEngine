using System.IO;
using System.Collections.Generic;
using ConversationEditorActor.Graph;
using ConversationEditorActor.Helper;
using ConversationEditorActor.Panel;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEditorActor
{
    public class ActorEditorWindow : EditorWindow
    {
        #region State
        private Actor actorData;
        private string currentAssetPath = "";
        private bool isDirty;
        private float leftPanelWidth = 360f;
        private bool resizingSplitter;
        private int selectedBodyPartId = 1;
        private HashSet<int> hiddenBodyPartIds = new HashSet<int>();
        private ActorEditorPanel actorEditorPanel;
        private ActorEditorGraph actorEditorGraph;
        #endregion

        #region Open
        [MenuItem("Window/ConversationEngine/Actor Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<ActorEditorWindow>("Actor Editor");
            window.minSize = new Vector2(980f, 560f);
            window.Show();
        }
        public static void OpenActorFile(string assetPath)
        {
            var window = GetWindow<ActorEditorWindow>("Actor Editor");
            window.minSize = new Vector2(980f, 560f);
            if (!window.ConfirmProceedWithUnsavedChanges()) return;
            window.LoadFromAssetPath(assetPath);
            window.Show();
            window.Focus();
        }
        #endregion

        #region Unity
        private void OnEnable()
        {
            if (actorEditorPanel == null) actorEditorPanel = new ActorEditorPanel();
            if (actorEditorGraph == null) actorEditorGraph = new ActorEditorGraph();
            if (actorData == null) actorData = ActorEditorHelpers.CreateDefaultActor();
            if (hiddenBodyPartIds == null) hiddenBodyPartIds = new HashSet<int>();
            if (selectedBodyPartId == 0 && actorData.BodyParts != null && actorData.BodyParts.Count > 0) selectedBodyPartId = actorData.BodyParts[0].Id;
            SyncHiddenBodyPartIds();
            saveChangesMessage = "Do you want to save changes to the current actor file?";
            UpdateWindowDirtyState();
        }
        private void OnGUI()
        {
            DrawToolbar();
            DrawLayout();
        }
        #endregion

        #region Draw
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(new GUIContent("New", "Create a new actor file in memory."), EditorStyles.toolbarButton, GUILayout.Width(56f))) CreateNewActor();
            if (GUILayout.Button(new GUIContent("Open", "Open an existing .actor file."), EditorStyles.toolbarButton, GUILayout.Width(56f))) OpenActorFromDialog();
            if (GUILayout.Button(new GUIContent("Save", "Save current actor file."), EditorStyles.toolbarButton, GUILayout.Width(56f))) SaveActor(false);
            if (GUILayout.Button(new GUIContent("Save As", "Save actor file using a new file path."), EditorStyles.toolbarButton, GUILayout.Width(62f))) SaveActor(true);
            GUILayout.Space(12f);
            string fileLabel = string.IsNullOrEmpty(currentAssetPath) ? "(unsaved)" : currentAssetPath;
            EditorGUILayout.LabelField(new GUIContent(fileLabel, "Current actor file path."), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            string stateLabel = isDirty ? "Modified" : "Saved";
            EditorGUILayout.LabelField(new GUIContent(stateLabel, "Save state."), EditorStyles.miniBoldLabel, GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();
        }
        private void DrawLayout()
        {
            Rect layoutRect = new Rect(0f, 20f, position.width, position.height - 20f);
            Rect leftRect = new Rect(layoutRect.x, layoutRect.y, leftPanelWidth, layoutRect.height);
            Rect splitterRect = new Rect(leftRect.xMax, layoutRect.y, 4f, layoutRect.height);
            Rect rightRect = new Rect(splitterRect.xMax, layoutRect.y, layoutRect.width - leftPanelWidth - splitterRect.width, layoutRect.height);
            GUILayout.BeginArea(leftRect);
            bool panelChanged = actorEditorPanel.Draw(actorData, ref selectedBodyPartId, hiddenBodyPartIds);
            GUILayout.EndArea();
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            EditorGUI.DrawRect(splitterRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            HandleSplitter(splitterRect, layoutRect);
            GUILayout.BeginArea(rightRect);
            bool graphChanged = actorEditorGraph.Draw(new Rect(0f, 0f, rightRect.width, rightRect.height), actorData, selectedBodyPartId, currentAssetPath, hiddenBodyPartIds);
            GUILayout.EndArea();
            if (panelChanged || graphChanged) MarkDirty();
        }
        private void HandleSplitter(Rect splitterRect, Rect layoutRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && splitterRect.Contains(currentEvent.mousePosition))
            {
                resizingSplitter = true;
                currentEvent.Use();
            }
            if (currentEvent.type == EventType.MouseDrag && resizingSplitter)
            {
                leftPanelWidth = Mathf.Clamp(currentEvent.mousePosition.x, 280f, layoutRect.width - 280f);
                Repaint();
            }
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0) resizingSplitter = false;
        }
        #endregion

        #region File Actions
        private void CreateNewActor()
        {
            if (!ConfirmProceedWithUnsavedChanges()) return;
            actorData = ActorEditorHelpers.CreateDefaultActor();
            currentAssetPath = "";
            hiddenBodyPartIds.Clear();
            isDirty = false;
            UpdateWindowDirtyState();
            selectedBodyPartId = actorData.BodyParts.Count > 0 ? actorData.BodyParts[0].Id : 0;
        }
        private void OpenActorFromDialog()
        {
            if (!ConfirmProceedWithUnsavedChanges()) return;
            string startPath = Application.dataPath;
            string absolutePath = EditorUtility.OpenFilePanel(new GUIContent("Open Actor File", "Choose an actor file.").text, startPath, "actor");
            if (string.IsNullOrEmpty(absolutePath)) return;
            LoadFromAbsolutePath(absolutePath);
        }
        private void LoadFromAssetPath(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return;
            string absolutePath = Path.GetFullPath(assetPath);
            LoadFromAbsolutePath(absolutePath);
        }
        private void LoadFromAbsolutePath(string absolutePath)
        {
            if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            {
                EditorUtility.DisplayDialog("Actor Editor", "The selected actor file does not exist.", "OK");
                return;
            }
            string json = File.ReadAllText(absolutePath);
            actorData = ActorEditorHelpers.DeserializeActor(json);
            currentAssetPath = ActorEditorHelpers.ToAssetPath(absolutePath);
            hiddenBodyPartIds.Clear();
            SyncHiddenBodyPartIds();
            isDirty = false;
            UpdateWindowDirtyState();
            selectedBodyPartId = actorData.BodyParts.Count > 0 ? actorData.BodyParts[0].Id : 0;
            Repaint();
        }
        private bool SaveActor(bool saveAs)
        {
            if (actorData == null) actorData = ActorEditorHelpers.CreateDefaultActor();
            string targetPath = currentAssetPath;
            if (saveAs || string.IsNullOrEmpty(targetPath))
            {
                string absolutePath = EditorUtility.SaveFilePanel(new GUIContent("Save Actor File", "Save actor data as .actor file.").text, Application.dataPath, "NewActor", "actor");
                if (string.IsNullOrEmpty(absolutePath)) return false;
                targetPath = ActorEditorHelpers.ToAssetPath(absolutePath);
                if (string.IsNullOrEmpty(targetPath))
                {
                    EditorUtility.DisplayDialog("Actor Editor", "Save location must be inside the Unity Assets folder.", "OK");
                    return false;
                }
            }
            string fullPath = Path.GetFullPath(targetPath);
            string json = ActorEditorHelpers.SerializeActor(actorData);
            File.WriteAllText(fullPath, json);
            AssetDatabase.Refresh();
            currentAssetPath = targetPath;
            isDirty = false;
            UpdateWindowDirtyState();
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(targetPath);
            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
            return true;
        }
        private bool ConfirmProceedWithUnsavedChanges()
        {
            if (!isDirty) return true;
            int result = EditorUtility.DisplayDialogComplex("Unsaved Changes", "There are unsaved actor changes.", "Save", "Cancel", "Discard");
            switch (result)
            {
                case 0:
                    return SaveActor(false);
                case 2:
                    return true;
                default:
                    return false;
            }
        }
        private void MarkDirty()
        {
            if (isDirty) return;
            isDirty = true;
            UpdateWindowDirtyState();
        }
        private void SyncHiddenBodyPartIds()
        {
            if (actorData?.BodyParts == null)
            {
                hiddenBodyPartIds.Clear();
                return;
            }
            HashSet<int> existingIds = new HashSet<int>();
            foreach (var bodyPart in actorData.BodyParts)
            {
                if (bodyPart == null) continue;
                existingIds.Add(bodyPart.Id);
            }
            List<int> idsToRemove = new List<int>();
            foreach (var hiddenId in hiddenBodyPartIds)
            {
                if (existingIds.Contains(hiddenId)) continue;
                idsToRemove.Add(hiddenId);
            }
            foreach (var hiddenId in idsToRemove) hiddenBodyPartIds.Remove(hiddenId);
        }
        public override void SaveChanges()
        {
            SaveActor(false);
        }
        public override void DiscardChanges()
        {
            isDirty = false;
            UpdateWindowDirtyState();
            base.DiscardChanges();
        }
        private void UpdateWindowDirtyState()
        {
            hasUnsavedChanges = isDirty;
            titleContent = new GUIContent(isDirty ? "Actor Editor*" : "Actor Editor", "Actor editor for .actor files.");
        }
        #endregion
    }
}
