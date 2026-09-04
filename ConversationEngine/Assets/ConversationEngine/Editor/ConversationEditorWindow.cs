using System.Collections.Generic;
using System.Globalization;
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

        #region Inspector Drafts
        private string selectedFunctionCategory = "Custom";
        private string selectedFunctionName = "";
        private string customFunctionName = "";
        private Dictionary<string, string> pendingFunctionParameters = new Dictionary<string, string>();
        private string pendingCustomParameterName = "";
        private string pendingCustomParameterValue = "";
        private int pendingFunctionTimestamp = 0;
        private readonly Dictionary<ConversationFunction, bool> functionParameterFoldouts = new Dictionary<ConversationFunction, bool>();
        private readonly Dictionary<ConversationNode, ConversationOption> pendingOptionsByNode = new Dictionary<ConversationNode, ConversationOption>();
        private readonly Dictionary<ConditionalBranch, ConditionRule> pendingConditionsByBranch = new Dictionary<ConditionalBranch, ConditionRule>();
        private static readonly ComparisonOperator[] comparisonOperatorValues =
        {
            ComparisonOperator.Equal,
            ComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterOrEqual,
            ComparisonOperator.LessThan,
            ComparisonOperator.LessOrEqual
        };
        private static readonly string[] comparisonOperatorLabels =
        {
            "Equal (=)",
            "NotEqual (!=)",
            "GreaterThan (>)",
            "GreaterOrEqual (>=)",
            "LessThan (<)",
            "LessOrEqual (<=)"
        };
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
        private const float optionDefaultWidth = 150f;
        private const float optionDefaultHeight = 60f;
        private const float optionDefaultSpacing = 10f;
        private const float minEditorNodeSize = 20f;
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
            functionNodeSelectedStyle.normal.background = MakeTextureWithBorder(2, 2, new Color(0.6f, 0.6f, 0.9f), new Color(1f, 0.84f, 0f, 1f), borderWidth);
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
            // Prefer showing option inspector when an option is selected (options also reference a parent node)
            if (graphSelectedOption != null)
            {
                DrawOptionInspector(graphSelectedOption);
            }
            else if (graphSelectedNode != null)
            {
                DrawNodeInspector(graphSelectedNode);
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
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(new GUIContent("ID", "Unique node identifier."), node.Id, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup(new GUIContent("Node Type", "Node behavior type (read-only)."), node.NodeType, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Connection", "Outgoing connection settings for start node."), EditorStyles.boldLabel);
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Target node for flow continuation.");
                    break;
                case ConversationNodeType.Conditional:
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Conditional Branches", "Condition-based outgoing branch settings."), EditorStyles.boldLabel);
                    DrawConditionalBranchSection(node);
                    break;
                case ConversationNodeType.End:
                    break;
                default:
                    DrawDialogueOrFunctionInspector(node);
                    break;
            }
            DrawSectionSeparator();
            EditorGUILayout.LabelField("Editor Properties", EditorStyles.boldLabel);
            node.EditorPosition = EditorGUILayout.Vector2Field(new GUIContent("Position", "Graph center position for this node."), node.EditorPosition, GUILayout.ExpandWidth(true));
            node.EditorSize = ClampEditorSize(EditorGUILayout.Vector2Field(new GUIContent("Size", "Graph size for this node. Minimum X/Y is 20."), node.EditorSize, GUILayout.ExpandWidth(true)));
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawDialogueOrFunctionInspector(ConversationNode node)
        {
            switch (node.NodeType)
            {
                case ConversationNodeType.Dialogue:
                    DrawSpeakerAndTextFields(node);
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Options", "Player options available from this dialogue node."), EditorStyles.boldLabel);
                    DrawOptionSection(node);
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Functions", "Timed functions executed while this node is active."), EditorStyles.boldLabel);
                    if (node.Functions == null) node.Functions = new List<ConversationFunction>();
                    DrawFunctionList(node.Functions);
                    return;
                case ConversationNodeType.Function:
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Functions", "Timed functions executed while this node is active."), EditorStyles.boldLabel);
                    if (node.Functions == null) node.Functions = new List<ConversationFunction>();
                    DrawFunctionList(node.Functions);
                    return;
                default:
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    return;
            }
        }

        private void DrawSpeakerAndTextFields(ConversationNode node)
        {
            if (conversationData.ResourceManager.Actors.Count > 0)
            {
                var actorIds = conversationData.ResourceManager.Actors.Select(a => a.Id).ToList();
                actorIds.Insert(0, "(None)");
                int currentIndex = string.IsNullOrEmpty(node.SpeakerActorId) ? 0 : actorIds.IndexOf(node.SpeakerActorId);
                if (currentIndex < 0) currentIndex = 0;
                int newIndex = EditorGUILayout.Popup(new GUIContent("Speaker Actor", "Actor speaking in this node."), currentIndex, actorIds.ToArray(), GUILayout.ExpandWidth(true));
                node.SpeakerActorId = newIndex == 0 ? "" : actorIds[newIndex];
            }
            else
            {
                node.SpeakerActorId = EditorGUILayout.TextField(new GUIContent("Speaker Actor ID", "Actor identifier for this node."), node.SpeakerActorId, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.LabelField(new GUIContent("Text", "Dialogue text shown to the player."));
            node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));
        }

        private void DrawOptionInspector(ConversationOption option)
        {
            EditorGUILayout.LabelField("Option Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            int selectedOptionIndex = graphView?.SelectedNode?.Options?.IndexOf(option) ?? 0;
            EnsureOptionEditorData(graphView?.SelectedNode, option, Mathf.Max(0, selectedOptionIndex));
            option.Text = EditorGUILayout.TextField(new GUIContent("Text", "Option text shown to the player."), option.Text, GUILayout.ExpandWidth(true));
            option.NextNodeId = DrawNodeIdDropdown("Next Node", option.NextNodeId, graphView?.SelectedNode, "Target node for this option.");
            option.EditorPosition = EditorGUILayout.Vector2Field(new GUIContent("Position", "Local graph position relative to the parent node."), option.EditorPosition, GUILayout.ExpandWidth(true));
            option.EditorSize = ClampEditorSize(EditorGUILayout.Vector2Field(new GUIContent("Size", "Graph size for this option node. Minimum X/Y is 20."), option.EditorSize, GUILayout.ExpandWidth(true)));
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(option.Conditions);
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawBranchInspector(ConditionalBranch branch)
        {
            EditorGUILayout.LabelField("Branch Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            branch.NextNodeIdTrue = DrawNodeIdDropdown("Next Node (True)", branch.NextNodeIdTrue, graphView?.SelectedNode, "Target node when branch evaluates true.");
            branch.NextNodeIdFalse = DrawNodeIdDropdown("Next Node (False)", branch.NextNodeIdFalse, graphView?.SelectedNode, "Target node when branch evaluates false.");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(branch.Conditions);
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private int DrawNodeIdDropdown(string label, int currentNodeId, ConversationNode excludeNode, string tooltip = "")
        {
            var nodeOptions = new List<string>();
            var nodeIds = new List<int>();
            nodeOptions.Add("NINGUNO");
            nodeIds.Add(0);
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.NodeType == ConversationNodeType.Start || node == excludeNode) continue;
                nodeOptions.Add(ConversationEditorHelpers.GetNodeDropdownText(node));
                nodeIds.Add(node.Id);
            }
            int currentIndex = nodeIds.IndexOf(currentNodeId);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), currentIndex, nodeOptions.ToArray());
            int newNodeId = nodeIds[newIndex];
            return newNodeId;
        }

        private int DrawNodeIdDropdownCompact(string label, int currentNodeId, ConversationNode excludeNode, string tooltip)
        {
            var nodeOptions = new List<string>();
            var nodeIds = new List<int>();
            nodeOptions.Add("NINGUNO");
            nodeIds.Add(0);
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.NodeType == ConversationNodeType.Start || node == excludeNode) continue;
                nodeOptions.Add(ConversationEditorHelpers.GetNodeDropdownText(node));
                nodeIds.Add(node.Id);
            }
            int currentIndex = nodeIds.IndexOf(currentNodeId);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), currentIndex, nodeOptions.ToArray(), GUILayout.ExpandWidth(true));
            return nodeIds[newIndex];
        }

        private void DrawOptionSection(ConversationNode node)
        {
            if (node.Options == null) node.Options = new List<ConversationOption>();
            if (!pendingOptionsByNode.TryGetValue(node, out var pendingOption))
            {
                pendingOption = new ConversationOption { Text = "", NextNodeId = 0, Conditions = new List<ConditionRule>() };
                pendingOptionsByNode[node] = pendingOption;
            }
            EditorGUILayout.BeginHorizontal();
            pendingOption.Text = EditorGUILayout.TextField(new GUIContent("", "New option text."), pendingOption.Text ?? "", GUILayout.ExpandWidth(true));
            pendingOption.NextNodeId = DrawNodeIdDropdownCompact("", pendingOption.NextNodeId, node, "Target node for the new option.");
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(new GUIContent("+", "Add option."), GUILayout.Width(28)))
            {
                if (string.IsNullOrWhiteSpace(pendingOption.Text)) EditorUtility.DisplayDialog("Invalid Option", "Option text cannot be empty.", "OK");
                else
                {
                    Undo.RecordObject(this, "Add Option");
                    node.Options.Add(CreateOptionForNode(node, pendingOption.Text.Trim(), pendingOption.NextNodeId, node.Options.Count));
                    pendingOption.Text = "";
                    pendingOption.NextNodeId = 0;
                    MarkDirty();
                }
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < node.Options.Count; i++)
            {
                if (node.Options[i].Conditions == null) node.Options[i].Conditions = new List<ConditionRule>();
                EnsureOptionEditorData(node, node.Options[i], i);
                EditorGUILayout.BeginHorizontal("box");
                node.Options[i].Text = EditorGUILayout.TextField(new GUIContent("", "Option text."), node.Options[i].Text ?? "", GUILayout.ExpandWidth(true));
                node.Options[i].NextNodeId = DrawNodeIdDropdownCompact("", node.Options[i].NextNodeId, node, "Target node for this option.");
                oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("X", "Remove this option."), GUILayout.Width(28)))
                {
                    Undo.RecordObject(this, "Remove Option");
                    node.Options.RemoveAt(i);
                    MarkDirty();
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private ConversationOption CreateOptionForNode(ConversationNode node, string text, int nextNodeId, int optionIndex)
        {
            return new ConversationOption
            {
                Text = text,
                NextNodeId = nextNodeId,
                Conditions = new List<ConditionRule>(),
                EditorPosition = GenerateOptionPosition(node, optionIndex),
                EditorSize = new Vector2(optionDefaultWidth, optionDefaultHeight)
            };
        }

        private void EnsureOptionEditorData(ConversationNode node, ConversationOption option, int optionIndex)
        {
            if (node == null || option == null) return;
            option.EditorSize = ClampEditorSize(option.EditorSize);
            if (option.EditorPosition == Vector2.zero) option.EditorPosition = GenerateOptionPosition(node, optionIndex);
        }

        private Vector2 ClampEditorSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(minEditorNodeSize, size.x), Mathf.Max(minEditorNodeSize, size.y));
        }

        private Vector2 GenerateOptionPosition(ConversationNode node, int optionIndex)
        {
            if (node == null) return new Vector2(optionDefaultWidth + optionDefaultSpacing, optionDefaultSpacing);
            float x = node.EditorSize.x + optionDefaultSpacing + Random.Range(10f, 45f);
            float y = (optionDefaultHeight + optionDefaultSpacing) * optionIndex + Random.Range(-20f, 20f);
            return new Vector2(x, y);
        }

        private void DrawConditionalBranchSection(ConversationNode node)
        {
            // Ensure singular conditionalBranch exists for conditional nodes
            if (node.conditionalBranch == null) node.conditionalBranch = new ConditionalBranch { Conditions = new List<ConditionRule>(), NextNodeIdTrue = 0, NextNodeIdFalse = 0 };
            var branch = node.conditionalBranch;
            if (branch.Conditions == null) branch.Conditions = new List<ConditionRule>();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Conditional Branch", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            branch.NextNodeIdTrue = DrawNodeIdDropdownCompact("Next Node (True)", branch.NextNodeIdTrue, node, "Target node when branch evaluates true.");
            branch.NextNodeIdFalse = DrawNodeIdDropdownCompact("Next Node (False)", branch.NextNodeIdFalse, node, "Target node when branch evaluates false.");
            EditorGUILayout.EndHorizontal();
            DrawConditionAddSection(branch);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawExistingConditionList(branch.Conditions);
            EditorGUILayout.EndVertical();
            node.DefaultBranchNodeId = DrawNodeIdDropdown("Default Branch Node", node.DefaultBranchNodeId, node, "Fallback target node when no branch matches.");
        }

        private void DrawConditionAddSection(ConditionalBranch branch)
        {
            if (!pendingConditionsByBranch.TryGetValue(branch, out var pendingCondition))
            {
                pendingCondition = new ConditionRule
                {
                    VariableName = "",
                    Operator = ComparisonOperator.Equal,
                    ValueDataType = ValueType.String,
                    Value = "",
                    IsValueVariable = false
                };
                pendingConditionsByBranch[branch] = pendingCondition;
            }
            EditorGUILayout.BeginVertical("box");
            pendingCondition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type expected for the condition value."), pendingCondition.ValueDataType, GUILayout.ExpandWidth(true));
            pendingCondition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name for this condition."), pendingCondition.VariableName ?? "", GUILayout.ExpandWidth(true));
            pendingCondition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), pendingCondition.Operator);
            DrawConditionValueRow(pendingCondition, false);
            if (GUILayout.Button(new GUIContent("Add", "Add this condition to the branch."), GUILayout.ExpandWidth(true)))
            {
                if (string.IsNullOrWhiteSpace(pendingCondition.VariableName)) EditorUtility.DisplayDialog("Invalid Condition", "Variable name cannot be empty.", "OK");
                else if (!IsConditionValueValid(pendingCondition)) EditorUtility.DisplayDialog("Invalid Condition", "Value does not match the selected value type.", "OK");
                else
                {
                    Undo.RecordObject(this, "Add Condition");
                    branch.Conditions.Add(new ConditionRule
                    {
                        VariableName = pendingCondition.VariableName.Trim(),
                        Operator = pendingCondition.Operator,
                        ValueDataType = pendingCondition.ValueDataType,
                        Value = pendingCondition.ValueDataType == ValueType.Boolean ? NormalizeBooleanValue(pendingCondition.Value) : (pendingCondition.Value ?? ""),
                        IsValueVariable = pendingCondition.ValueDataType == ValueType.Boolean ? false : pendingCondition.IsValueVariable
                    });
                    pendingCondition.Operator = ComparisonOperator.Equal;
                    pendingCondition.Value = pendingCondition.ValueDataType == ValueType.Boolean ? "true" : "";
                    pendingCondition.IsValueVariable = false;
                    MarkDirty();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawExistingConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(true);
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type used by this condition."), condition.ValueDataType, GUILayout.ExpandWidth(true));
                condition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name used by this condition."), condition.VariableName ?? "", GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
                condition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), condition.Operator);
                DrawConditionValueRow(condition, true);
                if (GUILayout.Button(new GUIContent("Remove", "Remove this condition."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(this, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConditionValueRow(ConditionRule condition, bool lockTypeSpecificFields)
        {
            if (condition.ValueDataType == ValueType.Boolean)
            {
                bool currentBool = ParseBooleanCondition(condition.Value);
                bool newBool = EditorGUILayout.ToggleLeft(new GUIContent("Is true", "Boolean value for this condition."), currentBool);
                condition.Value = newBool ? "true" : "false";
                condition.IsValueVariable = false;
                return;
            }
            EditorGUILayout.BeginHorizontal();
            condition.Value = EditorGUILayout.TextField(new GUIContent("Value", "Value to compare against."), condition.Value ?? "", GUILayout.ExpandWidth(true));
            bool previousGuiState = GUI.enabled;
            if (lockTypeSpecificFields) GUI.enabled = true;
            condition.IsValueVariable = EditorGUILayout.ToggleLeft(new GUIContent("variable", "Treat value as a variable name."), condition.IsValueVariable, GUILayout.Width(80));
            GUI.enabled = previousGuiState;
            EditorGUILayout.EndHorizontal();
        }

        private ComparisonOperator DrawComparisonOperatorDropdown(GUIContent label, ComparisonOperator value)
        {
            int index = System.Array.IndexOf(comparisonOperatorValues, value);
            if (index < 0) index = 0;
            int newIndex = EditorGUILayout.Popup(label, index, comparisonOperatorLabels, GUILayout.ExpandWidth(true));
            return comparisonOperatorValues[newIndex];
        }

        private bool IsConditionValueValid(ConditionRule condition)
        {
            if (condition == null) return false;
            if (condition.IsValueVariable) return !string.IsNullOrWhiteSpace(condition.Value);
            switch (condition.ValueDataType)
            {
                case ValueType.Integer:
                    return int.TryParse(condition.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                case ValueType.Decimal:
                    return decimal.TryParse(condition.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
                case ValueType.Boolean:
                    return true;
                default:
                    return true;
            }
        }

        private bool ParseBooleanCondition(string value)
        {
            return NormalizeBooleanValue(value) == "true";
        }

        private string NormalizeBooleanValue(string rawValue)
        {
            string value = (rawValue ?? "").Trim();
            if (string.IsNullOrEmpty(value)) return "true";
            string lowerValue = value.ToLowerInvariant();
            if (lowerValue == "true" || lowerValue == "1") return "true";
            if (lowerValue == "false" || lowerValue == "0") return "false";
            if ("true".StartsWith(lowerValue) || lowerValue.StartsWith("true")) return "true";
            if ("false".StartsWith(lowerValue) || lowerValue.StartsWith("false")) return "false";
            if (lowerValue[0] == 't') return "true";
            if (lowerValue[0] == 'f') return "false";
            return "true";
        }

        private void DrawConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var condition = conditions[i];
                EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);
                condition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name for this condition."), condition.VariableName ?? "", GUILayout.ExpandWidth(true));
                condition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), condition.Operator);
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type expected for this condition."), condition.ValueDataType, GUILayout.ExpandWidth(true));
                DrawConditionValueRow(condition, false);
                if (GUILayout.Button(new GUIContent("Remove", "Remove this condition."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(this, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button(new GUIContent("Add", "Add a new condition."), GUILayout.ExpandWidth(true), GUILayout.Height(25)))
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
            DrawFunctionAddSection(functions);
            DrawSectionSeparator();
            for (int i = 0; i < functions.Count; i++)
            {
                var func = functions[i];
                if (func.Parameters == null) func.Parameters = new Dictionary<string, string>();
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(new GUIContent("Function", "Captured function name."), func.MethodName ?? "", GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
                if (!functionParameterFoldouts.TryGetValue(func, out var isExpanded)) isExpanded = false;
                isExpanded = EditorGUILayout.Foldout(isExpanded, new GUIContent("Parameters", "Show or hide parameter values."), true);
                functionParameterFoldouts[func] = isExpanded;
                if (isExpanded)
                {
                    var parameterOrder = GetFunctionParameterOrder(func);
                    foreach (var parameterName in parameterOrder)
                    {
                        if (!func.Parameters.ContainsKey(parameterName)) func.Parameters[parameterName] = "";
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(new GUIContent("", "Parameter name."), parameterName, GUILayout.Width(130));
                        EditorGUI.EndDisabledGroup();
                        func.Parameters[parameterName] = EditorGUILayout.TextField(new GUIContent("", "Parameter value."), func.Parameters[parameterName] ?? "", GUILayout.ExpandWidth(true));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                func.Timestamp = EditorGUILayout.IntField(new GUIContent("Timestamp", "Execution order for this function."), func.Timestamp, GUILayout.ExpandWidth(true));
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("Remove", "Remove this function."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(this, "Remove Function");
                    functionParameterFoldouts.Remove(func);
                    functions.RemoveAt(i);
                    MarkDirty();
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawFunctionAddSection(List<ConversationFunction> functions)
        {
            var categoryNames = ConversationFunctionLibrary.GetFunctionCategoryNames();
            if (categoryNames.Length == 0) categoryNames = new[] { "Custom" };
            if (!categoryNames.Contains(selectedFunctionCategory)) selectedFunctionCategory = categoryNames[0];
            int categoryIndex = System.Array.IndexOf(categoryNames, selectedFunctionCategory);
            if (categoryIndex < 0) categoryIndex = 0;
            int newCategoryIndex = EditorGUILayout.Popup(new GUIContent("Category", "Function category filter."), categoryIndex, categoryNames, GUILayout.ExpandWidth(true));
            if (newCategoryIndex != categoryIndex)
            {
                selectedFunctionCategory = categoryNames[newCategoryIndex];
                selectedFunctionName = "";
                pendingFunctionParameters = new Dictionary<string, string>();
            }
            bool isCustomCategory = selectedFunctionCategory == "Custom";
            if (isCustomCategory)
            {
                customFunctionName = EditorGUILayout.TextField(new GUIContent("Function", "Custom function name."), customFunctionName ?? "", GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                pendingCustomParameterName = EditorGUILayout.TextField(new GUIContent("", "Custom parameter name."), pendingCustomParameterName ?? "", GUILayout.ExpandWidth(true));
                pendingCustomParameterValue = EditorGUILayout.TextField(new GUIContent("", "Custom parameter value."), pendingCustomParameterValue ?? "", GUILayout.ExpandWidth(true));
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button(new GUIContent("+", "Add custom parameter."), GUILayout.Width(28)))
                {
                    if (string.IsNullOrWhiteSpace(pendingCustomParameterName)) EditorUtility.DisplayDialog("Invalid Parameter", "Parameter name cannot be empty.", "OK");
                    else
                    {
                        string parameterName = pendingCustomParameterName.Trim();
                        pendingFunctionParameters[parameterName] = pendingCustomParameterValue ?? "";
                        pendingCustomParameterName = "";
                        pendingCustomParameterValue = "";
                    }
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
                foreach (var key in pendingFunctionParameters.Keys.ToList())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(new GUIContent("", "Captured parameter name."), key, GUILayout.Width(130));
                    EditorGUI.EndDisabledGroup();
                    pendingFunctionParameters[key] = EditorGUILayout.TextField(new GUIContent("", "Captured parameter value."), pendingFunctionParameters[key] ?? "", GUILayout.ExpandWidth(true));
                    oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button(new GUIContent("X", "Remove custom parameter."), GUILayout.Width(28)))
                    {
                        pendingFunctionParameters.Remove(key);
                        GUI.backgroundColor = oldColor;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                var functionsByCategory = ConversationFunctionLibrary.GetFunctionsForCategory(selectedFunctionCategory);
                if (functionsByCategory.Length == 0)
                {
                    selectedFunctionName = "";
                    pendingFunctionParameters.Clear();
                    EditorGUILayout.HelpBox("No functions available in this category.", MessageType.Info);
                }
                else
                {
                    if (!functionsByCategory.Contains(selectedFunctionName))
                    {
                        selectedFunctionName = functionsByCategory[0];
                        SetupPendingParametersForFunction(selectedFunctionName);
                    }
                    int selectedIndex = System.Array.IndexOf(functionsByCategory, selectedFunctionName);
                    if (selectedIndex < 0) selectedIndex = 0;
                    int newFunctionIndex = EditorGUILayout.Popup(new GUIContent("Function", "Function name filtered by category."), selectedIndex, functionsByCategory, GUILayout.ExpandWidth(true));
                    if (newFunctionIndex != selectedIndex)
                    {
                        selectedFunctionName = functionsByCategory[newFunctionIndex];
                        SetupPendingParametersForFunction(selectedFunctionName);
                    }
                    var parameterDefinitions = ConversationFunctionLibrary.GetFunctionParameters(selectedFunctionName);
                    if (parameterDefinitions != null)
                    {
                        foreach (var parameterDefinition in parameterDefinitions)
                        {
                            if (!pendingFunctionParameters.ContainsKey(parameterDefinition.Key)) pendingFunctionParameters[parameterDefinition.Key] = "";
                            pendingFunctionParameters[parameterDefinition.Key] = EditorGUILayout.TextField(new GUIContent(parameterDefinition.Key, parameterDefinition.Value), pendingFunctionParameters[parameterDefinition.Key] ?? "", GUILayout.ExpandWidth(true));
                        }
                    }
                }
            }
            pendingFunctionTimestamp = EditorGUILayout.IntField(new GUIContent("Timestamp", "Execution order for the new function."), pendingFunctionTimestamp, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(new GUIContent("Add", "Add function to this node."), GUILayout.ExpandWidth(true), GUILayout.Height(25)))
            {
                string methodName = isCustomCategory ? (customFunctionName ?? "").Trim() : selectedFunctionName;
                if (string.IsNullOrWhiteSpace(methodName))
                {
                    EditorUtility.DisplayDialog("Invalid Function", "Function name cannot be empty.", "OK");
                    return;
                }
                if (pendingFunctionParameters.Keys.Any(string.IsNullOrWhiteSpace))
                {
                    EditorUtility.DisplayDialog("Invalid Parameters", "Parameter names cannot be empty.", "OK");
                    return;
                }
                Undo.RecordObject(this, "Add Function");
                var newFunction = new ConversationFunction
                {
                    MethodName = methodName,
                    Parameters = new Dictionary<string, string>(pendingFunctionParameters),
                    Timestamp = pendingFunctionTimestamp
                };
                functions.Add(newFunction);
                functionParameterFoldouts[newFunction] = false;
                if (isCustomCategory)
                {
                    customFunctionName = "";
                    pendingFunctionParameters.Clear();
                    pendingCustomParameterName = "";
                    pendingCustomParameterValue = "";
                }
                else
                {
                    SetupPendingParametersForFunction(selectedFunctionName);
                }
                pendingFunctionTimestamp = 0;
                MarkDirty();
            }
        }

        private void SetupPendingParametersForFunction(string functionName)
        {
            pendingFunctionParameters = new Dictionary<string, string>();
            var parameterDefinitions = ConversationFunctionLibrary.GetFunctionParameters(functionName);
            if (parameterDefinitions == null) return;
            foreach (var parameter in parameterDefinitions) pendingFunctionParameters[parameter.Key] = "";
        }

        private IEnumerable<string> GetFunctionParameterOrder(ConversationFunction function)
        {
            var predefinedParameters = ConversationFunctionLibrary.GetFunctionParameters(function.MethodName);
            if (predefinedParameters != null)
            {
                foreach (var key in predefinedParameters.Keys)
                {
                    if (!function.Parameters.ContainsKey(key)) function.Parameters[key] = "";
                }
                foreach (var key in function.Parameters.Keys.Where(k => !predefinedParameters.ContainsKey(k)).ToList()) function.Parameters.Remove(key);
                return predefinedParameters.Keys;
            }
            return function.Parameters.Keys.OrderBy(key => key).ToList();
        }

        private void DrawSectionSeparator()
        {
            EditorGUILayout.Space(4);
            Rect separatorRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(separatorRect, new Color(0.35f, 0.35f, 0.35f, 1f));
            EditorGUILayout.Space(6);
        }

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
            NormalizeConversationConditionBooleanValues(conversationData);
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

        private void NormalizeConversationConditionBooleanValues(ConversationData data)
        {
            if (data?.ConversationManager?.Nodes == null) return;
            foreach (var node in data.ConversationManager.Nodes)
            {
                if (node.Options != null)
                {
                    foreach (var option in node.Options)
                    {
                        NormalizeConditionList(option.Conditions);
                    }
                }
                if (node.conditionalBranch != null)
                {
                    NormalizeConditionList(node.conditionalBranch.Conditions);
                }
            }
        }

        private void NormalizeConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            foreach (var condition in conditions)
            {
                if (condition == null || condition.ValueDataType != ValueType.Boolean) continue;
                condition.Value = NormalizeBooleanValue(condition.Value);
                condition.IsValueVariable = false;
            }
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