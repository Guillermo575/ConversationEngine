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
        private bool stylesInitialized = false;
        #endregion

        #region Grid Constants
        private const float gridSpacing = 20f;
        private Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        #endregion

        #region Zoom Controls
        private const float zoomControlScale = 1.5f;
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
            window.Show();
            window.Focus();
        }
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
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
            nodeHeaderStyle.fontSize = 11;
            nodeHeaderStyle.normal.textColor = Color.white;
            nodeHeaderStyle.fontStyle = FontStyle.Bold;
            stylesInitialized = true;
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
                else if (e.keyCode == KeyCode.Delete && selectedNode != null)
                {
                    DeleteNode(selectedNode);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.F && selectedNode != null)
                {
                    FrameNode(selectedNode);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    if (isConnecting)
                    {
                        isConnecting = false;
                        connectingFromNode = null;
                        connectingFromOption = null;
                        connectingFromBranch = null;
                    }
                    else if (selectedNode != null)
                    {
                        selectedNode = null;
                        selectedOption = null;
                        selectedBranch = null;
                        showInspector = false;
                    }
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
            if (GUILayout.Button("Auto-Layout", EditorStyles.toolbarButton, GUILayout.Width(80))) ShowAutoLayoutMenu();
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
            if (conversationData?.ConversationManager?.Nodes == null) return;
            Rect graphRect = CalculateGraphRect();
            currentGraphRect = graphRect;
            HandleGraphInput(graphRect);
            GUI.Box(graphRect, GUIContent.none);
            GUI.BeginGroup(graphRect);
            Rect localRect = new Rect(0, 0, graphRect.width, graphRect.height);
            DrawGrid(localRect);
            DrawConnections();
            DrawNodes();
            if (isConnecting) DrawConnectionLine();
            DrawZoomControls(localRect);
            GUI.EndGroup();
        }
        private Rect CalculateGraphRect()
        {
            return GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }
        private Vector2 WorldToGraph(Vector2 worldPos)
        {
            return (worldPos + panOffset) * zoom;
        }
        private Rect WorldToGraphRect(Rect worldRect)
        {
            return new Rect(WorldToGraph(worldRect.position), worldRect.size * zoom);
        }
        private Vector2 WindowToWorld(Vector2 windowPos)
        {
            Vector2 graphLocalPos = windowPos - currentGraphRect.position;
            return (graphLocalPos / zoom) - panOffset;
        }

        private void HandleGraphInput(Rect graphRect)
        {
            Event e = Event.current;
            if (!graphRect.Contains(e.mousePosition) && e.type != EventType.MouseUp) return;
            Rect zoomControlsRect = GetZoomControlsRect(graphRect);
            bool isPointerOverZoomControls = zoomControlsRect.Contains(e.mousePosition);
            if (isPointerOverZoomControls && !isDraggingView) return;
            if (e.type == EventType.ScrollWheel)
            {
                float oldZoom = zoom;
                float zoomDelta = -e.delta.y * 0.05f;
                float newZoom = Mathf.Clamp(zoom + zoomDelta, minZoom, maxZoom);

                if (!Mathf.Approximately(newZoom, oldZoom))
                {
                    Vector2 graphLocalMouse = e.mousePosition - graphRect.position;
                    Vector2 worldMouse = (graphLocalMouse / oldZoom) - panOffset;
                    zoom = newZoom;
                    panOffset = (graphLocalMouse / zoom) - worldMouse;
                    SaveEditorZoomSetting();
                }
                e.Use();
                Repaint();
                return;
            }
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (isRightClickMenuActive)
                {
                    isRightClickMenuActive = false;
                    e.Use();
                    return;
                }
                bool clickedOnNode = false;
                Vector2 mouseWorldPos = WindowToWorld(e.mousePosition);
                if (conversationData?.ConversationManager?.Nodes != null)
                {
                    foreach (var node in conversationData.ConversationManager.Nodes)
                    {
                        Rect nodeRect = new Rect(node.EditorPosition.x, node.EditorPosition.y, node.EditorSize.x, node.EditorSize.y);
                        if (nodeRect.Contains(mouseWorldPos))
                        {
                            clickedOnNode = true;
                            break;
                        }
                    }
                }
                if (clickedOnNode) return;
                if (isConnecting)
                {
                    isConnecting = false;
                    connectingFromNode = null;
                    connectingFromOption = null;
                    connectingFromBranch = null;
                    e.Use();
                    Repaint();
                    return;
                }
                isDraggingView = true;
                dragStartPos = e.mousePosition;
                e.Use();
                Repaint();
                return;
            }

            if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && e.alt)))
            {
                isDraggingView = true;
                dragStartPos = e.mousePosition;
                e.Use();
                return;
            }
            if (e.type == EventType.MouseDrag && isDraggingView)
            {
                panOffset += e.delta / zoom;
                e.Use();
                Repaint();
                return;
            }
            if (e.type == EventType.MouseUp)
            {
                isDraggingView = false;
                return;
            }
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                bool clickedOnNode = false;
                Vector2 mouseWorldPos = WindowToWorld(e.mousePosition);
                if (conversationData?.ConversationManager?.Nodes != null)
                {
                    foreach (var node in conversationData.ConversationManager.Nodes)
                    {
                        Rect nodeRect = new Rect(node.EditorPosition.x, node.EditorPosition.y, node.EditorSize.x, node.EditorSize.y);
                        if (nodeRect.Contains(mouseWorldPos))
                        {
                            clickedOnNode = true;
                            break;
                        }
                    }
                }
                if (!clickedOnNode)
                {
                    contextMenuPosition = mouseWorldPos;
                    ShowContextMenu();
                    e.Use();
                }
            }
        }

        private void DrawGrid(Rect rect)
        {
            Handles.BeginGUI();
            float spacing = gridSpacing * zoom;
            if (spacing <= 0.001f)
            {
                Handles.EndGUI();
                return;
            }
            int widthDivs = Mathf.CeilToInt(rect.width / spacing);
            int heightDivs = Mathf.CeilToInt(rect.height / spacing);
            float offsetX = Mathf.Repeat(panOffset.x * zoom, spacing);
            float offsetY = Mathf.Repeat(panOffset.y * zoom, spacing);
            Handles.color = gridColor;
            for (int i = 0; i <= widthDivs; i++)
            {
                float x = spacing * i + offsetX;
                Handles.DrawLine(new Vector3(x, 0f), new Vector3(x, rect.height));
            }
            for (int i = 0; i <= heightDivs; i++)
            {
                float y = spacing * i + offsetY;
                Handles.DrawLine(new Vector3(0f, y), new Vector3(rect.width, y));
            }
            Handles.EndGUI();
        }
        private void DrawNodes()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                DrawNode(node);
            }
        }

        private void DrawNode(ConversationNode node)
        {
            Rect nodeWorldRect = new Rect(node.EditorPosition.x, node.EditorPosition.y, node.EditorSize.x, node.EditorSize.y);
            Rect nodeRect = WorldToGraphRect(nodeWorldRect);
            GUIStyle style = GetNodeStyle(node);
            GUI.Box(nodeRect, "", style);
            GUILayout.BeginArea(nodeRect);
            DrawNodeContent(node);
            GUILayout.EndArea();
            HandleNodeInteraction(node, nodeRect);
            if (node.Options != null && node.Options.Count > 0) DrawNodeOptions(node, nodeRect);
            if (node.ConditionalBranches != null && node.ConditionalBranches.Count > 0) DrawConditionalBranches(node, nodeRect);
            if (selectedNode == node && node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
            {
                DrawResizeHandle(node, nodeRect);
            }
        }
        private GUIStyle GetNodeStyle(ConversationNode node)
        {
            bool isSelected = selectedNode == node;
            bool isDragging = isNodeBeingDragged && isSelected;
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    if (isDragging) return startNodeDraggingStyle;
                    if (isSelected) return startNodeSelectedStyle;
                    return startNodeStyle;
                case ConversationNodeType.End:
                    if (isDragging) return endNodeDraggingStyle;
                    if (isSelected) return endNodeSelectedStyle;
                    return endNodeStyle;
                case ConversationNodeType.Function:
                    if (isDragging) return functionNodeDraggingStyle;
                    if (isSelected) return functionNodeSelectedStyle;
                    return functionNodeStyle;
                case ConversationNodeType.Conditional:
                    if (isDragging) return conditionalNodeDraggingStyle;
                    if (isSelected) return conditionalNodeSelectedStyle;
                    return conditionalNodeStyle;
                default:
                    if (isDragging) return nodeDraggingStyle;
                    if (isSelected) return nodeSelectedStyle;
                    return nodeStyle;
            }
        }

        private void DrawNodeContent(ConversationNode node)
        {
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("START", nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                case ConversationNodeType.End:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("END", nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                case ConversationNodeType.Function:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("FUNCTION", nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                default:
                    GUILayout.Label($"ID: {node.Id}", nodeHeaderStyle);
                    if (!string.IsNullOrEmpty(node.SpeakerActorId))
                    {
                        var actor = conversationData.ResourceManager.Actors.FirstOrDefault(a => a.Id == node.SpeakerActorId);
                        if (actor != null && !string.IsNullOrEmpty(actor.IconPath))
                        {
                            GUILayout.Label($"?? {node.SpeakerActorId}");
                        }
                        else
                        {
                            GUILayout.Label($"Actor: {node.SpeakerActorId}");
                        }
                    }
                    if (!string.IsNullOrEmpty(node.Text))
                    {
                        string preview = node.Text.Length > 100 ? node.Text.Substring(0, 100) + "..." : node.Text;
                        var styleNode = EditorStyles.wordWrappedLabel;
                        styleNode.fontStyle = FontStyle.Bold;
                        GUILayout.Label(preview, styleNode);
                    }
                    break;
            }
        }

        private void HandleNodeInteraction(ConversationNode node, Rect nodeRect)
        {
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);
            if (e.type == EventType.MouseDown && nodeRect.Contains(mouseGraphPos))
            {
                if (e.button == 0)
                {
                    if (isRightClickMenuActive)
                    {
                        isRightClickMenuActive = false;
                        return;
                    }
                    if (isConnecting)
                    {
                        if (node.NodeType != ConversationNodeType.Start && node != connectingFromNode)
                        {
                            CompleteConnection(node);
                        }
                        else
                        {
                            isConnecting = false;
                            connectingFromNode = null;
                            connectingFromOption = null;
                            connectingFromBranch = null;
                        }
                        e.Use();
                        Repaint();
                        return;
                    }
                    selectedNode = node;
                    selectedOption = null;
                    selectedBranch = null;
                    showInspector = true;
                    isMouseOverNode = true;
                    isNodeBeingDragged = false;
                    GUI.FocusControl(null);
                    e.Use();
                    Repaint();
                }
                else if (e.button == 1)
                {
                    if (node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
                    {
                        selectedNode = node;
                        showInspector = true;
                        ShowNodeContextMenu(node);
                        e.Use();
                    }
                }
            }
            if (e.type == EventType.MouseDrag && selectedNode == node && !isConnecting && e.button == 0 && isMouseOverNode)
            {
                if (!isNodeBeingDragged) isNodeBeingDragged = true;
                Undo.RecordObject(this, "Move Node");
                node.EditorPosition += e.delta / zoom;
                node.EditorPosition.x = Mathf.Max(0, Mathf.Min(10000, node.EditorPosition.x));
                node.EditorPosition.y = Mathf.Max(0, Mathf.Min(10000, node.EditorPosition.y));
                MarkDirty();
                e.Use();
                Repaint();
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isMouseOverNode = false;
                if (isNodeBeingDragged)
                {
                    isNodeBeingDragged = false;
                    Repaint();
                }
            }
        }

        /// <summary>
        /// Converts a window-space mouse position to graph-local GUI coordinates.
        /// </summary>
        private Vector2 WindowToGraphLocal(Vector2 windowPos)
        {
            return windowPos - currentGraphRect.position;
        }
        #endregion

        #region Node Options and Branches
        private void DrawNodeOptions(ConversationNode node, Rect nodeRect)
        {
            float optionHeight = 60f;
            float optionWidth = 150f;
            float spacing = 10f;

            for (int i = 0; i < node.Options.Count; i++)
            {
                var option = node.Options[i];
                Rect optionWorldRect = new Rect(
                    node.EditorPosition.x + node.EditorSize.x + spacing,
                    node.EditorPosition.y + i * (optionHeight + spacing),
                    optionWidth,
                    optionHeight
                );
                Rect optionRect = WorldToGraphRect(optionWorldRect);

                GUI.Box(optionRect, "", optionNodeStyle);

                GUILayout.BeginArea(optionRect);
                GUILayout.Label($"Option {i + 1}", EditorStyles.boldLabel);
                GUILayout.Label(string.IsNullOrEmpty(option.Text) ? "(empty)" :
                    (option.Text.Length > 20 ? option.Text.Substring(0, 20) + "..." : option.Text));
                GUILayout.EndArea();

                HandleOptionInteraction(node, option, optionRect, i);
                DrawConnectionFromOption(node, option, optionRect);
            }
        }

        private void HandleOptionInteraction(ConversationNode node, ConversationOption option, Rect optionRect, int index)
        {
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);

            if (e.type == EventType.MouseDown && optionRect.Contains(mouseGraphPos))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = option;
                    connectingFromBranch = null;
                    e.Use();
                }
                else if (e.button == 0)
                {
                    selectedNode = node;
                    selectedOption = option;
                    selectedBranch = null;
                    e.Use();
                    Repaint();
                }
                else if (e.button == 1)
                {
                    ShowOptionContextMenu(node, option, index);
                    e.Use();
                }
            }

            if (e.type == EventType.MouseUp && e.button == 0 && isConnecting && connectingFromOption == null && optionRect.Contains(mouseGraphPos))
            {
                isConnecting = false;
                e.Use();
            }
        }

        private void DrawConditionalBranches(ConversationNode node, Rect nodeRect)
        {
            if (node.NodeType != ConversationNodeType.Conditional) return;

            float branchHeight = 40f;
            float branchWidth = 100f;
            float spacing = 10f;

            for (int i = 0; i < node.ConditionalBranches.Count; i++)
            {
                var branch = node.ConditionalBranches[i];

                Rect trueWorldRect = new Rect(
                    node.EditorPosition.x + node.EditorSize.x + spacing,
                    node.EditorPosition.y + i * (branchHeight * 2 + spacing),
                    branchWidth,
                    branchHeight
                );
                Rect trueRect = WorldToGraphRect(trueWorldRect);

                GUI.Box(trueRect, "", optionNodeStyle);
                GUILayout.BeginArea(trueRect);
                GUILayout.Label($"Branch {i + 1}: TRUE", EditorStyles.boldLabel);
                GUILayout.EndArea();

                Rect falseWorldRect = new Rect(
                    node.EditorPosition.x + node.EditorSize.x + spacing,
                    node.EditorPosition.y + i * (branchHeight * 2 + spacing) + branchHeight + spacing / 2,
                    branchWidth,
                    branchHeight
                );
                Rect falseRect = WorldToGraphRect(falseWorldRect);

                GUI.Box(falseRect, "", optionNodeStyle);
                GUILayout.BeginArea(falseRect);
                GUILayout.Label($"Branch {i + 1}: FALSE", EditorStyles.boldLabel);
                GUILayout.EndArea();

                HandleBranchInteraction(node, branch, trueRect, falseRect, i);
                DrawConnectionFromBranch(node, branch, trueRect, falseRect);
            }
        }

        private void HandleBranchInteraction(ConversationNode node, ConditionalBranch branch,
            Rect trueRect, Rect falseRect, int index)
        {
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);

            if (e.type == EventType.MouseDown && trueRect.Contains(mouseGraphPos))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = branch;
                    connectingBranchIndex = 0;
                    e.Use();
                }
                else if (e.button == 0)
                {
                    selectedNode = node;
                    selectedOption = null;
                    selectedBranch = branch;
                    e.Use();
                    Repaint();
                }
            }

            if (e.type == EventType.MouseDown && falseRect.Contains(mouseGraphPos))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = branch;
                    connectingBranchIndex = 1;
                    e.Use();
                }
                else if (e.button == 0)
                {
                    selectedNode = node;
                    selectedOption = null;
                    selectedBranch = branch;
                    e.Use();
                    Repaint();
                }
            }
        }

        private void DrawResizeHandle(ConversationNode node, Rect nodeRect)
        {
            Rect handleRect = new Rect(nodeRect.xMax - 10, nodeRect.yMax - 10, 10, 10);
            EditorGUIUtility.AddCursorRect(new Rect(currentGraphRect.x + handleRect.x, currentGraphRect.y + handleRect.y, handleRect.width, handleRect.height), MouseCursor.ResizeUpLeft);
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);
            if (e.type == EventType.MouseDown && handleRect.Contains(mouseGraphPos)) e.Use();
        }
        #endregion

        #region Connection Drawing
        private void DrawConnections()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;

            Handles.BeginGUI();

            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.NextNodeId > 0)
                {
                    var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.NextNodeId);
                    if (targetNode != null)
                    {
                        DrawConnection(node.EditorPosition + node.EditorSize / 2,
                                     targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2),
                                     Color.white);
                    }
                }

                if (node.Options != null)
                {
                    float optionHeight = 60f;
                    float optionWidth = 150f;
                    float spacing = 10f;

                    for (int i = 0; i < node.Options.Count; i++)
                    {
                        var option = node.Options[i];
                        if (option.NextNodeId > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == option.NextNodeId);
                            if (targetNode != null)
                            {
                                Vector2 optionPos = new Vector2(
                                    node.EditorPosition.x + node.EditorSize.x + spacing + optionWidth / 2,
                                    node.EditorPosition.y + i * (optionHeight + spacing) + optionHeight / 2
                                );

                                DrawConnection(optionPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2),
                                             Color.cyan);
                            }
                        }
                    }
                }

                if (node.ConditionalBranches != null)
                {
                    float branchHeight = 40f;
                    float branchWidth = 100f;
                    float spacing = 10f;

                    for (int i = 0; i < node.ConditionalBranches.Count; i++)
                    {
                        var branch = node.ConditionalBranches[i];

                        if (branch.NextNodeIdTrue > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdTrue);
                            if (targetNode != null)
                            {
                                Vector2 branchPos = new Vector2(
                                    node.EditorPosition.x + node.EditorSize.x + spacing + branchWidth / 2,
                                    node.EditorPosition.y + i * (branchHeight * 2 + spacing) + branchHeight / 2
                                );

                                DrawConnection(branchPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2),
                                             Color.green);
                            }
                        }

                        if (branch.NextNodeIdFalse > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdFalse);
                            if (targetNode != null)
                            {
                                Vector2 branchPos = new Vector2(
                                    node.EditorPosition.x + node.EditorSize.x + spacing + branchWidth / 2,
                                    node.EditorPosition.y + i * (branchHeight * 2 + spacing) + branchHeight * 1.5f + 5
                                );

                                DrawConnection(branchPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2),
                                             Color.red);
                            }
                        }
                    }
                }
            }

            Handles.EndGUI();
        }

        private void DrawConnection(Vector2 startWorld, Vector2 endWorld, Color color)
        {
            Vector2 start = WorldToGraph(startWorld);
            Vector2 end = WorldToGraph(endWorld);

            Handles.color = color;
            Vector2 tangentOffset = Vector2.right * (50f * zoom);
            Vector2 startTangent = start + tangentOffset;
            Vector2 endTangent = end - tangentOffset;
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 5f);

            Vector2 direction = (end - endTangent).normalized;
            Vector2 arrowPoint1 = end - direction * 10 + new Vector2(-direction.y, direction.x) * 5;
            Vector2 arrowPoint2 = end - direction * 10 - new Vector2(-direction.y, direction.x) * 5;
            Handles.DrawAAPolyLine(5f, end, arrowPoint1);
            Handles.DrawAAPolyLine(5f, end, arrowPoint2);
        }

        private void DrawConnectionLine()
        {
            if (!isConnecting) return;

            Vector2 startPos = Vector2.zero;

            if (connectingFromOption != null)
            {
                var node = connectingFromNode;
                int optionIndex = node.Options.IndexOf(connectingFromOption);
                if (optionIndex >= 0)
                {
                    float optionHeight = 60f;
                    float optionWidth = 150f;
                    float spacing = 10f;
                    startPos = new Vector2(
                        node.EditorPosition.x + node.EditorSize.x + spacing + optionWidth,
                        node.EditorPosition.y + optionIndex * (optionHeight + spacing) + optionHeight / 2
                    );
                }
            }
            else if (connectingFromBranch != null)
            {
                var node = connectingFromNode;
                int branchIndex = node.ConditionalBranches.IndexOf(connectingFromBranch);
                if (branchIndex >= 0)
                {
                    float branchHeight = 40f;
                    float branchWidth = 100f;
                    float spacing = 10f;
                    float yOffset = connectingBranchIndex == 0 ? branchHeight / 2 : branchHeight * 1.5f + 5;
                    startPos = new Vector2(
                        node.EditorPosition.x + node.EditorSize.x + spacing + branchWidth,
                        node.EditorPosition.y + branchIndex * (branchHeight * 2 + spacing) + yOffset
                    );
                }
            }
            else
            {
                startPos = connectingFromNode.EditorPosition + connectingFromNode.EditorSize / 2;
            }

            Vector2 endPos = WindowToWorld(Event.current.mousePosition);

            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(5f, WorldToGraph(startPos), WorldToGraph(endPos));
            Handles.EndGUI();

            Repaint();
        }

        private void CompleteConnection(ConversationNode targetNode)
        {
            if (!isConnecting || connectingFromNode == null) return;

            Undo.RecordObject(this, "Create Connection");

            if (connectingFromOption != null)
            {
                connectingFromOption.NextNodeId = targetNode.Id;
            }
            else if (connectingFromBranch != null)
            {
                if (connectingBranchIndex == 0)
                {
                    connectingFromBranch.NextNodeIdTrue = targetNode.Id;
                }
                else
                {
                    connectingFromBranch.NextNodeIdFalse = targetNode.Id;
                }
            }
            else
            {
                connectingFromNode.NextNodeId = targetNode.Id;
            }

            MarkDirty();
            isConnecting = false;
            connectingFromNode = null;
            connectingFromOption = null;
            connectingFromBranch = null;
            Repaint();
        }
        #endregion

        #region Inspector Panel
        private void DrawInspectorPanel()
        {
            // This is now called from within a GUILayout.BeginArea in DrawThreePanelLayout
            // So we don't need to create our own area here
            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);

            if (selectedNode != null)
            {
                DrawNodeInspector(selectedNode);
            }
            else if (selectedOption != null)
            {
                DrawOptionInspector(selectedOption);
            }
            else if (selectedBranch != null)
            {
                DrawBranchInspector(selectedBranch);
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
            option.NextNodeId = DrawNodeIdDropdown("Next Node", option.NextNodeId, selectedNode);
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
            branch.NextNodeIdTrue = DrawNodeIdDropdown("Next Node (True)", branch.NextNodeIdTrue, selectedNode);
            branch.NextNodeIdFalse = DrawNodeIdDropdown("Next Node (False)", branch.NextNodeIdFalse, selectedNode);
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

        private void DrawZoomControls(Rect area)
        {
            Rect containerRect = GetZoomControlsRect(area);
            EditorGUI.DrawRect(containerRect, new Color(0f, 0f, 0f, 0.4f));
            Rect labelRect = new Rect(containerRect.x, containerRect.y + (4f * zoomControlScale), containerRect.width, 20f * zoomControlScale);
            GUI.Label(labelRect, $"{zoom:F1}x", EditorStyles.centeredGreyMiniLabel);
            Rect zoomSliderRect = new Rect(
                containerRect.x + (10f * zoomControlScale),
                containerRect.y + (28f * zoomControlScale),
                14f * zoomControlScale,
                containerRect.height - (36f * zoomControlScale));
            float newZoom = GUI.VerticalSlider(zoomSliderRect, zoom, maxZoom, minZoom);
            if (!Mathf.Approximately(newZoom, zoom))
            {
                zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
                SaveEditorZoomSetting();
                Repaint();
            }
        }

        private Rect GetZoomControlsRect(Rect area)
        {
            float width = 34f * zoomControlScale;
            float height = 180f * zoomControlScale;
            float marginRight = 8f;
            float marginTop = 8f;
            return new Rect(area.xMax - width - marginRight, area.y + marginTop, width, height);
        }

        private void EnsureEditorSettings()
        {
            if (conversationData == null) return;
            if (conversationData.EditorSettings == null)
                conversationData.EditorSettings = new ConversationEditorSettings();
        }

        private void ApplyZoomFromConversationSettings()
        {
            if (conversationData == null)
            {
                zoom = 1f;
                return;
            }
            EnsureEditorSettings();
            float savedZoom = conversationData.EditorSettings.Zoom;
            if (savedZoom <= 0f) savedZoom = 1f;
            zoom = Mathf.Clamp(savedZoom, minZoom, maxZoom);
            conversationData.EditorSettings.Zoom = zoom;
        }

        private void SaveEditorZoomSetting()
        {
            if (conversationData == null) return;
            EnsureEditorSettings();
            float clampedZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            if (!Mathf.Approximately(conversationData.EditorSettings.Zoom, clampedZoom))
            {
                conversationData.EditorSettings.Zoom = clampedZoom;
                MarkDirty();
            }
            zoom = clampedZoom;
        }

        private void DrawConnectionFromOption(ConversationNode node, ConversationOption option, Rect optionRect)
        {
        }

        private void DrawConnectionFromBranch(ConversationNode node, ConditionalBranch branch, Rect trueRect, Rect falseRect)
        {
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

        private void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Node/Dialogue"), false, () => CreateNode(ConversationNodeType.Dialogue));
            menu.AddItem(new GUIContent("Create Node/Function"), false, () => CreateNode(ConversationNodeType.Function));
            menu.AddItem(new GUIContent("Create Node/Dialogue with Options"), false, CreateNodeWithOptions);
            menu.AddItem(new GUIContent("Create Node/Conditional"), false, () => CreateNode(ConversationNodeType.Conditional));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Auto-Layout/Horizontal"), false, () => AutoLayoutNodes(true));
            menu.AddItem(new GUIContent("Auto-Layout/Vertical"), false, () => AutoLayoutNodes(false));
            isRightClickMenuActive = true;
            menu.ShowAsContext();
        }

        private void ShowNodeContextMenu(ConversationNode node)
        {
            GenericMenu menu = new GenericMenu();
            if (node.NodeType != ConversationNodeType.End)
            {
                menu.AddItem(new GUIContent("Connect to Node"), false, () =>
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = null;
                    isRightClickMenuActive = false;
                    Repaint();
                });
                menu.AddSeparator("");
            }
            if (node.NodeType == ConversationNodeType.Dialogue)
            {
                menu.AddItem(new GUIContent("Add Option"), false, () =>
                {
                    Undo.RecordObject(this, "Add Option");
                    if (node.Options == null) node.Options = new List<ConversationOption>();
                    node.Options.Add(new ConversationOption());
                    MarkDirty();
                    isRightClickMenuActive = false;
                });
            }
            menu.AddItem(new GUIContent("Duplicate Node"), false, () =>
            {
                DuplicateNode(node);
                isRightClickMenuActive = false;
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () =>
            {
                DeleteNode(node);
                isRightClickMenuActive = false;
            });
            isRightClickMenuActive = true;
            menu.ShowAsContext();
        }

        private void ShowOptionContextMenu(ConversationNode node, ConversationOption option, int index)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate Option"), false, () =>
            {
                Undo.RecordObject(this, "Duplicate Option");
                var newOption = new ConversationOption
                {
                    Text = option.Text,
                    NextNodeId = 0,
                    Conditions = new List<ConditionRule>(option.Conditions ?? new List<ConditionRule>())
                };
                node.Options.Insert(index + 1, newOption);
                MarkDirty();
            });
            menu.AddItem(new GUIContent("Delete Option"), false, () =>
            {
                Undo.RecordObject(this, "Delete Option");
                node.Options.RemoveAt(index);
                MarkDirty();
            });
            menu.ShowAsContext();
        }

        private void CreateNode(ConversationNodeType nodeType)
        {
            Undo.RecordObject(this, "Create Node");
            var newNode = new ConversationNode
            {
                Id = ConversationNodeUtility.GetNextAvailableId(conversationData.ConversationManager.Nodes),
                NodeType = nodeType,
                EditorPosition = contextMenuPosition,
                EditorSize = nodeType == ConversationNodeType.Conditional ? new Vector2(150, 100) : new Vector2(200, 100)
            };
            conversationData.ConversationManager.Nodes.Add(newNode);
            selectedNode = newNode;
            showInspector = true;
            isRightClickMenuActive = false;
            MarkDirty();
            Repaint();
        }

        private void CreateNodeWithOptions()
        {
            Undo.RecordObject(this, "Create Node with Options");
            var newNode = new ConversationNode
            {
                Id = ConversationNodeUtility.GetNextAvailableId(conversationData.ConversationManager.Nodes),
                NodeType = ConversationNodeType.Dialogue,
                EditorPosition = contextMenuPosition,
                EditorSize = new Vector2(200, 100),
                Options = new List<ConversationOption>
                {
                    new ConversationOption { Text = "Option 1" },
                    new ConversationOption { Text = "Option 2" }
                }
            };
            conversationData.ConversationManager.Nodes.Add(newNode);
            selectedNode = newNode;
            showInspector = true;
            isRightClickMenuActive = false;
            MarkDirty();
            Repaint();
        }

        private void DuplicateNode(ConversationNode node)
        {
            Undo.RecordObject(this, "Duplicate Node");
            var newNode = new ConversationNode
            {
                Id = ConversationNodeUtility.GetNextAvailableId(conversationData.ConversationManager.Nodes),
                NodeType = node.NodeType,
                SpeakerActorId = node.SpeakerActorId,
                Text = node.Text,
                NextNodeId = 0,
                EditorPosition = node.EditorPosition + new Vector2(20, 20),
                EditorSize = node.EditorSize,
                Options = node.Options?.Select(o => new ConversationOption
                {
                    Text = o.Text,
                    NextNodeId = 0,
                    Conditions = new List<ConditionRule>(o.Conditions ?? new List<ConditionRule>())
                }).ToList(),
                Functions = node.Functions?.Select(f => new ConversationFunction
                {
                    MethodName = f.MethodName,
                    Parameters = new Dictionary<string, string>(f.Parameters ?? new Dictionary<string, string>()),
                    Timestamp = f.Timestamp
                }).ToList(),
                ConditionalBranches = node.ConditionalBranches?.Select(b => new ConditionalBranch
                {
                    Conditions = new List<ConditionRule>(b.Conditions ?? new List<ConditionRule>()),
                    NextNodeIdTrue = 0,
                    NextNodeIdFalse = 0
                }).ToList(),
                DefaultBranchNodeId = 0
            };
            conversationData.ConversationManager.Nodes.Add(newNode);
            selectedNode = newNode;
            MarkDirty();
            Repaint();
        }

        private void DeleteNode(ConversationNode node)
        {
            if (node.NodeType == ConversationNodeType.Start || node.NodeType == ConversationNodeType.End)
            {
                EditorUtility.DisplayDialog("Cannot Delete", "Cannot delete Start or End nodes.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Delete Node", $"Are you sure you want to delete node {node.Id}?", "Delete", "Cancel"))
            {
                Undo.RecordObject(this, "Delete Node");
                ConversationNodeUtility.RemoveNodeReferences(node.Id, conversationData.ConversationManager.Nodes);
                conversationData.ConversationManager.Nodes.Remove(node);
                if (selectedNode == node) selectedNode = null;
                MarkDirty();
                Repaint();
            }
        }

        private void FrameNode(ConversationNode node)
        {
            if (node == null) return;
            Vector2 graphCenter = new Vector2(currentGraphRect.width, currentGraphRect.height) * 0.5f;
            panOffset = graphCenter / zoom - (node.EditorPosition + node.EditorSize * 0.5f);
            Repaint();
        }

        private void ShowAutoLayoutMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Horizontal"), false, () => AutoLayoutNodes(true));
            menu.AddItem(new GUIContent("Vertical"), false, () => AutoLayoutNodes(false));
            menu.ShowAsContext();
        }

        private void AutoLayoutNodes(bool horizontal)
        {
            if (conversationData?.ConversationManager?.Nodes == null || conversationData.ConversationManager.Nodes.Count == 0) return;
            Undo.RecordObject(this, "Auto-Layout Nodes");
            var startNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);
            if (startNode == null) return;
            var visited = new System.Collections.Generic.HashSet<int>();
            var levelPositions = new System.Collections.Generic.Dictionary<int, float>();
            if (horizontal)
            {
                LayoutNodesHorizontal(startNode, 50f, 50f, visited, levelPositions, 0);
            }
            else
            {
                LayoutNodesVertical(startNode, 50f, 50f, visited, levelPositions, 0);
            }
            MarkDirty();
            Repaint();
        }

        private float LayoutNodesHorizontal(ConversationNode node, float x, float y, System.Collections.Generic.HashSet<int> visited,
            System.Collections.Generic.Dictionary<int, float> levelPositions, int level)
        {
            if (node == null || visited.Contains(node.Id)) return y;
            visited.Add(node.Id);
            if (!levelPositions.ContainsKey(level)) levelPositions[level] = y;
            else y = levelPositions[level];
            node.EditorPosition = new Vector2(x, y);
            var nextNodes = new System.Collections.Generic.List<ConversationNode>();
            if (node.NextNodeId > 0)
            {
                var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.NextNodeId);
                if (nextNode != null) nextNodes.Add(nextNode);
            }
            if (node.Options != null)
            {
                foreach (var option in node.Options)
                {
                    if (option.NextNodeId > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == option.NextNodeId);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                }
            }
            if (node.ConditionalBranches != null)
            {
                foreach (var branch in node.ConditionalBranches)
                {
                    if (branch.NextNodeIdTrue > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdTrue);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                    if (branch.NextNodeIdFalse > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdFalse);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                }
            }
            if (node.DefaultBranchNodeId > 0)
            {
                var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.DefaultBranchNodeId);
                if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
            }
            float nextX = x + autoLayoutSpacing;
            float currentY = y;
            if (nextNodes.Count > 1)
            {
                float totalHeight = (nextNodes.Count - 1) * autoLayoutVerticalSpacing;
                currentY = y - totalHeight / 2;
            }
            float maxY = currentY;
            foreach (var nextNode in nextNodes)
            {
                float branchMaxY = LayoutNodesHorizontal(nextNode, nextX, currentY, visited, levelPositions, level + 1);
                maxY = Mathf.Max(maxY, branchMaxY);
                currentY = branchMaxY + autoLayoutVerticalSpacing;
            }
            levelPositions[level] = Mathf.Max(levelPositions[level], maxY);
            return maxY;
        }

        private float LayoutNodesVertical(ConversationNode node, float x, float y, System.Collections.Generic.HashSet<int> visited,
            System.Collections.Generic.Dictionary<int, float> levelPositions, int level)
        {
            if (node == null || visited.Contains(node.Id)) return x;
            visited.Add(node.Id);
            if (!levelPositions.ContainsKey(level)) levelPositions[level] = x;
            else x = levelPositions[level];
            node.EditorPosition = new Vector2(x, y);
            var nextNodes = new System.Collections.Generic.List<ConversationNode>();
            if (node.NextNodeId > 0)
            {
                var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.NextNodeId);
                if (nextNode != null) nextNodes.Add(nextNode);
            }
            if (node.Options != null)
            {
                foreach (var option in node.Options)
                {
                    if (option.NextNodeId > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == option.NextNodeId);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                }
            }
            if (node.ConditionalBranches != null)
            {
                foreach (var branch in node.ConditionalBranches)
                {
                    if (branch.NextNodeIdTrue > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdTrue);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                    if (branch.NextNodeIdFalse > 0)
                    {
                        var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdFalse);
                        if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
                    }
                }
            }
            if (node.DefaultBranchNodeId > 0)
            {
                var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.DefaultBranchNodeId);
                if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
            }
            float nextY = y + autoLayoutSpacing;
            float currentX = x;
            if (nextNodes.Count > 1)
            {
                float totalWidth = (nextNodes.Count - 1) * autoLayoutVerticalSpacing;
                currentX = x - totalWidth / 2;
            }
            float maxX = currentX;
            foreach (var nextNode in nextNodes)
            {
                float branchMaxX = LayoutNodesVertical(nextNode, currentX, nextY, visited, levelPositions, level + 1);
                maxX = Mathf.Max(maxX, branchMaxX);
                currentX = branchMaxX + autoLayoutVerticalSpacing;
            }
            levelPositions[level] = Mathf.Max(levelPositions[level], maxX);
            return maxX;
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
            ApplyZoomFromConversationSettings();
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
            ApplyZoomFromConversationSettings();
            ConversationNodeUtility.EnsureStartNodeExists(conversationData);
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
            SaveEditorZoomSetting();
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

        private void MarkDirty()
        {
            if (conversationData == null) return;
            isDirty = true;
        }
        #endregion
    }
}