using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ConversationScheme;
using System.IO;
using Newtonsoft.Json;
using System.Xml;

namespace ConversationEditor
{
    /// <summary>
    /// Main editor window for visual conversation editing with node-based graph
    /// </summary>
    public class ConversationEditorWindow : EditorWindow
    {
        private ConversationData conversationData;
        private string currentFilePath;
        private bool isDirty = false;

        // View state
        private Vector2 panOffset = Vector2.zero;
        private float zoom = 1.0f;
        private const float minZoom = 0.5f;
        private const float maxZoom = 2.0f;

        // Selection and interaction
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

        // UI state
        private int selectedTab = 0;
        private Vector2 resourceScrollPos;
        private Vector2 nodeScrollPos;
        private Vector2 inspectorScrollPos;

        // Context menu
        private Vector2 contextMenuPosition;

        // Styles
        private GUIStyle nodeStyle;
        private GUIStyle nodeSelectedStyle;
        private GUIStyle startNodeStyle;
        private GUIStyle endNodeStyle;
        private GUIStyle functionNodeStyle;
        private GUIStyle optionNodeStyle;
        private GUIStyle conditionalNodeStyle;
        private GUIStyle nodeHeaderStyle;
        private bool stylesInitialized = false;

        // Grid
        private const float gridSpacing = 20f;
        private Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

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

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Basic node style (green for dialogue)
            nodeStyle = new GUIStyle("box");
            nodeStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.6f, 0.3f, 0.9f));
            nodeStyle.border = new RectOffset(4, 4, 4, 4);
            nodeStyle.padding = new RectOffset(8, 8, 8, 8);
            nodeStyle.alignment = TextAnchor.UpperLeft;
            nodeStyle.wordWrap = true;

            // Selected node style
            nodeSelectedStyle = new GUIStyle(nodeStyle);
            nodeSelectedStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.7f, 0.4f, 1f));

            // Start node style (blue)
            startNodeStyle = new GUIStyle(nodeStyle);
            startNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.2f, 0.4f, 0.8f, 0.9f));
            startNodeStyle.alignment = TextAnchor.MiddleCenter;
            startNodeStyle.fontSize = 16;
            startNodeStyle.fontStyle = FontStyle.Bold;

            // End node style (red)
            endNodeStyle = new GUIStyle(nodeStyle);
            endNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.8f, 0.2f, 0.2f, 0.9f));
            endNodeStyle.alignment = TextAnchor.MiddleCenter;
            endNodeStyle.fontSize = 16;
            endNodeStyle.fontStyle = FontStyle.Bold;

            // Function node style (white)
            functionNodeStyle = new GUIStyle(nodeStyle);
            functionNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.8f, 0.8f, 0.8f, 0.9f));

            // Option node style (light blue, rounded)
            optionNodeStyle = new GUIStyle(nodeStyle);
            optionNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.4f, 0.7f, 0.9f, 0.9f));

            // Conditional node style (yellow diamond)
            conditionalNodeStyle = new GUIStyle(nodeStyle);
            conditionalNodeStyle.normal.background = MakeTexture(2, 2, new Color(0.9f, 0.8f, 0.2f, 0.9f));

            // Node header style
            nodeHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
            nodeHeaderStyle.fontSize = 11;
            nodeHeaderStyle.normal.textColor = Color.white;

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

        private void OnGUI()
        {
            InitializeStyles();

            // Handle keyboard shortcuts
            HandleKeyboardShortcuts();

            // Top toolbar
            DrawToolbar();

            if (conversationData == null)
            {
                EditorGUILayout.HelpBox("No conversation file loaded. Create a new one or open an existing file.", MessageType.Info);
                return;
            }

            // Main content area with tabs
            selectedTab = GUILayout.Toolbar(selectedTab, new string[] { "Resources", "Conversation Graph" });

            if (selectedTab == 0)
            {
                DrawResourceManager();
            }
            else if (selectedTab == 1)
            {
                DrawConversationGraph();
            }
        }

        private void HandleKeyboardShortcuts()
        {
            Event e = Event.current;
            if (e.type == EventType.KeyDown)
            {
                // Ctrl+S to save
                if (e.control && e.keyCode == KeyCode.S)
                {
                    SaveConversation();
                    e.Use();
                }
                // Ctrl+N for new conversation
                else if (e.control && e.keyCode == KeyCode.N)
                {
                    CreateNewConversation();
                    e.Use();
                }
                // Delete key to delete selected node
                else if (e.keyCode == KeyCode.Delete && selectedNode != null)
                {
                    DeleteNode(selectedNode);
                    e.Use();
                }
                // F to frame selected node
                else if (e.keyCode == KeyCode.F && selectedNode != null)
                {
                    FrameNode(selectedNode);
                    e.Use();
                }
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                CreateNewConversation();
            }

            if (GUILayout.Button("Open", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                OpenConversationDialog();
            }

            GUI.enabled = conversationData != null;
            if (GUILayout.Button(isDirty ? "Save*" : "Save", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                SaveConversation();
            }

            if (GUILayout.Button("Save As", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                SaveConversationAs();
            }
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (conversationData != null)
            {
                GUILayout.Label(string.IsNullOrEmpty(currentFilePath) ? "Untitled" : Path.GetFileName(currentFilePath), EditorStyles.toolbarButton);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawResourceManager()
        {
            if (conversationData?.ResourceManager == null) return;

            resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos);

            EditorGUILayout.Space(10);

            // Scene Backgrounds
            EditorGUILayout.LabelField("Scene Backgrounds", EditorStyles.boldLabel);
            DrawResourceList(conversationData.ResourceManager.SceneBackgrounds, "Background");

            EditorGUILayout.Space(10);

            // Audio Backgrounds
            EditorGUILayout.LabelField("Audio Backgrounds", EditorStyles.boldLabel);
            DrawAudioBackgroundList(conversationData.ResourceManager.AudioBackgrounds);

            EditorGUILayout.Space(10);

            // Actors
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

        private void DrawConversationGraph()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;

            Rect graphRect = GUILayoutUtility.GetRect(position.width, position.height - 60);

            // Handle view panning and zooming
            HandleGraphInput(graphRect);

            // Draw grid
            DrawGrid(graphRect);

            // Begin zoomed area
            GUIUtility.ScaleAroundPivot(Vector2.one * zoom, graphRect.size / 2);

            // Draw connections first (so they appear behind nodes)
            DrawConnections();

            // Draw nodes
            DrawNodes();

            // Draw connection line when connecting
            if (isConnecting)
            {
                DrawConnectionLine();
            }

            // Reset scaling
            GUIUtility.ScaleAroundPivot(Vector2.one / zoom, graphRect.size / 2);

            // Draw inspector panel on the right
            DrawInspectorPanel();

            // Draw zoom slider
            DrawZoomControls();
        }

        private void HandleGraphInput(Rect graphRect)
        {
            Event e = Event.current;

            // Zoom with mouse wheel
            if (e.type == EventType.ScrollWheel && graphRect.Contains(e.mousePosition))
            {
                float zoomDelta = -e.delta.y * 0.05f;
                zoom = Mathf.Clamp(zoom + zoomDelta, minZoom, maxZoom);
                e.Use();
                Repaint();
            }

            // Pan view with middle mouse or Alt+left mouse
            if (e.type == EventType.MouseDown && (e.button == 2 || (e.button == 0 && e.alt)) && graphRect.Contains(e.mousePosition))
            {
                isDraggingView = true;
                dragStartPos = e.mousePosition;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && isDraggingView)
            {
                panOffset += e.delta / zoom;
                e.Use();
                Repaint();
            }
            else if (e.type == EventType.MouseUp && isDraggingView)
            {
                isDraggingView = false;
                e.Use();
            }

            // Right-click context menu
            if (e.type == EventType.ContextClick && graphRect.Contains(e.mousePosition))
            {
                contextMenuPosition = (e.mousePosition - panOffset) / zoom;
                ShowContextMenu();
                e.Use();
            }
        }

        private void DrawGrid(Rect rect)
        {
            Handles.BeginGUI();

            float spacing = gridSpacing * zoom;
            int widthDivs = Mathf.CeilToInt(rect.width / spacing);
            int heightDivs = Mathf.CeilToInt(rect.height / spacing);

            Vector2 offset = new Vector2(panOffset.x % spacing, panOffset.y % spacing);

            Handles.color = gridColor;

            for (int i = 0; i <= widthDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(spacing * i + offset.x, 0),
                    new Vector3(spacing * i + offset.x, rect.height)
                );
            }

            for (int i = 0; i <= heightDivs; i++)
            {
                Handles.DrawLine(
                    new Vector3(0, spacing * i + offset.y),
                    new Vector3(rect.width, spacing * i + offset.y)
                );
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
            Rect nodeRect = new Rect(
                node.EditorPosition.x + panOffset.x,
                node.EditorPosition.y + panOffset.y,
                node.EditorSize.x,
                node.EditorSize.y
            );

            // Choose style based on node type
            GUIStyle style = GetNodeStyle(node);
            bool isSelected = selectedNode == node;

            if (isSelected && node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
            {
                style = nodeSelectedStyle;
            }

            // Draw node box
            GUI.Box(nodeRect, "", style);

            // Draw node content
            GUILayout.BeginArea(nodeRect);
            DrawNodeContent(node);
            GUILayout.EndArea();

            // Handle node interaction
            HandleNodeInteraction(node, nodeRect);

            // Draw options if present
            if (node.Options != null && node.Options.Count > 0)
            {
                DrawNodeOptions(node, nodeRect);
            }

            // Draw conditional branches if present
            if (node.ConditionalBranches != null && node.ConditionalBranches.Count > 0)
            {
                DrawConditionalBranches(node, nodeRect);
            }

            // Draw resize handle
            if (isSelected && node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
            {
                DrawResizeHandle(node, nodeRect);
            }
        }

        private GUIStyle GetNodeStyle(ConversationNode node)
        {
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    return startNodeStyle;
                case ConversationNodeType.End:
                    return endNodeStyle;
                case ConversationNodeType.Function:
                    return functionNodeStyle;
                case ConversationNodeType.Conditional:
                    return conditionalNodeStyle;
                default:
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

                default:
                    // ID
                    GUILayout.Label($"ID: {node.Id}", nodeHeaderStyle);

                    // Actor
                    if (!string.IsNullOrEmpty(node.SpeakerActorId))
                    {
                        var actor = conversationData.ResourceManager.Actors.FirstOrDefault(a => a.Id == node.SpeakerActorId);
                        if (actor != null && !string.IsNullOrEmpty(actor.IconPath))
                        {
                            // Try to load and display icon
                            GUILayout.Label($"?? {node.SpeakerActorId}");
                        }
                        else
                        {
                            GUILayout.Label($"Actor: {node.SpeakerActorId}");
                        }
                    }

                    // Text preview
                    if (!string.IsNullOrEmpty(node.Text))
                    {
                        string preview = node.Text.Length > 100 ? node.Text.Substring(0, 100) + "..." : node.Text;
                        GUILayout.Label(preview, EditorStyles.wordWrappedLabel);
                    }
                    break;
            }
        }

        private void HandleNodeInteraction(ConversationNode node, Rect nodeRect)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && nodeRect.Contains(e.mousePosition))
            {
                if (e.button == 0) // Left click
                {
                    if (e.control || e.command)
                    {
                        // Start connecting
                        isConnecting = true;
                        connectingFromNode = node;
                        connectingFromOption = null;
                        connectingFromBranch = null;
                        e.Use();
                    }
                    else
                    {
                        // Select node
                        selectedNode = node;
                        selectedOption = null;
                        selectedBranch = null;
                        GUI.FocusControl(null);
                        e.Use();
                        Repaint();
                    }
                }
                else if (e.button == 1) // Right click
                {
                    if (node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
                    {
                        ShowNodeContextMenu(node);
                        e.Use();
                    }
                }
            }

            // Drag node
            if (e.type == EventType.MouseDrag && selectedNode == node && !isConnecting && e.button == 0)
            {
                Undo.RecordObject(this, "Move Node");
                node.EditorPosition += e.delta / zoom;
                MarkDirty();
                e.Use();
                Repaint();
            }

            // Complete connection
            if (e.type == EventType.MouseUp && e.button == 0 && isConnecting && nodeRect.Contains(e.mousePosition))
            {
                CompleteConnection(node);
                e.Use();
            }
        }

        private void DrawNodeOptions(ConversationNode node, Rect nodeRect)
        {
            float optionHeight = 60f;
            float optionWidth = 150f;
            float spacing = 10f;

            for (int i = 0; i < node.Options.Count; i++)
            {
                var option = node.Options[i];
                Rect optionRect = new Rect(
                    nodeRect.xMax + spacing,
                    nodeRect.y + i * (optionHeight + spacing),
                    optionWidth,
                    optionHeight
                );

                // Draw option node
                GUI.Box(optionRect, "", optionNodeStyle);

                GUILayout.BeginArea(optionRect);
                GUILayout.Label($"Option {i + 1}", EditorStyles.boldLabel);
                GUILayout.Label(string.IsNullOrEmpty(option.Text) ? "(empty)" : 
                    (option.Text.Length > 20 ? option.Text.Substring(0, 20) + "..." : option.Text));
                GUILayout.EndArea();

                // Handle option interaction
                HandleOptionInteraction(node, option, optionRect, i);

                // Draw connection line from option
                DrawConnectionFromOption(node, option, optionRect);
            }
        }

        private void HandleOptionInteraction(ConversationNode node, ConversationOption option, Rect optionRect, int index)
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && optionRect.Contains(e.mousePosition))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    // Start connecting from option
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

            // Complete connection to option
            if (e.type == EventType.MouseUp && e.button == 0 && isConnecting && 
                connectingFromOption == null && optionRect.Contains(e.mousePosition))
            {
                // Cannot connect TO an option, only FROM an option
                isConnecting = false;
                e.Use();
            }
        }

        private void DrawConditionalBranches(ConversationNode node, Rect nodeRect)
        {
            // For conditional nodes, draw branch outputs
            if (node.NodeType != ConversationNodeType.Conditional) return;

            float branchHeight = 40f;
            float branchWidth = 100f;
            float spacing = 10f;

            for (int i = 0; i < node.ConditionalBranches.Count; i++)
            {
                var branch = node.ConditionalBranches[i];

                // Draw TRUE branch
                Rect trueRect = new Rect(
                    nodeRect.xMax + spacing,
                    nodeRect.y + i * (branchHeight * 2 + spacing),
                    branchWidth,
                    branchHeight
                );

                GUI.Box(trueRect, "", optionNodeStyle);
                GUILayout.BeginArea(trueRect);
                GUILayout.Label($"Branch {i + 1}: TRUE", EditorStyles.boldLabel);
                GUILayout.EndArea();

                // Draw FALSE branch
                Rect falseRect = new Rect(
                    nodeRect.xMax + spacing,
                    nodeRect.y + i * (branchHeight * 2 + spacing) + branchHeight + 5,
                    branchWidth,
                    branchHeight
                );

                GUI.Box(falseRect, "", optionNodeStyle);
                GUILayout.BeginArea(falseRect);
                GUILayout.Label($"Branch {i + 1}: FALSE", EditorStyles.boldLabel);
                GUILayout.EndArea();

                // Handle branch interaction
                HandleBranchInteraction(node, branch, trueRect, falseRect, i);

                // Draw connections from branches
                DrawConnectionFromBranch(node, branch, trueRect, falseRect);
            }
        }

        private void HandleBranchInteraction(ConversationNode node, ConditionalBranch branch, 
            Rect trueRect, Rect falseRect, int index)
        {
            Event e = Event.current;

            // Handle TRUE branch
            if (e.type == EventType.MouseDown && trueRect.Contains(e.mousePosition))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = branch;
                    connectingBranchIndex = 0; // TRUE
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

            // Handle FALSE branch
            if (e.type == EventType.MouseDown && falseRect.Contains(e.mousePosition))
            {
                if (e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = branch;
                    connectingBranchIndex = 1; // FALSE
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
            EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeUpLeft);

            Event e = Event.current;
            if (e.type == EventType.MouseDown && handleRect.Contains(e.mousePosition))
            {
                // Start resizing (not implemented in detail for brevity)
                e.Use();
            }
        }

        private void DrawConnections()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;

            Handles.BeginGUI();

            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                // Draw main NextNodeId connection
                if (node.NextNodeId > 0)
                {
                    var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.NextNodeId);
                    if (targetNode != null)
                    {
                        DrawConnection(node.EditorPosition + node.EditorSize / 2 + panOffset,
                                     targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2) + panOffset,
                                     Color.white);
                    }
                }

                // Draw option connections
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
                                ) + panOffset;

                                DrawConnection(optionPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2) + panOffset,
                                             Color.cyan);
                            }
                        }
                    }
                }

                // Draw conditional branch connections
                if (node.ConditionalBranches != null)
                {
                    float branchHeight = 40f;
                    float branchWidth = 100f;
                    float spacing = 10f;

                    for (int i = 0; i < node.ConditionalBranches.Count; i++)
                    {
                        var branch = node.ConditionalBranches[i];

                        // TRUE branch
                        if (branch.NextNodeIdTrue > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdTrue);
                            if (targetNode != null)
                            {
                                Vector2 branchPos = new Vector2(
                                    node.EditorPosition.x + node.EditorSize.x + spacing + branchWidth / 2,
                                    node.EditorPosition.y + i * (branchHeight * 2 + spacing) + branchHeight / 2
                                ) + panOffset;

                                DrawConnection(branchPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2) + panOffset,
                                             Color.green);
                            }
                        }

                        // FALSE branch
                        if (branch.NextNodeIdFalse > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdFalse);
                            if (targetNode != null)
                            {
                                Vector2 branchPos = new Vector2(
                                    node.EditorPosition.x + node.EditorSize.x + spacing + branchWidth / 2,
                                    node.EditorPosition.y + i * (branchHeight * 2 + spacing) + branchHeight * 1.5f + 5
                                ) + panOffset;

                                DrawConnection(branchPos,
                                             targetNode.EditorPosition + new Vector2(0, targetNode.EditorSize.y / 2) + panOffset,
                                             Color.red);
                            }
                        }
                    }
                }
            }

            Handles.EndGUI();
        }

        private void DrawConnection(Vector2 start, Vector2 end, Color color)
        {
            Handles.color = color;
            Vector2 startTangent = start + Vector2.right * 50;
            Vector2 endTangent = end + Vector2.left * 50;
            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 3f);

            // Draw arrow at end
            Vector2 direction = (end - endTangent).normalized;
            Vector2 arrowPoint1 = end - direction * 10 + new Vector2(-direction.y, direction.x) * 5;
            Vector2 arrowPoint2 = end - direction * 10 - new Vector2(-direction.y, direction.x) * 5;
            Handles.DrawAAPolyLine(3f, end, arrowPoint1);
            Handles.DrawAAPolyLine(3f, end, arrowPoint2);
        }

        private void DrawConnectionFromOption(ConversationNode node, ConversationOption option, Rect optionRect)
        {
            // Already handled in DrawConnections
        }

        private void DrawConnectionFromBranch(ConversationNode node, ConditionalBranch branch, Rect trueRect, Rect falseRect)
        {
            // Already handled in DrawConnections
        }

        private void DrawConnectionLine()
        {
            if (!isConnecting) return;

            Vector2 startPos = Vector2.zero;

            if (connectingFromOption != null)
            {
                // Find option position
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
                    ) + panOffset;
                }
            }
            else if (connectingFromBranch != null)
            {
                // Find branch position
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
                    ) + panOffset;
                }
            }
            else
            {
                startPos = connectingFromNode.EditorPosition + connectingFromNode.EditorSize / 2 + panOffset;
            }

            Vector2 endPos = Event.current.mousePosition;

            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Handles.DrawLine(startPos, endPos);
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

        private void DrawInspectorPanel()
        {
            Rect inspectorRect = new Rect(position.width - 300, 60, 300, position.height - 60);
            GUILayout.BeginArea(inspectorRect, "", "box");

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

            GUILayout.EndArea();
        }

        private void DrawNodeInspector(ConversationNode node)
        {
            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ID (read-only)
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("ID", node.Id);
            EditorGUI.EndDisabledGroup();

            // Node Type (read-only for Start/End)
            EditorGUI.BeginDisabledGroup(node.NodeType == ConversationNodeType.Start || node.NodeType == ConversationNodeType.End);
            node.NodeType = (ConversationNodeType)EditorGUILayout.EnumPopup("Node Type", node.NodeType);
            EditorGUI.EndDisabledGroup();

            if (node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
            {
                // Speaker Actor
                if (conversationData.ResourceManager.Actors.Count > 0)
                {
                    var actorIds = conversationData.ResourceManager.Actors.Select(a => a.Id).ToList();
                    actorIds.Insert(0, "(None)");
                    int currentIndex = string.IsNullOrEmpty(node.SpeakerActorId) ? 0 : 
                        actorIds.IndexOf(node.SpeakerActorId);
                    if (currentIndex < 0) currentIndex = 0;

                    int newIndex = EditorGUILayout.Popup("Speaker Actor", currentIndex, actorIds.ToArray());
                    node.SpeakerActorId = newIndex == 0 ? "" : actorIds[newIndex];
                }
                else
                {
                    node.SpeakerActorId = EditorGUILayout.TextField("Speaker Actor ID", node.SpeakerActorId);
                }

                // Text
                EditorGUILayout.LabelField("Text:");
                node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.MinHeight(60));

                // Next Node ID
                node.NextNodeId = EditorGUILayout.IntField("Next Node ID", node.NextNodeId);

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
                        EditorGUILayout.LabelField($"Option {i + 1}");
                        node.Options[i].Text = EditorGUILayout.TextField("Text", node.Options[i].Text);
                        node.Options[i].NextNodeId = EditorGUILayout.IntField("Next Node ID", node.Options[i].NextNodeId);

                        if (GUILayout.Button("Remove Option"))
                        {
                            Undo.RecordObject(this, "Remove Option");
                            node.Options.RemoveAt(i);
                            MarkDirty();
                            break;
                        }
                        EditorGUILayout.EndVertical();
                    }

                    if (GUILayout.Button("Add Option"))
                    {
                        Undo.RecordObject(this, "Add Option");
                        node.Options.Add(new ConversationOption());
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
                        branch.NextNodeIdTrue = EditorGUILayout.IntField("Next Node (True)", branch.NextNodeIdTrue);
                        branch.NextNodeIdFalse = EditorGUILayout.IntField("Next Node (False)", branch.NextNodeIdFalse);

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

                    if (GUILayout.Button("Add Branch"))
                    {
                        Undo.RecordObject(this, "Add Branch");
                        node.ConditionalBranches.Add(new ConditionalBranch());
                        MarkDirty();
                    }

                    // Default Branch
                    node.DefaultBranchNodeId = EditorGUILayout.IntField("Default Branch Node ID", node.DefaultBranchNodeId);
                }
            }

            // Editor Position and Size
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Editor Properties", EditorStyles.boldLabel);
            node.EditorPosition = EditorGUILayout.Vector2Field("Position", node.EditorPosition);
            node.EditorSize = EditorGUILayout.Vector2Field("Size", node.EditorSize);
        }

        private void DrawOptionInspector(ConversationOption option)
        {
            EditorGUILayout.LabelField("Option Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            option.Text = EditorGUILayout.TextField("Text", option.Text);
            option.NextNodeId = EditorGUILayout.IntField("Next Node ID", option.NextNodeId);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(option.Conditions);
        }

        private void DrawBranchInspector(ConditionalBranch branch)
        {
            EditorGUILayout.LabelField("Branch Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            branch.NextNodeIdTrue = EditorGUILayout.IntField("Next Node (True)", branch.NextNodeIdTrue);
            branch.NextNodeIdFalse = EditorGUILayout.IntField("Next Node (False)", branch.NextNodeIdFalse);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(branch.Conditions);
        }

        private void DrawConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;

            for (int i = 0; i < conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var condition = conditions[i];

                condition.VariableName = EditorGUILayout.TextField("Variable", condition.VariableName);
                condition.Operator = (ComparisonOperator)EditorGUILayout.EnumPopup("Operator", condition.Operator);
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup("Value Type", condition.ValueDataType);
                condition.Value = EditorGUILayout.TextField("Value", condition.Value);
                condition.IsValueVariable = EditorGUILayout.Toggle("Is Value Variable", condition.IsValueVariable);

                if (GUILayout.Button("Remove Condition"))
                {
                    Undo.RecordObject(this, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Condition"))
            {
                Undo.RecordObject(this, "Add Condition");
                conditions.Add(new ConditionRule());
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

                // Predefined function dropdown
                string[] predefinedFunctions = ConversationFunctionLibrary.GetFunctionNames();
                int currentIndex = System.Array.IndexOf(predefinedFunctions, func.MethodName);
                if (currentIndex < 0) currentIndex = predefinedFunctions.Length - 1; // "Custom"

                int newIndex = EditorGUILayout.Popup("Function", currentIndex, predefinedFunctions);
                string selectedFunction = predefinedFunctions[newIndex];

                if (selectedFunction == "Custom")
                {
                    func.MethodName = EditorGUILayout.TextField("Method Name", func.MethodName);
                }
                else
                {
                    func.MethodName = selectedFunction;
                }

                // Parameters
                var paramDef = ConversationFunctionLibrary.GetFunctionParameters(func.MethodName);
                if (paramDef != null && paramDef.Count > 0)
                {
                    EditorGUILayout.LabelField("Parameters:", EditorStyles.boldLabel);

                    if (func.Parameters == null)
                        func.Parameters = new Dictionary<string, string>();

                    foreach (var param in paramDef)
                    {
                        if (!func.Parameters.ContainsKey(param.Key))
                            func.Parameters[param.Key] = "";

                        func.Parameters[param.Key] = EditorGUILayout.TextField(param.Key, func.Parameters[param.Key]);
                    }
                }
                else
                {
                    // Custom parameters
                    EditorGUILayout.LabelField("Parameters (key=value):");
                    if (func.Parameters == null)
                        func.Parameters = new Dictionary<string, string>();

                    var keys = func.Parameters.Keys.ToList();
                    foreach (var key in keys)
                    {
                        EditorGUILayout.BeginHorizontal();
                        string newValue = EditorGUILayout.TextField(key, func.Parameters[key]);
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

                    if (GUILayout.Button("Add Parameter"))
                    {
                        func.Parameters["newParam"] = "";
                        MarkDirty();
                    }
                }

                func.Timestamp = EditorGUILayout.IntField("Timestamp", func.Timestamp);

                if (GUILayout.Button("Remove Function"))
                {
                    Undo.RecordObject(this, "Remove Function");
                    functions.RemoveAt(i);
                    MarkDirty();
                    break;
                }

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Function"))
            {
                Undo.RecordObject(this, "Add Function");
                functions.Add(new ConversationFunction());
                MarkDirty();
            }
        }

        private void DrawZoomControls()
        {
            Rect zoomRect = new Rect(10, position.height - 40, 200, 20);
            GUILayout.BeginArea(zoomRect);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Zoom: {(zoom * 100):F0}%", GUILayout.Width(80));
            float newZoom = GUILayout.HorizontalSlider(zoom, minZoom, maxZoom);
            if (newZoom != zoom)
            {
                zoom = newZoom;
                Repaint();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Create Node/Dialogue"), false, () => CreateNode(ConversationNodeType.Dialogue));
            menu.AddItem(new GUIContent("Create Node/Function"), false, () => CreateNode(ConversationNodeType.Function));
            menu.AddItem(new GUIContent("Create Node/Dialogue with Options"), false, () => CreateNodeWithOptions());
            menu.AddItem(new GUIContent("Create Node/Conditional"), false, () => CreateNode(ConversationNodeType.Conditional));
            menu.ShowAsContext();
        }

        private void ShowNodeContextMenu(ConversationNode node)
        {
            GenericMenu menu = new GenericMenu();

            if (node.NodeType == ConversationNodeType.Dialogue)
            {
                menu.AddItem(new GUIContent("Add Option"), false, () => {
                    Undo.RecordObject(this, "Add Option");
                    if (node.Options == null) node.Options = new List<ConversationOption>();
                    node.Options.Add(new ConversationOption());
                    MarkDirty();
                });
            }

            menu.AddItem(new GUIContent("Duplicate Node"), false, () => DuplicateNode(node));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete Node"), false, () => DeleteNode(node));

            menu.ShowAsContext();
        }

        private void ShowOptionContextMenu(ConversationNode node, ConversationOption option, int index)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate Option"), false, () => {
                Undo.RecordObject(this, "Duplicate Option");
                var newOption = new ConversationOption
                {
                    Text = option.Text,
                    NextNodeId = 0,
                    Conditions = new List<ConditionRule>(option.Conditions)
                };
                node.Options.Insert(index + 1, newOption);
                MarkDirty();
            });
            menu.AddItem(new GUIContent("Delete Option"), false, () => {
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

            if (EditorUtility.DisplayDialog("Delete Node",
                $"Are you sure you want to delete node {node.Id}?", "Delete", "Cancel"))
            {
                Undo.RecordObject(this, "Delete Node");
                ConversationNodeUtility.RemoveNodeReferences(node.Id, conversationData.ConversationManager.Nodes);
                conversationData.ConversationManager.Nodes.Remove(node);

                if (selectedNode == node)
                    selectedNode = null;

                MarkDirty();
                Repaint();
            }
        }

        private void FrameNode(ConversationNode node)
        {
            panOffset = -node.EditorPosition + new Vector2(position.width / 2, position.height / 2);
            Repaint();
        }

        private void CreateNewConversation()
        {
            if (isDirty)
            {
                int result = EditorUtility.DisplayDialogComplex("Unsaved Changes",
                    "You have unsaved changes. Do you want to save them?",
                    "Save", "Don't Save", "Cancel");

                if (result == 0) // Save
                {
                    SaveConversation();
                }
                else if (result == 2) // Cancel
                {
                    return;
                }
            }

            conversationData = new ConversationData();

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

            currentFilePath = null;
            isDirty = false;
            selectedNode = null;
            panOffset = Vector2.zero;
            zoom = 1.0f;

            Repaint();
        }

        private void OpenConversationDialog()
        {
            string path = EditorUtility.OpenFilePanel("Open Conversation", "Assets", "json");
            if (!string.IsNullOrEmpty(path))
            {
                LoadConversation(path);
            }
        }

        private void LoadConversation(string filePath)
        {
            if (isDirty)
            {
                int result = EditorUtility.DisplayDialogComplex("Unsaved Changes",
                    "You have unsaved changes. Do you want to save them?",
                    "Save", "Don't Save", "Cancel");

                if (result == 0) // Save
                {
                    SaveConversation();
                }
                else if (result == 2) // Cancel
                {
                    return;
                }
            }

            try
            {
                string json = File.ReadAllText(filePath);
                conversationData = ConversationJsonSettings.Deserialize<ConversationData>(json);

                if (conversationData == null)
                {
                    EditorUtility.DisplayDialog("Error", "Failed to load conversation file.", "OK");
                    return;
                }

                // Ensure Start node exists
                ConversationNodeUtility.EnsureStartNodeExists(conversationData);

                currentFilePath = filePath;
                isDirty = false;
                selectedNode = null;
                panOffset = Vector2.zero;
                zoom = 1.0f;

                Repaint();
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to load conversation: {ex.Message}", "OK");
            }
        }

        private void SaveConversation()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SaveConversationAs();
            }
            else
            {
                SaveToFile(currentFilePath);
            }
        }

        private void SaveConversationAs()
        {
            string path = EditorUtility.SaveFilePanel("Save Conversation", "Assets", "conversation", "json");
            if (!string.IsNullOrEmpty(path))
            {
                currentFilePath = path;
                SaveToFile(path);
            }
        }

        private void SaveToFile(string filePath)
        {
            try
            {
                // Fill in End node references for any nodes with NextNodeId = 0
                var endNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.End);
                if (endNode != null)
                {
                    foreach (var node in conversationData.ConversationManager.Nodes)
                    {
                        if (node.NodeType != ConversationNodeType.End && node.NextNodeId == 0 && 
                            (node.Options == null || node.Options.Count == 0) &&
                            (node.ConditionalBranches == null || node.ConditionalBranches.Count == 0))
                        {
                            node.NextNodeId = endNode.Id;
                        }
                    }
                }

                string json = ConversationJsonSettings.Serialize(conversationData);
                File.WriteAllText(filePath, json);

                isDirty = false;
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", "Conversation saved successfully!", "OK");
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog("Error", $"Failed to save conversation: {ex.Message}", "OK");
            }
        }

        private void MarkDirty()
        {
            isDirty = true;
        }
    }
}
