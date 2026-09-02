using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private ConversationData conversationData;
        private string currentFilePath;
        private bool isDirty = false;
        private ConversationGraphView graphView;
        #endregion

        #region View State
        private Vector2 panOffset = Vector2.zero;
        private float zoom = 1.0f;
        private const float minZoom = 0.1f;
        private const float maxZoom = 5.0f;
        private Rect currentGraphRect;
        #endregion

        #region Selection and Interaction
        private ConversationNode selectedNode;
        private ConversationOption selectedOption;
        private ConditionalBranch selectedBranch;
        private Vector2 dragStartPos;
        private bool isDraggingView = false;
        private bool isDraggingNode = false;
        private bool isConnecting = false;
        private ConversationNode connectingFromNode;
        private ConversationOption connectingFromOption;
        private ConditionalBranch connectingFromBranch;
        private int connectingBranchIndex = 0;
        private ConversationNode draggedNode = null;
        private bool isMouseOverNode = false;
        private bool isRightClickMenuActive = false;
        private bool isNodeBeingDragged = false;
        #endregion

        #region UI State
        private Vector2 resourceScrollPos;
        private Vector2 nodeScrollPos;
        private Vector2 inspectorScrollPos;
        #endregion

        #region Panel Sizes
        private float leftPanelWidth = 250f;
        private float rightPanelWidth = 300f;
        private bool isDraggingLeftSplitter = false;
        private bool isDraggingRightSplitter = false;
        private bool showInspector = false;
        #endregion

        #region Auto-layout
        private float autoLayoutSpacing = 250f;
        private float autoLayoutVerticalSpacing = 150f;
        #endregion

        #region Context Menu
        private Vector2 contextMenuPosition;
        #endregion

        #region Styles
        private GUIStyle nodeStyle;
        private GUIStyle nodeSelectedStyle;
        private GUIStyle nodeDraggingStyle;
        private GUIStyle startNodeStyle;
        private GUIStyle startNodeSelectedStyle;
        private GUIStyle startNodeDraggingStyle;
        private GUIStyle endNodeStyle;
        private GUIStyle endNodeSelectedStyle;
        private GUIStyle endNodeDraggingStyle;
        private GUIStyle functionNodeStyle;
        private GUIStyle functionNodeSelectedStyle;
        private GUIStyle functionNodeDraggingStyle;
        private GUIStyle optionNodeStyle;
        private GUIStyle conditionalNodeStyle;
        private GUIStyle conditionalNodeSelectedStyle;
        private GUIStyle conditionalNodeDraggingStyle;
        private GUIStyle nodeHeaderStyle;
        private GUIStyle nodeBodyTextStyle;
        private GUIStyle nodeActorTextStyle;
        private bool stylesInitialized = false;
        #endregion

        #region Grid Constants
        private const float gridSpacing = 20f;
        private Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        #endregion

        #region Zoom Controls
        private const float zoomControlScale = 1.5f;
        private const int minNodeFontSize = 8;
        private const int nodeHeaderBaseFontSize = 11;
        private const int nodeBodyBaseFontSize = 12;
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
        }
        private void OnDestroy()
        {
            if (isDirty)
            {
                if (EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Do you want to save them?",
                    "Save", "Don't Save"))
                {
                    SaveConversation();
                }
            }
        }
        private void OnUndoRedo()
        {
            isDirty = true;
            Repaint();
        }
        private void OnGUI()
        {
            InitializeStyles();
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

        #region Style Initialization
        private void InitializeStyles()
        {
            if (stylesInitialized) return;
            int borderWidth = 4;
            nodeStyle = new GUIStyle("box");
            nodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), Color.black, borderWidth);
            nodeStyle.border = new RectOffset(borderWidth, borderWidth, borderWidth, borderWidth);
            nodeStyle.padding = new RectOffset(borderWidth + 4, borderWidth + 4, borderWidth + 4, borderWidth + 4);
            nodeStyle.alignment = TextAnchor.UpperLeft;
            nodeStyle.wordWrap = true;
            nodeSelectedStyle = new GUIStyle(nodeStyle);
            nodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            nodeDraggingStyle = new GUIStyle(nodeStyle);
            nodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f), Color.white, borderWidth);
            startNodeStyle = new GUIStyle(nodeStyle);
            startNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), Color.black, borderWidth);
            startNodeStyle.alignment = TextAnchor.MiddleCenter;
            startNodeStyle.fontSize = 16;
            startNodeStyle.fontStyle = FontStyle.Bold;
            startNodeSelectedStyle = new GUIStyle(startNodeStyle);
            startNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            startNodeDraggingStyle = new GUIStyle(startNodeStyle);
            startNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f), Color.white, borderWidth);
            endNodeStyle = new GUIStyle(nodeStyle);
            endNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.black, borderWidth);
            endNodeStyle.alignment = TextAnchor.MiddleCenter;
            endNodeStyle.fontSize = 16;
            endNodeStyle.fontStyle = FontStyle.Bold;
            endNodeSelectedStyle = new GUIStyle(endNodeStyle);
            endNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            endNodeDraggingStyle = new GUIStyle(endNodeStyle);
            endNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.white, borderWidth);
            functionNodeStyle = new GUIStyle(nodeStyle);
            functionNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), Color.black, borderWidth);
            functionNodeSelectedStyle = new GUIStyle(functionNodeStyle);
            functionNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            functionNodeDraggingStyle = new GUIStyle(functionNodeStyle);
            functionNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.6f, 0.9f), Color.white, borderWidth);
            optionNodeStyle = new GUIStyle(nodeStyle);
            optionNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.4f, 0.7f, 0.9f, 0.9f), Color.black, borderWidth);
            conditionalNodeStyle = new GUIStyle(nodeStyle);
            conditionalNodeStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), Color.black, borderWidth);
            conditionalNodeSelectedStyle = new GUIStyle(conditionalNodeStyle);
            conditionalNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
            conditionalNodeDraggingStyle = new GUIStyle(conditionalNodeStyle);
            conditionalNodeDraggingStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f), Color.white, borderWidth);
            nodeHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            nodeHeaderStyle.padding = new RectOffset(borderWidth + 4, borderWidth + 4, borderWidth + 4, borderWidth + 4);
            nodeHeaderStyle.fontSize = nodeHeaderBaseFontSize;
            nodeHeaderStyle.normal.textColor = Color.white;
            nodeHeaderStyle.fontStyle = FontStyle.Bold;
            nodeBodyTextStyle = new GUIStyle(EditorStyles.wordWrappedLabel);
            nodeBodyTextStyle.fontStyle = FontStyle.Bold;
            nodeBodyTextStyle.fontSize = nodeBodyBaseFontSize;
            nodeBodyTextStyle.normal.textColor = Color.white;
            nodeActorTextStyle = new GUIStyle(EditorStyles.label);
            nodeActorTextStyle.wordWrap = true;
            nodeActorTextStyle.fontSize = nodeBodyBaseFontSize;
            nodeActorTextStyle.normal.textColor = Color.white;
            stylesInitialized = true;
        }
        private int GetScaledNodeFontSize(int baseFontSize)
        {
            return Mathf.Max(minNodeFontSize, Mathf.RoundToInt(baseFontSize * zoom));
        }
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
        private Texture2D MakeTextureWithBorder(int width, int height, Color fillColor, Color borderColor, int borderWidth)
        {
            int totalWidth = width + borderWidth * 2;
            int totalHeight = height + borderWidth * 2;
            Color[] pixels = new Color[totalWidth * totalHeight];
            for (int y = 0; y < totalHeight; y++)
            {
                for (int x = 0; x < totalWidth; x++)
                {
                    if (x < borderWidth || x >= totalWidth - borderWidth ||
                        y < borderWidth || y >= totalHeight - borderWidth)
                    {
                        pixels[y * totalWidth + x] = borderColor;
                    }
                    else
                    {
                        pixels[y * totalWidth + x] = fillColor;
                    }
                }
            }
            Texture2D texture = new Texture2D(totalWidth, totalHeight, TextureFormat.RGBA32, false);
            texture.SetPixels(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply();
            return texture;
        }
        #endregion

        #region UI Layout
        private void DrawThreePanelLayout()
        {
            float toolbarHeight = 40f;
            float totalWidth = position.width;
            float totalHeight = position.height - toolbarHeight;
            Rect leftPanelRect = new Rect(0, toolbarHeight, leftPanelWidth, totalHeight);
            GUILayout.BeginArea(leftPanelRect);
            DrawResourceManager();
            GUILayout.EndArea();
            Rect leftSplitterRect = new Rect(leftPanelWidth, toolbarHeight, 5, totalHeight);
            DrawSplitter(leftSplitterRect, ref isDraggingLeftSplitter, ref leftPanelWidth, 150f, totalWidth * 0.5f);
            float centerWidth = showInspector ? totalWidth - leftPanelWidth - rightPanelWidth - 10 : totalWidth - leftPanelWidth - 5;
            Rect centerPanelRect = new Rect(leftPanelWidth + 5, toolbarHeight, centerWidth, totalHeight);
            GUILayout.BeginArea(centerPanelRect);
            DrawConversationGraph();
            GUILayout.EndArea();
            if (showInspector)
            {
                float rightSplitterX = leftPanelWidth + 5 + centerWidth;
                Rect rightSplitterRect = new Rect(rightSplitterX, toolbarHeight, 5, totalHeight);
                DrawSplitter(rightSplitterRect, ref isDraggingRightSplitter, ref rightPanelWidth, 200f, totalWidth * 0.5f);
                Rect rightPanelRect = new Rect(rightSplitterX + 5, toolbarHeight, rightPanelWidth, totalHeight);
                GUILayout.BeginArea(rightPanelRect);
                DrawInspectorPanel();
                GUILayout.EndArea();
            }
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
            if (e.type == EventType.KeyDown)
            {
                if (e.control && e.keyCode == KeyCode.S)
                {
                    SaveConversation();
                    e.Use();
                }
                else if (e.control && e.keyCode == KeyCode.N)
                {
                    CreateNewConversation();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Delete && graphView?.SelectedNode != null)
                {
                    graphView.DeleteSelectedNode();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.F && graphView?.SelectedNode != null)
                {
                    graphView.FrameSelectedNode();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    graphView?.HandleEscapeAction();
                    showInspector = graphView != null && graphView.HasSelection;
                    e.Use();
                    Repaint();
                }
            }
        }
        #endregion

        #region Toolbar Drawing
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50))) CreateNewConversation();
            if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50))) OpenConversationDialog();
            GUI.enabled = conversationData != null;
            if (GUILayout.Button(isDirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(50))) SaveConversation();
            if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(60))) SaveConversationAs();
            GUILayout.Space(10);
            if (GUILayout.Button("Auto-Layout", EditorStyles.toolbarButton, GUILayout.Width(80))) graphView?.ShowAutoLayoutMenu();
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            if (conversationData != null)
            {
                GUILayout.Label(string.IsNullOrEmpty(currentFilePath) ? "Untitled" : Path.GetFileName(currentFilePath), EditorStyles.toolbarButton);
            }
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Resource Manager
        private void DrawResourceManager()
        {
            if (conversationData?.ResourceManager == null) return;
            resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Backgrounds", EditorStyles.boldLabel);
            DrawResourceList(conversationData.ResourceManager.SceneBackgrounds, "Background");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Audio Backgrounds", EditorStyles.boldLabel);
            DrawAudioBackgroundList(conversationData.ResourceManager.AudioBackgrounds);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actors", EditorStyles.boldLabel);
            DrawActorList(conversationData.ResourceManager.Actors);
            EditorGUILayout.EndScrollView();
        }
        private void DrawResourceList<T>(List<T> resources, string typeName) where T : Resource, new()
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < resources.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                resources[i].Id = EditorGUILayout.TextField("ID", resources[i].Id);
                resources[i].Path = EditorGUILayout.TextField("Path", resources[i].Path);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(this, "Remove Resource");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button($"Add {typeName}"))
            {
                Undo.RecordObject(this, $"Add {typeName}");
                resources.Add(new T());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }
        private void DrawAudioBackgroundList(List<AudioBackground> resources)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < resources.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                resources[i].Id = EditorGUILayout.TextField("ID", resources[i].Id);
                resources[i].Path = EditorGUILayout.TextField("Path", resources[i].Path);
                resources[i].AudioType = (AudioChannelType)EditorGUILayout.EnumPopup("Audio Type", resources[i].AudioType);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(this, "Remove Audio");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Audio Background"))
            {
                Undo.RecordObject(this, "Add Audio Background");
                resources.Add(new AudioBackground());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }
        private void DrawActorList(List<Actor> actors)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < actors.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                actors[i].Id = EditorGUILayout.TextField("ID", actors[i].Id);
                actors[i].Path = EditorGUILayout.TextField("Actor JSON Path", actors[i].Path);
                actors[i].IconPath = EditorGUILayout.TextField("Icon Path", actors[i].IconPath);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(this, "Remove Actor");
                    actors.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Actor"))
            {
                Undo.RecordObject(this, "Add Actor");
                actors.Add(new Actor());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }
        #endregion

        #region Graph Drawing
        private void DrawConversationGraph()
        {
            if (graphView == null) return;
            graphView.SetReadOnlyMode(false);
            graphView.Draw();
        }
        #endregion

        #region Inspector Panel
        private void DrawInspectorPanel()
        {
            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);
            var graphSelectedNode = graphView?.SelectedNode;
            var graphSelectedOption = graphView?.SelectedOption;
            var graphSelectedBranch = graphView?.SelectedBranch;
            if (graphSelectedNode != null)
            {
                DrawNodeInspector(graphSelectedNode);
            }
            else if (graphSelectedOption != null)
            {
                DrawOptionInspector(graphSelectedOption);
            }
            else if (graphSelectedBranch != null)
            {
                DrawBranchInspector(graphSelectedBranch);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a node to edit its properties", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNodeInspector(ConversationNode node)
        {
            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            // Set label width to be proportional to inspector panel width
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f; // Fixed label width for consistency
            // ID (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("ID", node.Id, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            // Node Type (read-only for Start/End)
            EditorGUI.BeginDisabledGroup(node.NodeType == ConversationNodeType.Start || node.NodeType == ConversationNodeType.End);
            node.NodeType = (ConversationNodeType)EditorGUILayout.EnumPopup("Node Type", node.NodeType, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            // Allow Start node to edit NextNodeId too
            if (node.NodeType == ConversationNodeType.Start)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
                node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node);
            }
            else if (node.NodeType != ConversationNodeType.End)
            {
                // Speaker Actor
                if (conversationData.ResourceManager.Actors.Count > 0)
                {
                    var actorIds = conversationData.ResourceManager.Actors.Select(a => a.Id).ToList();
                    actorIds.Insert(0, "(None)");
                    int currentIndex = string.IsNullOrEmpty(node.SpeakerActorId) ? 0 : 
                        actorIds.IndexOf(node.SpeakerActorId);
                    if (currentIndex < 0) currentIndex = 0;

                    int newIndex = EditorGUILayout.Popup("Speaker Actor", currentIndex, actorIds.ToArray(), GUILayout.ExpandWidth(true));
                    node.SpeakerActorId = newIndex == 0 ? "" : actorIds[newIndex];
                }
                else
                {
                    node.SpeakerActorId = EditorGUILayout.TextField("Speaker Actor ID", node.SpeakerActorId, GUILayout.ExpandWidth(true));
                }
                // Text
                EditorGUILayout.LabelField("Text:");
                node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));
                // Next Node ID with dropdown
                node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node);
                // Options
                if (node.NodeType == ConversationNodeType.Dialogue)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);

                    if (node.Options == null)
                        node.Options = new List<ConversationOption>();

                    for (int i = 0; i < node.Options.Count; i++)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"Option {i + 1}", EditorStyles.boldLabel);
                        // Ensure the option has a valid Conditions list
                        if (node.Options[i].Conditions == null)
                            node.Options[i].Conditions = new List<ConditionRule>();
                        node.Options[i].Text = EditorGUILayout.TextField("Text", node.Options[i].Text ?? "", GUILayout.ExpandWidth(true));
                        node.Options[i].NextNodeId = DrawNodeIdDropdown("Next Node", node.Options[i].NextNodeId, node);
                        if (GUILayout.Button("Remove Option", GUILayout.ExpandWidth(true)))
                        {
                            Undo.RecordObject(this, "Remove Option");
                            node.Options.RemoveAt(i);
                            MarkDirty();
                            break;
                        }
                        EditorGUILayout.EndVertical();
                    }
                    if (GUILayout.Button("Add Option", GUILayout.ExpandWidth(true), GUILayout.Height(25)))
                    {
                        Undo.RecordObject(this, "Add Option");
                        var newOption = new ConversationOption
                        {
                            Text = "New Option",
                            NextNodeId = 0,
                            Conditions = new List<ConditionRule>()
                        };
                        node.Options.Add(newOption);
                        MarkDirty();
                    }
                }
                // Functions
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Functions", EditorStyles.boldLabel);
                if (node.Functions == null)
                    node.Functions = new List<ConversationFunction>();
                DrawFunctionList(node.Functions);
                // Conditional Branches
                if (node.NodeType == ConversationNodeType.Conditional)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Conditional Branches", EditorStyles.boldLabel);
                    if (node.ConditionalBranches == null)
                        node.ConditionalBranches = new List<ConditionalBranch>();
                    for (int i = 0; i < node.ConditionalBranches.Count; i++)
                    {
                        EditorGUILayout.BeginVertical("box");
                        EditorGUILayout.LabelField($"Branch {i + 1}");
                        var branch = node.ConditionalBranches[i];
                        branch.NextNodeIdTrue = DrawNodeIdDropdown("Next Node (True)", branch.NextNodeIdTrue, node);
                        branch.NextNodeIdFalse = DrawNodeIdDropdown("Next Node (False)", branch.NextNodeIdFalse, node);
                        // Conditions
                        DrawConditionList(branch.Conditions);
                        if (GUILayout.Button("Remove Branch"))
                        {
                            Undo.RecordObject(this, "Remove Branch");
                            node.ConditionalBranches.RemoveAt(i);
                            MarkDirty();
                            break;
                        }
                        EditorGUILayout.EndVertical();
                    }
                    if (GUILayout.Button("Add Branch", GUILayout.ExpandWidth(true), GUILayout.Height(25)))
                    {
                        Undo.RecordObject(this, "Add Branch");
                        var newBranch = new ConditionalBranch
                        {
                            Conditions = new List<ConditionRule>(),
                            NextNodeIdTrue = 0,
                            NextNodeIdFalse = 0
                        };
                        node.ConditionalBranches.Add(newBranch);
                        MarkDirty();
                    }
                    // Default Branch
                    node.DefaultBranchNodeId = DrawNodeIdDropdown("Default Branch Node", node.DefaultBranchNodeId, node);
                }
            }
            // Editor Position and Size
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Properties", EditorStyles.boldLabel);
            node.EditorPosition = EditorGUILayout.Vector2Field("Position", node.EditorPosition, GUILayout.ExpandWidth(true));
            node.EditorSize = EditorGUILayout.Vector2Field("Size", node.EditorSize, GUILayout.ExpandWidth(true));
            // Restore original label width
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawOptionInspector(ConversationOption option)
        {
            EditorGUILayout.LabelField("Option Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            option.Text = EditorGUILayout.TextField("Text", option.Text, GUILayout.ExpandWidth(true));
            option.NextNodeId = DrawNodeIdDropdown("Next Node", option.NextNodeId, graphView?.SelectedNode);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(option.Conditions);
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawBranchInspector(ConditionalBranch branch)
        {
            EditorGUILayout.LabelField("Branch Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            branch.NextNodeIdTrue = DrawNodeIdDropdown("Next Node (True)", branch.NextNodeIdTrue, graphView?.SelectedNode);
            branch.NextNodeIdFalse = DrawNodeIdDropdown("Next Node (False)", branch.NextNodeIdFalse, graphView?.SelectedNode);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(branch.Conditions);
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private int DrawNodeIdDropdown(string label, int currentNodeId, ConversationNode excludeNode)
        {
            // Build list of available nodes
            var nodeOptions = new List<string>();
            var nodeIds = new List<int>();
            // Add "NINGUNO" option
            nodeOptions.Add("NINGUNO");
            nodeIds.Add(0);
            // Add all valid target nodes
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                // Skip Start nodes and the node itself (but allow selecting End nodes)
                if (node.NodeType == ConversationNodeType.Start || node == excludeNode) continue;
                // Use helper to format node text
                nodeOptions.Add(ConversationEditorHelpers.GetNodeDropdownText(node));
                nodeIds.Add(node.Id);
            }

            // Find current selection
            int currentIndex = nodeIds.IndexOf(currentNodeId);
            if (currentIndex < 0) currentIndex = 0;
            // Draw dropdown
            int newIndex = EditorGUILayout.Popup(label, currentIndex, nodeOptions.ToArray());
            int newNodeId = nodeIds[newIndex];
            if (newNodeId != currentNodeId)
            {
                MarkDirty();
            }
            return newNodeId;
        }

        private void DrawConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var condition = conditions[i];
                EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);
                condition.VariableName = EditorGUILayout.TextField("Variable", condition.VariableName ?? "", GUILayout.ExpandWidth(true));
                condition.Operator = (ComparisonOperator)EditorGUILayout.EnumPopup("Operator", condition.Operator, GUILayout.ExpandWidth(true));
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup("Value Type", condition.ValueDataType, GUILayout.ExpandWidth(true));
                condition.Value = EditorGUILayout.TextField("Value", condition.Value ?? "", GUILayout.ExpandWidth(true));
                condition.IsValueVariable = EditorGUILayout.Toggle("Is Value Variable", condition.IsValueVariable);
                if (GUILayout.Button("Remove Condition", GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(this, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    break;
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Condition", GUILayout.ExpandWidth(true), GUILayout.Height(25)))
            {
                Undo.RecordObject(this, "Add Condition");
                var newCondition = new ConditionRule
                {
                    VariableName = "newVariable",
                    Operator = ComparisonOperator.Equal,
                    ValueDataType = ValueType.String,
                    Value = "",
                    IsValueVariable = false
                };
                conditions.Add(newCondition);
                MarkDirty();
            }
        }

        private void DrawFunctionList(List<ConversationFunction> functions)
        {
            if (functions == null) return;
            for (int i = 0; i < functions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var func = functions[i];
                // Ensure function has valid parameters dictionary
                if (func.Parameters == null)
                    func.Parameters = new Dictionary<string, string>();
                EditorGUILayout.LabelField($"Function {i + 1}", EditorStyles.boldLabel);
                // Predefined function dropdown
                string[] predefinedFunctions = ConversationFunctionLibrary.GetFunctionNames();
                int currentIndex = System.Array.IndexOf(predefinedFunctions, func.MethodName);
                if (currentIndex < 0) currentIndex = predefinedFunctions.Length - 1; // "Custom"
                int newIndex = EditorGUILayout.Popup("Function", currentIndex, predefinedFunctions, GUILayout.ExpandWidth(true));
                string selectedFunction = predefinedFunctions[newIndex];
                if (selectedFunction == "Custom")
                {
                    func.MethodName = EditorGUILayout.TextField("Method Name", func.MethodName ?? "", GUILayout.ExpandWidth(true));
                }
                else
                {
                    func.MethodName = selectedFunction;
                }
                var paramDef = ConversationFunctionLibrary.GetFunctionParameters(func.MethodName);
                if (paramDef != null && paramDef.Count > 0)
                {
                    EditorGUILayout.LabelField("Parameters:", EditorStyles.boldLabel);
                    foreach (var param in paramDef)
                    {
                        if (!func.Parameters.ContainsKey(param.Key))
                            func.Parameters[param.Key] = "";
                        func.Parameters[param.Key] = EditorGUILayout.TextField(param.Key, func.Parameters[param.Key], GUILayout.ExpandWidth(true));
                    }
                }
                else
                {
                    // Custom parameters
                    EditorGUILayout.LabelField("Parameters (key=value):", EditorStyles.boldLabel);
                    var keys = func.Parameters.Keys.ToList();
                    foreach (var key in keys)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string newValue = EditorGUILayout.TextField(key, func.Parameters[key] ?? "", GUILayout.ExpandWidth(true));
                        if (newValue != func.Parameters[key])
                        {
                            func.Parameters[key] = newValue;
                            MarkDirty();
                        }
                        if (GUILayout.Button("X", GUILayout.Width(25)))
                        {
                            func.Parameters.Remove(key);
                            MarkDirty();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("Add Parameter", GUILayout.ExpandWidth(true)))
                    {
                        func.Parameters["newParam"] = "";
                        MarkDirty();
                    }
                }
                func.Timestamp = EditorGUILayout.IntField("Timestamp", func.Timestamp, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Remove Function", GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(this, "Remove Function");
                    functions.RemoveAt(i);
                    MarkDirty();
                    break;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("Add Function", GUILayout.ExpandWidth(true), GUILayout.Height(25)))
            {
                Undo.RecordObject(this, "Add Function");
                var newFunction = new ConversationFunction
                {
                    MethodName = "Custom",
                    Parameters = new Dictionary<string, string>(),
                    Timestamp = 0
                };
                functions.Add(newFunction);
                MarkDirty();
            }
        }

        private void OpenConversationDialog()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("Open Conversation", "Assets",
                new string[] { "Conversation Files", "conversation,json", "All Files", "*" });
            if (!string.IsNullOrEmpty(path))
            {
                LoadConversation(path);
            }
        }

        private void EnsureEditorSettings()
        {
            if (conversationData == null) return;
            if (conversationData.EditorSettings == null)
                conversationData.EditorSettings = new ConversationEditorSettings();
        }

        private void CreateNewConversation()
        {
            conversationData = new ConversationData();
            EnsureEditorSettings();
            conversationData.ConversationManager = new ConversationManager();
            var startNode = new ConversationNode
            {
                Id = 1,
                NodeType = ConversationNodeType.Start,
                NextNodeId = 0,
                EditorPosition = new Vector2(0, 0),
                EditorSize = new Vector2(150, 80)
            };
            var endNode = new ConversationNode
            {
                Id = 2,
                NodeType = ConversationNodeType.End,
                NextNodeId = 0,
                EditorPosition = new Vector2(400, 0),
                EditorSize = new Vector2(150, 80)
            };
            conversationData.ConversationManager.Nodes.Add(startNode);
            conversationData.ConversationManager.Nodes.Add(endNode);
            graphView?.SetConversationData(conversationData);
            currentFilePath = null;
            isDirty = false;
            selectedNode = null;
            selectedOption = null;
            selectedBranch = null;
            showInspector = false;
            panOffset = Vector2.zero;
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
            ConversationNodeUtility.EnsureStartNodeExists(conversationData);
            graphView?.SetConversationData(conversationData);
            currentFilePath = filePath;
            isDirty = false;
            selectedNode = null;
            selectedOption = null;
            selectedBranch = null;
            showInspector = false;
            panOffset = Vector2.zero;
            Repaint();
        }

        private void SaveConversation()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveConversationAs();
                return;
            }
            SaveToFile(currentFilePath);
        }

        private void SaveConversationAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Conversation", "Assets", "conversation", "conversation");
            if (string.IsNullOrEmpty(path)) return;
            currentFilePath = path;
            SaveToFile(path);
        }

        private void SaveToFile(string filePath)
        {
            if (conversationData == null) return;
            try
            {
                string json = ConversationJsonSettings.Serialize(conversationData);
                File.WriteAllText(filePath, json);
                isDirty = false;
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save conversation: {ex.Message}", "OK");
            }
        }

        private void SyncInspectorVisibilityFromGraph()
        {
            showInspector = graphView != null && graphView.HasSelection;
        }

        private void MarkDirty()
        {
            if (conversationData == null) return;
            isDirty = true;
        }
        #endregion
    }
}