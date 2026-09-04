using System.Collections.Generic;
using System.Linq;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEditor
{
    using System.Collections.Generic;
    public class ConversationGraphView
    {
        #region Core Data
        private ConversationData conversationData;
        private readonly EditorWindow ownerWindow;
        private bool isReadOnly;
        #endregion

        #region View State
        private Vector2 panOffset = Vector2.zero;
        private float zoom = 1.0f;
        private const float minZoom = 0.1f;
        private const float maxZoom = 5.0f;
        private Rect currentGraphRect;
        #endregion

        #region Selection State
        private ConversationNode selectedNode;
        private ConversationOption selectedOption;
        private ConditionalBranch selectedBranch;
        #endregion

        #region Interaction State
        private Vector2 dragStartPos;
        private bool isDraggingView = false;
        private bool isConnecting = false;
        private ConversationNode connectingFromNode;
        private ConversationOption connectingFromOption;
        private ConditionalBranch connectingFromBranch;
        private int connectingBranchIndex = 0;
        private bool isMouseOverNode = false;
        private bool isRightClickMenuActive = false;
        private bool isNodeBeingDragged = false;
        private bool isMouseOverOption = false;
        private bool isOptionBeingDragged = false;
        private ConversationNode optionDragParentNode;
        private ConversationOption optionBeingDragged;
        #endregion

        #region Layout State
        private float autoLayoutSpacing = 250f;
        private float autoLayoutVerticalSpacing = 150f;
        private Vector2 contextMenuPosition;
        #endregion

        #region Constants
        private const float gridSpacing = 20f;
        private static readonly Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        private const float zoomControlScale = 1.5f;
        private const int minNodeFontSize = 8;
        private const int nodeHeaderBaseFontSize = 11;
        private const int nodeBodyBaseFontSize = 12;
        private const float optionDefaultWidth = 150f;
        private const float optionDefaultHeight = 60f;
        private const float optionDefaultSpacing = 10f;
        private const float minEditorNodeSize = 20f;
        private const float nodeHorizontalPadding = 16f;
        private const float nodeVerticalPadding = 12f;
        private const float estimatedLineSpacing = 3f;
        #endregion

        #region
        private ConversationNodeStyle conversationNodeStyle;
        #endregion

        #region Events
        public System.Action OnDirty;
        public System.Action OnSelectionChanged;
        public System.Action OnRepaintRequested;
        #endregion

        #region Public Props
        public ConversationNode SelectedNode => selectedNode;
        public ConversationOption SelectedOption => selectedOption;
        public ConditionalBranch SelectedBranch => selectedBranch;
        public bool HasSelection => selectedNode != null || selectedOption != null || selectedBranch != null;
        public bool IsReadOnly => isReadOnly;
        #endregion

        #region Public API
        public ConversationGraphView(EditorWindow ownerWindow, bool isReadOnly = false)
        {
            this.ownerWindow = ownerWindow;
            this.isReadOnly = isReadOnly;
        }
        public void SetConversationData(ConversationData data)
        {
            conversationData = data;
            EnsureEditorSettings();
            ApplyZoomFromConversationSettings();
            EnsureMinimumEditorSizesInConversation();
            EnsureOptionEditorDataInConversation();
            ClearSelection();
            panOffset = Vector2.zero;
        }
        public void Draw()
        {
            Rect graphRect = CalculateGraphRect();
            Draw(graphRect);
        }
        public void Draw(Rect graphRect)
        {
            conversationNodeStyle = ConversationNodeStyle.GetSingleton();
            if (conversationData?.ConversationManager?.Nodes == null) return;
            currentGraphRect = graphRect;
            HandleGraphInput(graphRect);
            GUI.Box(graphRect, GUIContent.none);
            GUI.BeginGroup(graphRect);
            Rect localRect = new Rect(0, 0, graphRect.width, graphRect.height);
            DrawGrid(localRect);
            DrawConnections();
            DrawNodes();
            if (isConnecting && !isReadOnly) DrawConnectionLine();
            DrawZoomControls(localRect);
            GUI.EndGroup();
        }
        public void ShowAutoLayoutMenu()
        {
            if (isReadOnly) return;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Horizontal"), false, () => AutoLayoutNodes(true));
            menu.AddItem(new GUIContent("Vertical"), false, () => AutoLayoutNodes(false));
            menu.ShowAsContext();
        }
        public void FrameAllNodes(Rect graphRect)
        {
            if (conversationData?.ConversationManager?.Nodes == null || conversationData.ConversationManager.Nodes.Count == 0) return;
            Rect bounds = GetConversationBounds();
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            currentGraphRect = graphRect;
            float padding = 40f;
            float availableWidth = Mathf.Max(1f, graphRect.width - padding * 2f);
            float availableHeight = Mathf.Max(1f, graphRect.height - padding * 2f);
            float zoomX = availableWidth / bounds.width;
            float zoomY = availableHeight / bounds.height;
            zoom = Mathf.Clamp(Mathf.Min(zoomX, zoomY), minZoom, maxZoom);
            Vector2 graphCenter = new Vector2(graphRect.width, graphRect.height) * 0.5f;
            panOffset = graphCenter / zoom - bounds.center;
            if (!isReadOnly) SaveEditorZoomSetting();
        }

        public void SetReadOnlyMode(bool readOnly)
        {
            isReadOnly = readOnly;
            if (isReadOnly)
            {
                isConnecting = false;
                connectingFromNode = null;
                connectingFromOption = null;
                connectingFromBranch = null;
            }
        }

        public void DeleteSelectedNode()
        {
            if (isReadOnly || selectedNode == null) return;
            DeleteNode(selectedNode);
        }

        public void FrameSelectedNode()
        {
            if (selectedNode == null) return;
            FrameNode(selectedNode);
        }

        public void HandleEscapeAction()
        {
            if (isConnecting)
            {
                isConnecting = false;
                connectingFromNode = null;
                connectingFromOption = null;
                connectingFromBranch = null;
                RequestRepaint();
                return;
            }
            if (HasSelection) ClearSelection();
        }

        public bool IsInConnectionMode()
        {
            return isConnecting;
        }
        #endregion

        #region Drawing
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

        private Vector2 WindowToGraphLocal(Vector2 windowPos)
        {
            return windowPos - currentGraphRect.position;
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
            Rect nodeWorldRect = GetNodeWorldRect(node);
            Rect nodeRect = WorldToGraphRect(nodeWorldRect);
            GUIStyle style = GetNodeStyle(node);
            if (node.NodeType == ConversationNodeType.Conditional)
            {
                // Draw diamond shape for conditional node
                Vector2 center = node.EditorPosition;
                float hx = node.EditorSize.x * 0.5f;
                float hy = node.EditorSize.y * 0.5f;
                Vector3[] points = new Vector3[4];
                points[0] = WorldToGraph(new Vector2(center.x - hx, center.y)); // left
                points[1] = WorldToGraph(new Vector2(center.x, center.y - hy)); // top
                points[2] = WorldToGraph(new Vector2(center.x + hx, center.y)); // right
                points[3] = WorldToGraph(new Vector2(center.x, center.y + hy)); // bottom
                Handles.BeginGUI();
                Handles.color = new Color(0.9f, 0.8f, 0.2f, 0.9f);
                Handles.DrawAAConvexPolygon(points);
                Handles.color = Color.black;
                Handles.DrawAAPolyLine(3f, points[0], points[1], points[2], points[3], points[0]);
                Handles.EndGUI();
                // draw label
                GUILayout.BeginArea(nodeRect);
                GUILayout.FlexibleSpace();
                GUILayout.Label("Conditional", conversationNodeStyle.nodeHeaderStyle);
                GUILayout.FlexibleSpace();
                GUILayout.EndArea();
            }
            else
            {
                GUI.Box(nodeRect, "", style);
                GUILayout.BeginArea(nodeRect);
                DrawNodeContent(node);
                GUILayout.EndArea();
            }
            HandleNodeInteraction(node, nodeRect);
            if (node.Options != null && node.Options.Count > 0) DrawNodeOptions(node, nodeRect);
            if (node.NodeType == ConversationNodeType.Conditional) DrawConditionalIndicators(node, nodeRect);
            if (!isReadOnly && selectedNode == node && node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
                DrawResizeHandle(node, nodeRect);
        }

        private void DrawNodeContent(ConversationNode node)
        {
            conversationNodeStyle.nodeHeaderStyle.fontSize = GetScaledNodeFontSize(nodeHeaderBaseFontSize);
            conversationNodeStyle.nodeBodyTextStyle.fontSize = GetScaledNodeFontSize(nodeBodyBaseFontSize);
            conversationNodeStyle.nodeActorTextStyle.fontSize = GetScaledNodeFontSize(nodeBodyBaseFontSize);
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("START", conversationNodeStyle.nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                case ConversationNodeType.End:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("END", conversationNodeStyle.nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                case ConversationNodeType.Function:
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("FUNCTION", conversationNodeStyle.nodeHeaderStyle);
                    GUILayout.FlexibleSpace();
                    break;
                default:
                    GUILayout.Label($"ID: {node.Id}", conversationNodeStyle.nodeHeaderStyle);
                    if (!string.IsNullOrEmpty(node.SpeakerActorId)) GUILayout.Label($"Actor: {node.SpeakerActorId}", conversationNodeStyle.nodeActorTextStyle);
                    if (!string.IsNullOrEmpty(node.Text))
                    {
                        int previewLength = GetNodePreviewTextLength(node, !string.IsNullOrEmpty(node.SpeakerActorId));
                        string preview = BuildPreviewText(node.Text, previewLength);
                        GUILayout.Label(preview, conversationNodeStyle.nodeBodyTextStyle);
                    }
                    break;
            }
        }
        #endregion

        #region Input
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
                    if (!isReadOnly) SaveEditorZoomSetting();
                }
                e.Use();
                RequestRepaint();
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
                Vector2 mouseWorldPos = WindowToWorld(e.mousePosition);
                if (IsPointerOverInteractiveElement(mouseWorldPos)) return;
                if (isConnecting)
                {
                    isConnecting = false;
                    connectingFromNode = null;
                    connectingFromOption = null;
                    connectingFromBranch = null;
                    e.Use();
                    RequestRepaint();
                    return;
                }
                isDraggingView = true;
                dragStartPos = e.mousePosition;
                e.Use();
                RequestRepaint();
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
                RequestRepaint();
                return;
            }
            if (e.type == EventType.MouseUp)
            {
                isDraggingView = false;
                return;
            }
            if (isReadOnly) return;
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                Vector2 mouseWorldPos = WindowToWorld(e.mousePosition);
                if (IsPointerOverInteractiveElement(mouseWorldPos)) return;
                contextMenuPosition = mouseWorldPos;
                ShowContextMenu();
                e.Use();
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
                    if (!isReadOnly && isConnecting)
                    {
                        if (node.NodeType != ConversationNodeType.Start && node != connectingFromNode) CompleteConnection(node);
                        else
                        {
                            isConnecting = false;
                            connectingFromNode = null;
                            connectingFromOption = null;
                            connectingFromBranch = null;
                        }
                        e.Use();
                        RequestRepaint();
                        return;
                    }
                    SetSelection(node, null, null);
                    isMouseOverNode = true;
                    isNodeBeingDragged = false;
                    GUI.FocusControl(null);
                    e.Use();
                    RequestRepaint();
                }
                else if (!isReadOnly && e.button == 1)
                {
                    if (node.NodeType != ConversationNodeType.Start && node.NodeType != ConversationNodeType.End)
                    {
                        SetSelection(node, null, null);
                        ShowNodeContextMenu(node);
                        e.Use();
                    }
                }
            }
            if (isReadOnly) return;
            if (e.type == EventType.MouseDrag && selectedNode == node && !isConnecting && e.button == 0 && isMouseOverNode)
            {
                if (!isNodeBeingDragged) isNodeBeingDragged = true;
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Move Node");
                node.EditorPosition += e.delta / zoom;
                node.EditorPosition.x = Mathf.Max(0, Mathf.Min(10000, node.EditorPosition.x));
                node.EditorPosition.y = Mathf.Max(0, Mathf.Min(10000, node.EditorPosition.y));
                MarkDirty();
                e.Use();
                RequestRepaint();
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                isMouseOverNode = false;
                if (isNodeBeingDragged)
                {
                    isNodeBeingDragged = false;
                    RequestRepaint();
                }
            }
        }
        #endregion

        #region Options Branches
        private void DrawNodeOptions(ConversationNode node, Rect nodeRect)
        {
            for (int i = 0; i < node.Options.Count; i++)
            {
                var option = node.Options[i];
                Rect optionWorldRect = GetOptionWorldRect(node, option, i);
                Rect optionRect = WorldToGraphRect(optionWorldRect);
                GUIStyle optionStyle = GetOptionStyle(option);
                GUI.Box(optionRect, GUIContent.none, optionStyle);
                Rect optionContentRect = new Rect(optionRect.x + 8f, optionRect.y + 6f, optionRect.width - 16f, optionRect.height - 12f);
                GUILayout.BeginArea(optionContentRect);
                GUILayout.Label(new GUIContent($"Option {i + 1}", "Option node index inside the parent dialogue node."), EditorStyles.boldLabel);
                int maxPreviewLength = GetOptionPreviewTextLength(option);
                string optionPreview = BuildPreviewText(option.Text, maxPreviewLength, "(empty)");
                GUILayout.Label(new GUIContent(optionPreview, "Option text preview sized to the current option node dimensions."));
                GUILayout.EndArea();
                HandleOptionInteraction(node, option, optionRect, i);
            }
        }

        private void HandleOptionInteraction(ConversationNode node, ConversationOption option, Rect optionRect, int index)
        {
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);
            if (e.type == EventType.MouseDown && optionRect.Contains(mouseGraphPos))
            {
                if (!isReadOnly && e.button == 0 && (e.control || e.command))
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = option;
                    connectingFromBranch = null;
                    e.Use();
                    return;
                }
                if (e.button == 0)
                {
                    SetSelection(node, option, null);
                    isMouseOverNode = false;
                    isMouseOverOption = true;
                    isOptionBeingDragged = false;
                    optionDragParentNode = node;
                    optionBeingDragged = option;
                    e.Use();
                    RequestRepaint();
                    return;
                }
                if (!isReadOnly && e.button == 1)
                {
                    SetSelection(node, option, null);
                    ShowOptionContextMenu(node, option, index);
                    e.Use();
                    return;
                }
            }
            if (isReadOnly) return;
            if (e.type == EventType.MouseDrag && selectedNode == node && selectedOption == option && !isConnecting && e.button == 0 && isMouseOverOption && optionDragParentNode == node && optionBeingDragged == option)
            {
                if (!isOptionBeingDragged) isOptionBeingDragged = true;
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Move Option Node");
                option.EditorPosition += e.delta / zoom;
                option.EditorPosition.x = Mathf.Clamp(option.EditorPosition.x, -10000f, 10000f);
                option.EditorPosition.y = Mathf.Clamp(option.EditorPosition.y, -10000f, 10000f);
                MarkDirty();
                e.Use();
                RequestRepaint();
                return;
            }
            if (e.type == EventType.MouseUp && e.button == 0)
            {
                if (isOptionBeingDragged) RequestRepaint();
                isOptionBeingDragged = false;
                isMouseOverOption = false;
                optionDragParentNode = null;
                optionBeingDragged = null;
            }
        }

        private void DrawConditionalIndicators(ConversationNode node, Rect nodeRect)
        {
            // Draw small interactive indicators to the left (true) and right (false) of the conditional diamond
            Vector2 center = node.EditorPosition;
            float indicatorSize = 16f;
            Rect trueWorldRect = new Rect(center.x - node.EditorSize.x * 0.5f - indicatorSize - 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
            Rect falseWorldRect = new Rect(center.x + node.EditorSize.x * 0.5f + 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
            Rect trueRect = WorldToGraphRect(trueWorldRect);
            Rect falseRect = WorldToGraphRect(falseWorldRect);
            GUI.Box(trueRect, "", conversationNodeStyle.optionNodeStyle);
            GUI.Box(falseRect, "", conversationNodeStyle.optionNodeStyle);
            GUILayout.BeginArea(trueRect);
            GUILayout.Label("T", EditorStyles.boldLabel);
            GUILayout.EndArea();
            GUILayout.BeginArea(falseRect);
            GUILayout.Label("F", EditorStyles.boldLabel);
            GUILayout.EndArea();
            HandleBranchInteraction(node, node.conditionalBranch, trueRect, falseRect);
        }

        private void HandleBranchInteraction(ConversationNode node, ConditionalBranch branch, Rect trueRect, Rect falseRect)
        {
            Event e = Event.current;
            Vector2 mouseGraphPos = WindowToGraphLocal(e.mousePosition);
            if (e.type == EventType.MouseDown && trueRect.Contains(mouseGraphPos))
            {
                if (!isReadOnly && e.button == 0 && (e.control || e.command))
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
                    SetSelection(node, null, branch);
                    e.Use();
                    RequestRepaint();
                }
            }
            if (e.type == EventType.MouseDown && falseRect.Contains(mouseGraphPos))
            {
                if (!isReadOnly && e.button == 0 && (e.control || e.command))
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
                    SetSelection(node, null, branch);
                    e.Use();
                    RequestRepaint();
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

        #region Connections
        private void DrawConnections()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;
            Handles.BeginGUI();
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                Rect nodeRect = GetNodeWorldRect(node);
                if (node.NextNodeId > 0)
                {
                    var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.NextNodeId);
                    if (targetNode != null)
                    {
                        Vector2 startMid = node.EditorPosition;
                        Vector2 targetPoint = GetNodeConnectionPoint(targetNode, startMid);
                        DrawConnection(startMid, targetPoint, Color.white);
                    }
                }
                if (node.Options != null)
                {
                    for (int i = 0; i < node.Options.Count; i++)
                    {
                        var option = node.Options[i];
                        Rect optionRect = GetOptionWorldRect(node, option, i);
                        // Start point should be on the right edge of the parent node and vertically
                        // aligned with the option center but clamped to the node's vertical bounds
                        float clampedY = Mathf.Clamp(optionRect.center.y, nodeRect.yMin, nodeRect.yMax);
                        Vector2 optionStart = new Vector2(nodeRect.xMax, clampedY);
                        Vector2 optionEnd = new Vector2(optionRect.xMin, optionRect.center.y);
                        DrawParentOptionLink(optionStart, optionEnd, Color.white);
                        if (option.NextNodeId > 0)
                        {
                            var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == option.NextNodeId);
                            if (targetNode != null)
                            {
                                Vector2 optionPos = new Vector2(optionRect.xMax, optionRect.center.y);
                                var targetPoint = GetNodeConnectionPoint(targetNode, optionPos);
                                DrawConnection(optionPos, targetPoint, Color.cyan);
                            }
                        }
                    }
                }
                if (node.NodeType == ConversationNodeType.Conditional && node.conditionalBranch != null)
                {
                    var branch = node.conditionalBranch;
                    Vector2 center = node.EditorPosition;
                    Vector2 leftPos = new Vector2(center.x - node.EditorSize.x * 0.5f, center.y);
                    Vector2 rightPos = new Vector2(center.x + node.EditorSize.x * 0.5f, center.y);
                    if (branch.NextNodeIdTrue > 0)
                    {
                        var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdTrue);
                        if (targetNode != null)
                        {
                            var targetPoint = GetNodeConnectionPoint(targetNode, leftPos);
                            DrawConnection(leftPos, targetPoint, Color.green);
                        }
                    }
                    if (branch.NextNodeIdFalse > 0)
                    {
                        var targetNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == branch.NextNodeIdFalse);
                        if (targetNode != null)
                        {
                            var targetPoint = GetNodeConnectionPoint(targetNode, rightPos);
                            DrawConnection(rightPos, targetPoint, Color.red);
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
            // Calculate a tangent based on the direction between points for a nicer curve
            Vector2 delta = end - start;
            Vector2 startTangent, endTangent;
            if (delta.sqrMagnitude < 0.0001f)
            {
                Vector2 fallback = Vector2.right * (50f * zoom);
                startTangent = start + fallback;
                endTangent = end - fallback;
            }
            else
            {
                float distance = delta.magnitude;
                float tangentLength = Mathf.Clamp(distance * 0.5f, 50f * zoom, 200f * zoom);
                Vector2 tangent = delta.normalized * tangentLength;
                startTangent = start + tangent;
                endTangent = end - tangent;
            }

            Handles.DrawBezier(start, end, startTangent, endTangent, color, null, 5f);

            Vector2 direction = (end - endTangent).normalized;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector2.down;
            Vector2 arrowPoint1 = end - direction * (10f * zoom) + new Vector2(-direction.y, direction.x) * (5f * zoom);
            Vector2 arrowPoint2 = end - direction * (10f * zoom) - new Vector2(-direction.y, direction.x) * (5f * zoom);
            Handles.DrawAAPolyLine(5f, end, arrowPoint1);
            Handles.DrawAAPolyLine(5f, end, arrowPoint2);
        }

        private void DrawConnectionLine()
        {
            if (!isConnecting || isReadOnly) return;
            Vector2 startPos = Vector2.zero;
            if (connectingFromOption != null)
            {
                var node = connectingFromNode;
                int optionIndex = node.Options.IndexOf(connectingFromOption);
                if (optionIndex >= 0)
                {
                    Rect optionRect = GetOptionWorldRect(node, connectingFromOption, optionIndex);
                    startPos = new Vector2(optionRect.xMax, optionRect.center.y);
                }
            }
            else if (connectingFromBranch != null)
            {
                var node = connectingFromNode;
                Vector2 center = node.EditorPosition;
                float hx = node.EditorSize.x * 0.5f;
                startPos = connectingBranchIndex == 0 ? new Vector2(center.x - hx, center.y) : new Vector2(center.x + hx, center.y);
            }
            else
            {
                startPos = connectingFromNode.EditorPosition;
            }
            Vector2 endPos = WindowToWorld(Event.current.mousePosition);
            Handles.BeginGUI();
            Handles.color = Color.yellow;
            Handles.DrawAAPolyLine(5f, WorldToGraph(startPos), WorldToGraph(endPos));
            Handles.EndGUI();
            RequestRepaint();
        }

        private void CompleteConnection(ConversationNode targetNode)
        {
            if (!isConnecting || connectingFromNode == null || isReadOnly) return;
            if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Create Connection");
            if (connectingFromOption != null) connectingFromOption.NextNodeId = targetNode.Id;
            else if (connectingFromBranch != null)
            {
                if (connectingBranchIndex == 0) connectingFromBranch.NextNodeIdTrue = targetNode.Id;
                else connectingFromBranch.NextNodeIdFalse = targetNode.Id;
            }
            else connectingFromNode.NextNodeId = targetNode.Id;
            MarkDirty();
            isConnecting = false;
            connectingFromNode = null;
            connectingFromOption = null;
            connectingFromBranch = null;
            RequestRepaint();
        }

        private void DrawParentOptionLink(Vector2 startWorld, Vector2 endWorld, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(3f, WorldToGraph(startWorld), WorldToGraph(endWorld));
        }

        private Vector2 GetNodeConnectionPoint(ConversationNode node, Vector2 fromWorld)
        {
            // Determine the best point on the node edge (midpoint of the chosen side)
            Rect nodeRect = GetNodeWorldRect(node);
            Vector2 center = node.EditorPosition;
            Vector2 dir = fromWorld - center;

            switch (node.NodeType)
            {
                case ConversationNodeType.Conditional:
                    if (node.conditionalBranch != null)
                    {
                        float hx = node.EditorSize.x * 0.5f;
                        float hy = node.EditorSize.y * 0.5f;
                        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                        {
                            return dir.x < 0 ? new Vector2(center.x - hx, center.y) : new Vector2(center.x + hx, center.y);
                        }
                        else
                        {
                            return dir.y < 0 ? new Vector2(center.x, center.y - hy) : new Vector2(center.x, center.y + hy);
                        }
                    }
                    // fall through to rectangle behavior if no conditional branch data
                    break;
                default:
                    break;
            }

            // Rectangular nodes: pick side based on which axis is dominant
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            {
                return dir.x < 0 ? new Vector2(nodeRect.xMin, nodeRect.center.y) : new Vector2(nodeRect.xMax, nodeRect.center.y);
            }
            else
            {
                return dir.y < 0 ? new Vector2(nodeRect.center.x, nodeRect.yMin) : new Vector2(nodeRect.center.x, nodeRect.yMax);
            }
        }
        #endregion

        #region Menus
        private void ShowContextMenu()
        {
            if (isReadOnly) return;
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
            if (isReadOnly) return;
            GenericMenu menu = new GenericMenu();

            // Use switch for node type handling per project rules
            switch (node.NodeType)
            {
                case ConversationNodeType.Conditional:
                    // Provide two explicit connect actions for conditional true/false branches
                    menu.AddItem(new GUIContent("Connect to Node (true)"), false, () =>
                    {
                        isConnecting = true;
                        connectingFromNode = node;
                        connectingFromOption = null;
                        // ensure branch exists
                        if (node.conditionalBranch == null) node.conditionalBranch = new ConditionalBranch { Conditions = new List<ConditionRule>(), NextNodeIdTrue = 0, NextNodeIdFalse = 0 };
                        connectingFromBranch = node.conditionalBranch;
                        connectingBranchIndex = 0;
                        isRightClickMenuActive = false;
                        RequestRepaint();
                    });
                    menu.AddItem(new GUIContent("Connect to Node (false)"), false, () =>
                    {
                        isConnecting = true;
                        connectingFromNode = node;
                        connectingFromOption = null;
                        if (node.conditionalBranch == null) node.conditionalBranch = new ConditionalBranch { Conditions = new List<ConditionRule>(), NextNodeIdTrue = 0, NextNodeIdFalse = 0 };
                        connectingFromBranch = node.conditionalBranch;
                        connectingBranchIndex = 1;
                        isRightClickMenuActive = false;
                        RequestRepaint();
                    });
                    menu.AddSeparator("");
                    break;
                case ConversationNodeType.Dialogue:
                    // Dialogue-specific: allow adding options
                    menu.AddItem(new GUIContent("Add Option"), false, () =>
                    {
                        if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Add Option");
                        if (node.Options == null) node.Options = new List<ConversationOption>();
                        node.Options.Add(CreateOption(node, "", node.Options.Count));
                        MarkDirty();
                        isRightClickMenuActive = false;
                    });
                    break;
                default:
                    break;
            }
            if (node.NodeType != ConversationNodeType.End && node.NodeType != ConversationNodeType.Conditional)
            {
                menu.AddItem(new GUIContent("Connect to Node"), false, () =>
                {
                    isConnecting = true;
                    connectingFromNode = node;
                    connectingFromOption = null;
                    connectingFromBranch = null;
                    isRightClickMenuActive = false;
                    RequestRepaint();
                });
                menu.AddSeparator("");
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
            if (isReadOnly) return;
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate Option"), false, () =>
            {
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Duplicate Option");
                var newOption = new ConversationOption
                {
                    Text = option.Text,
                    NextNodeId = 0,
                    Conditions = new List<ConditionRule>(option.Conditions ?? new List<ConditionRule>()),
                    EditorPosition = option.EditorPosition + new Vector2(25f, 20f),
                    EditorSize = option.EditorSize
                };
                node.Options.Insert(index + 1, newOption);
                SetSelection(node, newOption, null);
                MarkDirty();
                isRightClickMenuActive = false;
            });
            menu.AddItem(new GUIContent("Create New Option"), false, () =>
            {
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Create Option");
                if (node.Options == null) node.Options = new List<ConversationOption>();
                var newOption = CreateOption(node, "-", node.Options.Count);
                node.Options.Insert(index + 1, newOption);
                SetSelection(node, newOption, null);
                MarkDirty();
                isRightClickMenuActive = false;
            });
            menu.AddItem(new GUIContent("Delete Option"), false, () =>
            {
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Delete Option");
                node.Options.RemoveAt(index);
                if (selectedOption == option) SetSelection(node, null, null);
                MarkDirty();
                isRightClickMenuActive = false;
            });
            isRightClickMenuActive = true;
            menu.ShowAsContext();
        }
        #endregion

        #region Node Ops
        private bool HasOnlyStartAndEndNodes()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return false;
            var nodes = conversationData.ConversationManager.Nodes;
            if (nodes.Count != 2) return false;
            bool hasStart = false;
            bool hasEnd = false;
            foreach (var node in nodes)
            {
                if (node.NodeType == ConversationNodeType.Start) hasStart = true;
                else if (node.NodeType == ConversationNodeType.End) hasEnd = true;
                else return false;
            }
            return hasStart && hasEnd;
        }

        private void TryAutoLinkStartNode(ConversationNode newNode)
        {
            if (conversationData?.ConversationManager?.Nodes == null || newNode == null) return;
            var startNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);
            if (startNode == null) return;
            startNode.NextNodeId = newNode.Id;
        }

        private void CreateNode(ConversationNodeType nodeType)
        {
            if (isReadOnly || conversationData?.ConversationManager?.Nodes == null) return;
            if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Create Node");
            bool shouldAutoLink = HasOnlyStartAndEndNodes();
            Vector2 editorSize = nodeType == ConversationNodeType.Conditional ? new Vector2(150, 100) : new Vector2(200, 100);
            var newNode = new ConversationNode
            {
                Id = ConversationNodeUtility.GetNextAvailableId(conversationData.ConversationManager.Nodes),
                NodeType = nodeType,
                EditorPosition = ToNodeCenterPosition(contextMenuPosition, editorSize),
                EditorSize = editorSize,
                conditionalBranch = nodeType == ConversationNodeType.Conditional ? new ConditionalBranch { Conditions = new List<ConditionRule>(), NextNodeIdTrue = 0, NextNodeIdFalse = 0 } : null
            };
            conversationData.ConversationManager.Nodes.Add(newNode);
            if (shouldAutoLink) TryAutoLinkStartNode(newNode);
            SetSelection(newNode, null, null);
            isRightClickMenuActive = false;
            MarkDirty();
            RequestRepaint();
        }

        private void CreateNodeWithOptions()
        {
            if (isReadOnly || conversationData?.ConversationManager?.Nodes == null) return;
            if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Create Node with Options");
            bool shouldAutoLink = HasOnlyStartAndEndNodes();
            Vector2 editorSize = new Vector2(200, 100);
            var newNode = new ConversationNode
            {
                Id = ConversationNodeUtility.GetNextAvailableId(conversationData.ConversationManager.Nodes),
                NodeType = ConversationNodeType.Dialogue,
                EditorPosition = ToNodeCenterPosition(contextMenuPosition, editorSize),
                EditorSize = editorSize,
                Options = new List<ConversationOption>()
            };
            newNode.Options.Add(CreateOption(newNode, "Option 1", 0));
            newNode.Options.Add(CreateOption(newNode, "Option 2", 1));
            conversationData.ConversationManager.Nodes.Add(newNode);
            if (shouldAutoLink) TryAutoLinkStartNode(newNode);
            SetSelection(newNode, null, null);
            isRightClickMenuActive = false;
            MarkDirty();
            RequestRepaint();
        }

        private void DuplicateNode(ConversationNode node)
        {
            if (isReadOnly || node == null || conversationData?.ConversationManager?.Nodes == null) return;
            if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Duplicate Node");
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
                    Conditions = new List<ConditionRule>(o.Conditions ?? new List<ConditionRule>()),
                    EditorPosition = o.EditorPosition,
                    EditorSize = o.EditorSize
                }).ToList(),
                Functions = node.Functions?.Select(f => new ConversationFunction
                {
                    MethodName = f.MethodName,
                    Parameters = new Dictionary<string, string>(f.Parameters ?? new Dictionary<string, string>()),
                    Timestamp = f.Timestamp
                }).ToList(),
                conditionalBranch = node.conditionalBranch != null ? new ConditionalBranch
                {
                    Conditions = new List<ConditionRule>(node.conditionalBranch.Conditions ?? new List<ConditionRule>()),
                    NextNodeIdTrue = 0,
                    NextNodeIdFalse = 0
                } : null,
                DefaultBranchNodeId = 0
            };
            conversationData.ConversationManager.Nodes.Add(newNode);
            SetSelection(newNode, null, null);
            MarkDirty();
            RequestRepaint();
        }

        private void DeleteNode(ConversationNode node)
        {
            if (isReadOnly || node == null || conversationData?.ConversationManager?.Nodes == null) return;
            if (node.NodeType == ConversationNodeType.Start || node.NodeType == ConversationNodeType.End)
            {
                EditorUtility.DisplayDialog("Cannot Delete", "Cannot delete Start or End nodes.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Delete Node", $"Are you sure you want to delete node {node.Id}?", "Delete", "Cancel"))
            {
                if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Delete Node");
                ConversationNodeUtility.RemoveNodeReferences(node.Id, conversationData.ConversationManager.Nodes);
                conversationData.ConversationManager.Nodes.Remove(node);
                if (selectedNode == node) ClearSelection();
                MarkDirty();
                RequestRepaint();
            }
        }

        private void FrameNode(ConversationNode node)
        {
            if (node == null) return;
            Vector2 graphCenter = new Vector2(currentGraphRect.width, currentGraphRect.height) * 0.5f;
            panOffset = graphCenter / zoom - node.EditorPosition;
            RequestRepaint();
        }
        #endregion

        #region Layout
        private Rect GetConversationBounds()
        {
            bool hasBounds = false;
            Rect bounds = default;
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                Rect nodeBounds = GetNodeVisualBounds(node);
                if (!hasBounds)
                {
                    bounds = nodeBounds;
                    hasBounds = true;
                    continue;
                }
                bounds = EncapsulateRect(bounds, nodeBounds);
            }
            return hasBounds ? bounds : new Rect(0f, 0f, 1f, 1f);
        }

        private Rect GetNodeVisualBounds(ConversationNode node)
        {
            Rect bounds = GetNodeWorldRect(node);
            if (node.Options != null && node.Options.Count > 0)
            {
                for (int i = 0; i < node.Options.Count; i++)
                {
                    Rect optionRect = GetOptionWorldRect(node, node.Options[i], i);
                    bounds = EncapsulateRect(bounds, optionRect);
                }
            }
            if (node.NodeType == ConversationNodeType.Conditional && node.conditionalBranch != null)
            {
                // include small left/right indicators in visual bounds
                Vector2 center = node.EditorPosition;
                float indicatorSize = 16f;
                Rect trueRect = new Rect(center.x - node.EditorSize.x * 0.5f - indicatorSize - 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
                Rect falseRect = new Rect(center.x + node.EditorSize.x * 0.5f + 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
                bounds = EncapsulateRect(bounds, trueRect);
                bounds = EncapsulateRect(bounds, falseRect);
            }
            return bounds;
        }

        private Rect EncapsulateRect(Rect a, Rect b)
        {
            float xMin = Mathf.Min(a.xMin, b.xMin);
            float yMin = Mathf.Min(a.yMin, b.yMin);
            float xMax = Mathf.Max(a.xMax, b.xMax);
            float yMax = Mathf.Max(a.yMax, b.yMax);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void AutoLayoutNodes(bool horizontal)
        {
            if (isReadOnly || conversationData?.ConversationManager?.Nodes == null || conversationData.ConversationManager.Nodes.Count == 0) return;
            if (ownerWindow != null) Undo.RecordObject(ownerWindow, "Auto-Layout Nodes");
            var startNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);
            if (startNode == null) return;
            var visited = new HashSet<int>();
            var levelPositions = new Dictionary<int, float>();
            if (horizontal) LayoutNodesHorizontal(startNode, 50f, 50f, visited, levelPositions, 0);
            else LayoutNodesVertical(startNode, 50f, 50f, visited, levelPositions, 0);
            MarkDirty();
            RequestRepaint();
        }

        private float LayoutNodesHorizontal(ConversationNode node, float x, float y, HashSet<int> visited, Dictionary<int, float> levelPositions, int level)
        {
            if (node == null || visited.Contains(node.Id)) return y;
            visited.Add(node.Id);
            if (!levelPositions.ContainsKey(level)) levelPositions[level] = y;
            else y = levelPositions[level];
            node.EditorPosition = ToNodeCenterPosition(new Vector2(x, y), node.EditorSize);
            var nextNodes = GetConnectedNodes(node);
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

        private float LayoutNodesVertical(ConversationNode node, float x, float y, HashSet<int> visited, Dictionary<int, float> levelPositions, int level)
        {
            if (node == null || visited.Contains(node.Id)) return x;
            visited.Add(node.Id);
            if (!levelPositions.ContainsKey(level)) levelPositions[level] = x;
            else x = levelPositions[level];
            node.EditorPosition = ToNodeCenterPosition(new Vector2(x, y), node.EditorSize);
            var nextNodes = GetConnectedNodes(node);
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

        private List<ConversationNode> GetConnectedNodes(ConversationNode node)
        {
            var nextNodes = new List<ConversationNode>();
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
            if (node.conditionalBranch != null)
            {
                var branch = node.conditionalBranch;
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
            if (node.DefaultBranchNodeId > 0)
            {
                var nextNode = conversationData.ConversationManager.Nodes.FirstOrDefault(n => n.Id == node.DefaultBranchNodeId);
                if (nextNode != null && !nextNodes.Contains(nextNode)) nextNodes.Add(nextNode);
            }
            return nextNodes;
        }
        #endregion

        #region Zoom
        private void DrawZoomControls(Rect area)
        {
            Rect containerRect = GetZoomControlsRect(area);
            EditorGUI.DrawRect(containerRect, new Color(0f, 0f, 0f, 0.4f));
            Rect labelRect = new Rect(containerRect.x, containerRect.y + (4f * zoomControlScale), containerRect.width, 20f * zoomControlScale);
            GUI.Label(labelRect, new GUIContent($"{zoom:F1}x", "Current graph zoom level."), EditorStyles.centeredGreyMiniLabel);
            Rect zoomSliderRect = new Rect(containerRect.x + (10f * zoomControlScale), containerRect.y + (28f * zoomControlScale), 14f * zoomControlScale, containerRect.height - (36f * zoomControlScale));
            float newZoom = GUI.VerticalSlider(zoomSliderRect, zoom, maxZoom, minZoom);
            if (!Mathf.Approximately(newZoom, zoom))
            {
                zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
                if (!isReadOnly) SaveEditorZoomSetting();
                RequestRepaint();
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
            if (conversationData.EditorSettings == null) conversationData.EditorSettings = new ConversationEditorSettings();
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
        #endregion

        #region Styles
        private GUIStyle GetNodeStyle(ConversationNode node)
        {
            bool isSelected = selectedNode == node;
            bool isDragging = isNodeBeingDragged && isSelected;
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    if (isDragging) return conversationNodeStyle.startNodeDraggingStyle;
                    if (isSelected) return conversationNodeStyle.startNodeSelectedStyle;
                    return conversationNodeStyle.startNodeStyle;
                case ConversationNodeType.End:
                    if (isDragging) return conversationNodeStyle.endNodeDraggingStyle;
                    if (isSelected) return conversationNodeStyle.endNodeSelectedStyle;
                    return conversationNodeStyle.endNodeStyle;
                case ConversationNodeType.Function:
                    if (isDragging) return conversationNodeStyle.functionNodeDraggingStyle;
                    if (isSelected) return conversationNodeStyle.functionNodeSelectedStyle;
                    return conversationNodeStyle.functionNodeStyle;
                case ConversationNodeType.Conditional:
                    if (isDragging) return conversationNodeStyle.conditionalNodeDraggingStyle;
                    if (isSelected) return conversationNodeStyle.conditionalNodeSelectedStyle;
                    return conversationNodeStyle.conditionalNodeStyle;
                default:
                    if (isDragging) return conversationNodeStyle.nodeDraggingStyle;
                    if (isSelected) return conversationNodeStyle.nodeSelectedStyle;
                    return conversationNodeStyle.nodeStyle;
            }
        }

        private GUIStyle GetOptionStyle(ConversationOption option)
        {
            bool isSelected = selectedOption == option;
            bool isDragging = isOptionBeingDragged && isSelected;
            if (isDragging) return conversationNodeStyle.optionNodeDraggingStyle;
            if (isSelected) return conversationNodeStyle.optionNodeSelectedStyle;
            return conversationNodeStyle.optionNodeStyle;
        }

        private int GetScaledNodeFontSize(int baseFontSize)
        {
            return Mathf.Max(minNodeFontSize, Mathf.RoundToInt(baseFontSize * zoom));
        }
        #endregion

        #region Internal Helpers
        private void SetSelection(ConversationNode node, ConversationOption option, ConditionalBranch branch)
        {
            selectedNode = node;
            selectedOption = option;
            selectedBranch = branch;
            OnSelectionChanged?.Invoke();
        }

        private void ClearSelection()
        {
            selectedNode = null;
            selectedOption = null;
            selectedBranch = null;
            OnSelectionChanged?.Invoke();
        }

        private void MarkDirty()
        {
            OnDirty?.Invoke();
        }

        private void RequestRepaint()
        {
            OnRepaintRequested?.Invoke();
            ownerWindow?.Repaint();
        }

        private void EnsureOptionEditorDataInConversation()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.Options == null) continue;
                for (int i = 0; i < node.Options.Count; i++)
                {
                    EnsureOptionEditorData(node, node.Options[i], i);
                }
            }
        }

        private void EnsureMinimumEditorSizesInConversation()
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;
            bool hasChanges = false;
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                Vector2 clampedNodeSize = ClampEditorSize(node.EditorSize);
                if (clampedNodeSize != node.EditorSize)
                {
                    node.EditorSize = clampedNodeSize;
                    hasChanges = true;
                }
                if (node.Options == null) continue;
                foreach (var option in node.Options)
                {
                    Vector2 clampedOptionSize = ClampEditorSize(option.EditorSize);
                    if (clampedOptionSize != option.EditorSize)
                    {
                        option.EditorSize = clampedOptionSize;
                        hasChanges = true;
                    }
                }
            }
            if (hasChanges) MarkDirty();
        }

        private Vector2 ClampEditorSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(minEditorNodeSize, size.x), Mathf.Max(minEditorNodeSize, size.y));
        }

        private int GetNodePreviewTextLength(ConversationNode node, bool hasActorLine)
        {
            int bodyFontSize = GetScaledNodeFontSize(nodeBodyBaseFontSize);
            float usableWidth = Mathf.Max(minEditorNodeSize, node.EditorSize.x - nodeHorizontalPadding);
            float headerHeight = GetScaledNodeFontSize(nodeHeaderBaseFontSize) + estimatedLineSpacing;
            float actorHeight = hasActorLine ? bodyFontSize + estimatedLineSpacing : 0f;
            float usableHeight = Mathf.Max(minEditorNodeSize, node.EditorSize.y - headerHeight - actorHeight - nodeVerticalPadding);
            return EstimatePreviewLength(usableWidth, usableHeight, bodyFontSize);
        }

        private int GetOptionPreviewTextLength(ConversationOption option)
        {
            int bodyFontSize = GetScaledNodeFontSize(nodeBodyBaseFontSize);
            float usableWidth = Mathf.Max(minEditorNodeSize, option.EditorSize.x - nodeHorizontalPadding);
            float headerHeight = bodyFontSize + estimatedLineSpacing;
            float usableHeight = Mathf.Max(minEditorNodeSize, option.EditorSize.y - headerHeight - nodeVerticalPadding);
            return EstimatePreviewLength(usableWidth, usableHeight, bodyFontSize);
        }

        private int EstimatePreviewLength(float width, float height, int fontSize)
        {
            float estimatedCharacterWidth = Mathf.Max(1f, fontSize * 0.55f);
            float lineHeight = Mathf.Max(1f, fontSize + estimatedLineSpacing);
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(width / estimatedCharacterWidth));
            int maxLines = Mathf.Max(1, Mathf.FloorToInt(height / lineHeight));
            return charsPerLine * maxLines;
        }

        private string BuildPreviewText(string sourceText, int maxLength, string emptyFallback = "")
        {
            if (string.IsNullOrEmpty(sourceText)) return emptyFallback;
            int safeLength = Mathf.Max(1, maxLength);
            if (sourceText.Length <= safeLength) return sourceText;
            int trimmedLength = Mathf.Max(1, safeLength - 3);
            return sourceText.Substring(0, trimmedLength) + "...";
        }

        private Rect GetOptionWorldRect(ConversationNode node, ConversationOption option, int optionIndex)
        {
            EnsureOptionEditorData(node, option, optionIndex);
            Rect nodeRect = GetNodeWorldRect(node);
            Vector2 optionWorldPos = nodeRect.position + option.EditorPosition;
            return new Rect(optionWorldPos, option.EditorSize);
        }

        private void EnsureOptionEditorData(ConversationNode node, ConversationOption option, int optionIndex)
        {
            if (node == null || option == null) return;
            option.EditorSize = ClampEditorSize(option.EditorSize);
            if (option.EditorPosition == Vector2.zero) option.EditorPosition = GenerateOptionPosition(node, optionIndex);
        }

        private Vector2 GenerateOptionPosition(ConversationNode node, int optionIndex)
        {
            float baseX = node.EditorSize.x + optionDefaultSpacing + Random.Range(10f, 45f);
            float baseY = (optionDefaultHeight + optionDefaultSpacing) * optionIndex + Random.Range(-20f, 20f);
            return new Vector2(baseX, baseY);
        }

        private ConversationOption CreateOption(ConversationNode node, string text, int optionIndex)
        {
            var option = new ConversationOption
            {
                Text = text,
                NextNodeId = 0,
                Conditions = new List<ConditionRule>(),
                EditorSize = new Vector2(optionDefaultWidth, optionDefaultHeight),
                EditorPosition = GenerateOptionPosition(node, optionIndex)
            };
            return option;
        }

        private bool IsPointerOverInteractiveElement(Vector2 mouseWorldPos)
        {
            if (conversationData?.ConversationManager?.Nodes == null) return false;
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                Rect nodeRect = GetNodeWorldRect(node);
                if (nodeRect.Contains(mouseWorldPos)) return true;
                if (node.Options != null)
                {
                    for (int i = 0; i < node.Options.Count; i++)
                    {
                        Rect optionRect = GetOptionWorldRect(node, node.Options[i], i);
                        if (optionRect.Contains(mouseWorldPos)) return true;
                    }
                }
                if (node.NodeType != ConversationNodeType.Conditional || node.conditionalBranch == null) continue;
                float indicatorSize = 16f;
                Vector2 center = node.EditorPosition;
                Rect trueRect = new Rect(center.x - node.EditorSize.x * 0.5f - indicatorSize - 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
                Rect falseRect = new Rect(center.x + node.EditorSize.x * 0.5f + 6f, center.y - indicatorSize * 0.5f, indicatorSize, indicatorSize);
                if (trueRect.Contains(mouseWorldPos) || falseRect.Contains(mouseWorldPos)) return true;
            }
            return false;
        }

        private Rect GetNodeWorldRect(ConversationNode node)
        {
            return new Rect(TranslateNodeDrawPosition(node.EditorPosition, node.EditorSize), node.EditorSize);
        }

        private Vector2 TranslateNodeDrawPosition(Vector2 position, Vector2 size)
        {
            return position - size * 0.5f;
        }

        private Vector2 ToNodeCenterPosition(Vector2 drawPosition, Vector2 size)
        {
            return drawPosition + size * 0.5f;
        }
        #endregion
    }
}