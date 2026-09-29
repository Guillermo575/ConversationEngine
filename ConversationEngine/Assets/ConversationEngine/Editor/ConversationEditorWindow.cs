using System.Collections.Generic;
using System.IO;
using ConversationEditor.Graph;
using ConversationEditor.Helper;
using ConversationEditor.JSON;
using ConversationEditor.Panel;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEditor
{
    /// <summary>
    /// Main editor window for visual conversation editing with node-based graph
    /// </summary>
    public class ConversationEditorWindow : EditorWindow
    {
        #region Core Data
        private ConversationEditorCore conversationEditorCore = ConversationEditorCore.GetSingleton();
        private ConversationData conversationData { set { conversationEditorCore.conversationData = value; } get { return conversationEditorCore.conversationData; } }
        private string currentFilePath { set { conversationEditorCore.currentFilePath = value; } get { return conversationEditorCore.currentFilePath; } }
        private bool isDirty { set { conversationEditorCore.isDirty = value; } get { return conversationEditorCore.isDirty; } }
        public ConversationGraphView graphView { get; private set; }
        private EditorProperties editorProperties;
        private EditorInspector editorInspector;
        #endregion

        #region Panel Sizes
        private float leftPanelWidth = 250f;
        private float rightPanelWidth = 300f;
        private bool isDraggingLeftSplitter = false;
        private bool isDraggingRightSplitter = false;
        private bool showInspector = false;
        #endregion

        #region Unity Menu Items
        [MenuItem("Window/ConversationEngine/Conversation Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConversationEditorWindow>("Conversation Editor");
            window.minSize = new Vector2(800, 600);
        }
        public static void OpenConversationFile(string filePath)
        {
            var window = GetWindow<ConversationEditorWindow>("Conversation Editor");
            window.LoadConversation(filePath);
            window.maximized = true;
            window.Show();
            window.Focus();
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
            if (graphView == null)
            {
                graphView = new ConversationGraphView(this, false);
                graphView.OnDirty += MarkDirty;
                graphView.OnSelectionChanged += SyncInspectorVisibilityFromGraph;
                graphView.OnRepaintRequested += Repaint;
            }
            if (editorProperties == null)
            {
                editorProperties = new EditorProperties(this);
                editorProperties.OnDirty += MarkDirty;
                editorProperties.OnResourceManagerVisibility += HideResourceManager;
            }
            if (editorInspector == null)
            {
                editorInspector = new EditorInspector(this, graphView);
                editorInspector.OnDirty += MarkDirty;
            }
            ApplyReadOnlyModeFromSettings();
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (graphView != null)
            {
                graphView.OnDirty -= MarkDirty;
                graphView.OnSelectionChanged -= SyncInspectorVisibilityFromGraph;
                graphView.OnRepaintRequested -= Repaint;
            }
            if (editorProperties != null)
            {
                editorProperties.OnDirty -= MarkDirty;
                editorProperties.OnResourceManagerVisibility -= HideResourceManager;
            }
            if (editorInspector != null)
            {
                editorInspector.OnDirty -= MarkDirty;
            }
        }
        private void OnDestroy()
        {
            if (!isDirty || IsReadOnlyMode()) return;
            if (EditorUtility.DisplayDialog("Unsaved Changes", "You have unsaved changes. Do you want to save them?", "Save", "Don't Save")) SaveConversation();
        }
        private void OnUndoRedo()
        {
            Repaint();
        }
        private void OnGUI()
        {
            HandleKeyboardShortcuts();
            DrawToolbar();
            if (conversationData == null)
            {
                EditorGUILayout.HelpBox("No conversation file loaded. Create a new one or open an existing file.", MessageType.Info);
                return;
            }
            DrawThreePanelLayout();
        }
        #endregion

        #region UI Layout
        private void DrawThreePanelLayout()
        {
            float toolbarHeight = 40f;
            float totalWidth = position.width;
            float totalHeight = position.height - toolbarHeight;
            bool isResourcePanelVisible = IsResourceManagerVisible();
            float centerPanelX = 0f;
            float centerPanelWidth = totalWidth;
            if (isResourcePanelVisible)
            {
                Rect leftPanelRect = new Rect(0f, toolbarHeight, leftPanelWidth, totalHeight);
                GUILayout.BeginArea(leftPanelRect);
                DrawResourceManager();
                GUILayout.EndArea();
                Rect leftSplitterRect = new Rect(leftPanelWidth, toolbarHeight, 5f, totalHeight);
                DrawSplitter(leftSplitterRect, ref isDraggingLeftSplitter, ref leftPanelWidth, 150f, totalWidth * 0.5f);
                centerPanelX = leftPanelWidth + 5f;
                centerPanelWidth -= leftPanelWidth + 5f;
            }
            if (showInspector) centerPanelWidth -= rightPanelWidth + 5f;
            if (!isResourcePanelVisible)
            {
                Rect showButtonRect = new Rect(10f, toolbarHeight + 6f, 130f, 22f);
                if (GUI.Button(showButtonRect, "Show properties"))
                {
                    SetResourceManagerVisibility(true);
                    Event.current.Use();
                }
            }
            Rect centerPanelRect = new Rect(centerPanelX, toolbarHeight, centerPanelWidth, totalHeight);
            GUILayout.BeginArea(centerPanelRect);
            DrawConversationGraph();
            GUILayout.EndArea();
            if (!showInspector) return;
            float rightSplitterX = centerPanelX + centerPanelWidth;
            Rect rightSplitterRect = new Rect(rightSplitterX, toolbarHeight, 5f, totalHeight);
            DrawSplitter(rightSplitterRect, ref isDraggingRightSplitter, ref rightPanelWidth, 200f, totalWidth * 0.5f);
            Rect rightPanelRect = new Rect(rightSplitterX + 5f, toolbarHeight, rightPanelWidth, totalHeight);
            GUILayout.BeginArea(rightPanelRect);
            editorInspector.DrawInspectorPanel();
            GUILayout.EndArea();
        }
        private void DrawSplitter(Rect splitterRect, ref bool isDragging, ref float panelWidth, float minWidth, float maxWidth)
        {
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            GUI.Box(splitterRect, "", EditorStyles.toolbar);
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && splitterRect.Contains(e.mousePosition))
            {
                isDragging = true;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0 && isDragging)
            {
                isDragging = false;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDragging)
            {
                panelWidth += e.delta.x;
                panelWidth = Mathf.Clamp(panelWidth, minWidth, maxWidth);
                e.Use();
                Repaint();
            }
        }
        #endregion

        #region Input Handling
        private void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            if (e.type != EventType.KeyDown) return;
            if (e.control && e.keyCode == KeyCode.S)
            {
                if (!IsReadOnlyMode()) SaveConversation();
                e.Use();
                return;
            }
            if (e.control && e.keyCode == KeyCode.N)
            {
                if (!IsReadOnlyMode()) CreateNewConversation();
                e.Use();
                return;
            }
            if (e.control && e.keyCode == KeyCode.Z)
            {
                PerformUndo();
                e.Use();
                return;
            }
            if ((e.control && e.keyCode == KeyCode.Y) || (e.control && e.shift && e.keyCode == KeyCode.Z))
            {
                PerformRedo();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.Delete && graphView?.SelectedNode != null)
            {
                if (!IsReadOnlyMode()) graphView.DeleteSelectedNode();
                e.Use();
                return;
            }
            if (e.keyCode == KeyCode.F && graphView?.SelectedNode != null)
            {
                graphView.FrameSelectedNode();
                e.Use();
                return;
            }
            if (e.keyCode != KeyCode.Escape) return;
            graphView?.HandleEscapeAction();
            showInspector = graphView != null && graphView.HasSelection;
            e.Use();
            Repaint();
        }
        #endregion

        #region Toolbar Drawing
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawFileMenuButton();
            DrawAdjustMenuButton();
            DrawViewMenuButton();
            GUILayout.Space(8f);
            GUI.enabled = conversationData != null && !IsReadOnlyMode() && conversationEditorCore.CanUndo();
            if (GUILayout.Button("Undo Ctrl+Z", EditorStyles.toolbarButton, GUILayout.Width(90f))) PerformUndo();
            GUI.enabled = conversationData != null && !IsReadOnlyMode() && conversationEditorCore.CanRedo();
            if (GUILayout.Button("Redo Ctrl+Y", EditorStyles.toolbarButton, GUILayout.Width(90f))) PerformRedo();
            GUI.enabled = conversationData != null;
            bool readOnly = IsReadOnlyMode();
            if (GUILayout.Button(readOnly ? "Unlock" : "Lock", EditorStyles.toolbarButton, GUILayout.Width(60f))) ToggleReadOnlyMode();
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (conversationData != null) GUILayout.Label(string.IsNullOrEmpty(currentFilePath) ? "Untitled" : Path.GetFileName(currentFilePath), EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFileMenuButton()
        {
            GUI.enabled = conversationData != null || !IsReadOnlyMode();
            if (GUILayout.Button("File", EditorStyles.toolbarDropDown, GUILayout.Width(60f)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("New\tCtrl+N"), false, () => { if (!IsReadOnlyMode()) CreateNewConversation(); });
                menu.AddItem(new GUIContent("Open"), false, OpenConversationDialog);
                if (conversationData != null && !IsReadOnlyMode())
                {
                    menu.AddItem(new GUIContent("Save\tCtrl+S"), false, () => SaveConversation());
                    menu.AddItem(new GUIContent("Save As"), false, () => SaveConversationAs());
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Save\tCtrl+S"));
                    menu.AddDisabledItem(new GUIContent("Save As"));
                }
                menu.ShowAsContext();
            }
            GUI.enabled = true;
        }

        private void DrawAdjustMenuButton()
        {
            GUI.enabled = conversationData != null && !IsReadOnlyMode();
            if (GUILayout.Button("Adjust", EditorStyles.toolbarDropDown, GUILayout.Width(70f)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Horizontal"), false, () => graphView?.AutoLayoutNodesFromToolbar(true));
                menu.AddItem(new GUIContent("Vertical"), false, () => graphView?.AutoLayoutNodesFromToolbar(false));
                menu.ShowAsContext();
            }
            GUI.enabled = true;
        }

        private void DrawViewMenuButton()
        {
            GUI.enabled = conversationData != null;
            if (GUILayout.Button("View", EditorStyles.toolbarDropDown, GUILayout.Width(60f)))
            {
                GenericMenu menu = new GenericMenu();
                bool isVisible = IsResourceManagerVisible();
                menu.AddItem(new GUIContent(isVisible ? "Hide ResourceManager" : "Show ResourceManager"), false, () => SetResourceManagerVisibility(!isVisible));
                menu.AddItem(new GUIContent("Reset Config"), false, ResetConfigurationValues);
                menu.ShowAsContext();
            }
            GUI.enabled = true;
        }
        #endregion

        #region Graph Drawing
        private void DrawConversationGraph()
        {
            if (graphView == null) return;
            graphView.SetReadOnlyMode(IsReadOnlyMode());
            graphView.Draw();
        }
        #endregion

        #region Property Drawing
        public void DrawResourceManager()
        {
            if (editorProperties == null) return;
            editorProperties.DrawResourceManager();
        }
        private void HideResourceManager()
        {
            if (editorProperties == null) return;
            SetResourceManagerVisibility(false);
        }
        #endregion

        #region Drawing Tools
        private void OpenConversationDialog()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Open Conversation", "Assets", new string[] { "Conversation Files", "conversation,json", "All Files", "*" });
            if (!string.IsNullOrEmpty(path)) LoadConversation(path);
        }

        private void EnsureEditorSettings()
        {
            if (conversationData == null) return;
            if (conversationData.EditorSettings == null) conversationData.EditorSettings = new ConversationEditorSettings();
        }

        private bool IsResourceManagerVisible()
        {
            if (conversationData == null) return true;
            EnsureEditorSettings();
            return !conversationData.EditorSettings.IsResourcePanelHidden;
        }

        private void SetResourceManagerVisibility(bool isVisible)
        {
            if (conversationData == null) return;
            EnsureEditorSettings();
            conversationData.EditorSettings.IsResourcePanelHidden = !isVisible;
            Repaint();
        }

        private void CreateNewConversation()
        {
            if (IsReadOnlyMode()) return;
            conversationData = new ConversationData();
            EnsureEditorSettings();
            conversationData.ConversationManager = new ConversationManager();
            var startNode = new ConversationNode { Id = 1, NodeType = ConversationNodeType.Start, NextNodeId = 0, EditorPosition = new Vector2(0, 0), EditorSize = new Vector2(150, 80) };
            var endNode = new ConversationNode { Id = 2, NodeType = ConversationNodeType.End, NextNodeId = 0, EditorPosition = new Vector2(400, 0), EditorSize = new Vector2(150, 80) };
            conversationData.ConversationManager.Nodes.Add(startNode);
            conversationData.ConversationManager.Nodes.Add(endNode);
            graphView?.SetConversationData(conversationData);
            currentFilePath = null;
            isDirty = false;
            showInspector = false;
            conversationEditorCore.ClearHistory();
            ApplyReadOnlyModeFromSettings();
            Repaint();
        }
        #endregion

        #region File Operations
        private void LoadConversation(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return;
            string json = File.ReadAllText(filePath);
            conversationData = ConversationJsonSettings.Deserialize<ConversationData>(json);
            if (conversationData == null)
            {
                EditorUtility.DisplayDialog("Error", "Failed to load conversation file.", "OK");
                return;
            }
            EnsureEditorSettings();
            NormalizeConversationConditionBooleanValues(conversationData);
            ConversationNodeUtility.EnsureStartNodeExists(conversationData);
            graphView?.SetConversationData(conversationData);
            currentFilePath = filePath;
            isDirty = false;
            showInspector = false;
            conversationEditorCore.ClearHistory();
            ApplyReadOnlyModeFromSettings();
            Repaint();
        }

        private void NormalizeConversationConditionBooleanValues(ConversationData data)
        {
            if (data?.ConversationManager?.Nodes == null) return;
            foreach (var node in data.ConversationManager.Nodes)
            {
                if (node.Options != null)
                {
                    foreach (var option in node.Options) NormalizeConditionList(option.Conditions);
                }
                if (node.conditionalBranch != null) NormalizeConditionList(node.conditionalBranch.Conditions);
            }
        }

        private void NormalizeConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            foreach (var condition in conditions)
            {
                if (condition == null || condition.ValueDataType != ValueType.Boolean) continue;
                condition.Value = ConversationEditorHelpers.NormalizeBooleanValue(condition.Value);
                condition.IsValueVariable = false;
            }
        }

        private void SaveConversation()
        {
            if (IsReadOnlyMode()) return;
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveConversationAs();
                return;
            }
            SaveToFile(currentFilePath);
        }

        private void SaveConversationAs()
        {
            if (IsReadOnlyMode()) return;
            string path = EditorUtility.SaveFilePanel("Save Conversation", "Assets", "conversation", "conversation");
            if (string.IsNullOrEmpty(path)) return;
            currentFilePath = path;
            SaveToFile(path);
        }

        private bool SaveToFile(string filePath)
        {
            if (conversationData == null || string.IsNullOrEmpty(filePath)) return false;
            try
            {
                string json = ConversationJsonSettings.Serialize(conversationData);
                File.WriteAllText(filePath, json);
                isDirty = false;
                AssetDatabase.Refresh();
                return true;
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save conversation: {ex.Message}", "OK");
                return false;
            }
        }

        public void RegisterUndoState(string actionName)
        {
            if (conversationData == null || IsReadOnlyMode()) return;
            conversationEditorCore.RegisterUndoSnapshot();
        }

        private void PerformUndo()
        {
            if (IsReadOnlyMode()) return;
            if (!conversationEditorCore.TryUndo()) return;
            graphView?.SetConversationData(conversationData);
            ApplyReadOnlyModeFromSettings();
            Repaint();
        }

        private void PerformRedo()
        {
            if (IsReadOnlyMode()) return;
            if (!conversationEditorCore.TryRedo()) return;
            graphView?.SetConversationData(conversationData);
            ApplyReadOnlyModeFromSettings();
            Repaint();
        }

        private bool IsReadOnlyMode()
        {
            if (conversationData?.EditorSettings == null) return false;
            return conversationData.EditorSettings.IsReadOnly;
        }

        private void ApplyReadOnlyModeFromSettings()
        {
            bool readOnly = IsReadOnlyMode();
            graphView?.SetReadOnlyMode(readOnly);
            editorInspector?.SetReadOnlyMode(readOnly);
            editorProperties?.SetReadOnlyMode(readOnly);
        }

        private void ToggleReadOnlyMode()
        {
            if (conversationData == null) return;
            if (!TrySaveBeforeReadOnlyToggle()) return;
            conversationData.EditorSettings.IsReadOnly = !conversationData.EditorSettings.IsReadOnly;
            ApplyReadOnlyModeFromSettings();
            SaveToFile(currentFilePath);
            Repaint();
        }

        private bool TrySaveBeforeReadOnlyToggle()
        {
            if (conversationData == null) return false;
            bool confirmSave = EditorUtility.DisplayDialog("Save Changes", "Do you want to save before changing read-only mode?", "Save", "Cancel");
            if (!confirmSave) return false;
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveConversationAs();
            }
            else
            {
                SaveConversation();
            }
            if (string.IsNullOrEmpty(currentFilePath)) return false;
            return SaveToFile(currentFilePath);
        }

        private void ResetConfigurationValues()
        {
            if (conversationData == null) return;
            RegisterUndoState("Reset Config");
            EnsureEditorSettings();
            conversationData.EditorSettings.Zoom = 1f;
            conversationData.EditorSettings.CameraPosition = Vector2.zero;
            conversationData.EditorSettings.IsResourcePanelHidden = false;
            conversationData.EditorSettings.IsReadOnly = false;
            leftPanelWidth = 250f;
            rightPanelWidth = 300f;
            showInspector = false;
            graphView?.SetConversationData(conversationData);
            ApplyReadOnlyModeFromSettings();
            MarkDirty();
            Repaint();
        }

        private void SyncInspectorVisibilityFromGraph()
        {
            showInspector = graphView != null && graphView.HasSelection;
        }

        private void MarkDirty()
        {
            if (conversationData == null || IsReadOnlyMode()) return;
            isDirty = true;
        }
        #endregion
    }
}