using ConversationScheme;
using UnityEditor;
using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// Encapsulates all resize-related logic for conversation nodes and options.
    /// Handles resize state, calculations, and interactions independently.
    /// </summary>
    public class ConversationNodeResizer
    {
        #region Resize Handle Types
        public enum ResizeHandleType
        {
            None,
            Top,
            Bottom,
            Left,
            Right,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }
        #endregion

        #region Constants
        private const float resizeHandleThickness = 10f;
        private const float minEditorNodeSize = 20f;
        #endregion

        #region State
        private bool isResizingNode = false;
        private bool isResizingOption = false;
        private ConversationNode resizingNode;
        private ConversationNode resizingOptionParentNode;
        private ConversationOption resizingOption;
        private ResizeHandleType activeResizeHandle = ResizeHandleType.None;
        private Vector2 resizeStartMouseWorldPosition;
        private Rect resizeStartWorldRect;
        #endregion

        #region Properties
        public bool IsResizingNode => isResizingNode;
        public bool IsResizingOption => isResizingOption;
        public bool IsResizing => isResizingNode || isResizingOption;
        public ResizeHandleType ActiveResizeHandle => activeResizeHandle;
        public ConversationNode ResizingNode => resizingNode;
        public ConversationOption ResizingOption => resizingOption;
        #endregion

        #region Public API

        /// <summary>
        /// Attempts to start resizing a node from the given mouse position.
        /// </summary>
        public bool TryStartNodeResize(ConversationNode node, Rect nodeRect, Vector2 mouseGraphPos, Vector2 mouseWorldPos)
        {
            if (node == null) return false;

            if (TryGetResizeHandle(nodeRect, mouseGraphPos, out var resizeHandle))
            {
                // nodeRect is provided in graph-local coordinates for hit testing.
                // For resize calculations we must store the world-space rectangle of the node.
                Rect nodeWorldRect = ConversationEditorHelpers.GetNodeWorldRect(node.EditorPosition, node.EditorSize);
                StartResize(node, null, null, resizeHandle, mouseWorldPos, nodeWorldRect);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to start resizing an option from the given mouse position.
        /// </summary>
        public bool TryStartOptionResize(ConversationNode parentNode, ConversationOption option, Rect optionRect, Vector2 mouseGraphPos, Vector2 mouseWorldPos)
        {
            if (parentNode == null || option == null) return false;

            if (TryGetResizeHandle(optionRect, mouseGraphPos, out var resizeHandle))
            {
                // optionRect is in graph-local coordinates for hit testing. Compute world rect for resizing.
                Rect optionWorldRect = ConversationEditorHelpers.GetOptionWorldRect(parentNode.EditorPosition, parentNode.EditorSize, option.EditorPosition, option.EditorSize);
                StartResize(null, parentNode, option, resizeHandle, mouseWorldPos, optionWorldRect);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Applies the current resize operation based on mouse movement.
        /// </summary>
        public bool ApplyResize(Vector2 newMouseWorldPos, System.Action<ConversationNode, Vector2> onNodeResized, System.Action<ConversationOption, Vector2> onOptionResized)
        {
            if (!IsResizing) return false;

            Vector2 worldDelta = newMouseWorldPos - resizeStartMouseWorldPosition;
            Rect resizedRect = CalculateResizedRect(resizeStartWorldRect, worldDelta, activeResizeHandle);

            if (isResizingNode && resizingNode != null)
            {
                resizingNode.EditorPosition = resizedRect.center;
                resizingNode.EditorSize = ClampEditorSize(resizedRect.size);
                onNodeResized?.Invoke(resizingNode, resizedRect.size);
            }
            else if (isResizingOption && resizingOptionParentNode != null && resizingOption != null)
            {
                Rect parentRect = GetNodeWorldRect(resizingOptionParentNode);
                resizingOption.EditorSize = ClampEditorSize(resizedRect.size);
                resizingOption.EditorPosition = resizedRect.position - parentRect.position;
                onOptionResized?.Invoke(resizingOption, resizedRect.size);
            }

            return true;
        }

        /// <summary>
        /// Stops the current resize operation.
        /// </summary>
        public void StopResize()
        {
            isResizingNode = false;
            isResizingOption = false;
            resizingNode = null;
            resizingOptionParentNode = null;
            resizingOption = null;
            activeResizeHandle = ResizeHandleType.None;
        }

        /// <summary>
        /// Draws the resize handle rectangles and cursor hints.
        /// </summary>
        public void DrawResizeHandles(Rect targetRect, System.Func<Rect, Rect> toWindowRect)
        {
            var resizeHandles = GetResizeHandleRects(targetRect);
            DrawInvisibleResizeBoxes(resizeHandles);
            AddResizeCursor(resizeHandles.topRect, MouseCursor.ResizeVertical, toWindowRect);
            AddResizeCursor(resizeHandles.bottomRect, MouseCursor.ResizeVertical, toWindowRect);
            AddResizeCursor(resizeHandles.leftRect, MouseCursor.ResizeHorizontal, toWindowRect);
            AddResizeCursor(resizeHandles.rightRect, MouseCursor.ResizeHorizontal, toWindowRect);
            AddResizeCursor(resizeHandles.topLeftRect, MouseCursor.ResizeUpLeft, toWindowRect);
            AddResizeCursor(resizeHandles.bottomRightRect, MouseCursor.ResizeUpLeft, toWindowRect);
            AddResizeCursor(resizeHandles.topRightRect, MouseCursor.ResizeUpRight, toWindowRect);
            AddResizeCursor(resizeHandles.bottomLeftRect, MouseCursor.ResizeUpRight, toWindowRect);

            Rect centerRect = GetCenterCursorRect(targetRect);
            if (centerRect.width > 0f && centerRect.height > 0f)
                EditorGUIUtility.AddCursorRect(toWindowRect(centerRect), MouseCursor.Pan);
        }

        /// <summary>
        /// Determines if the mouse is over a resize handle and returns the handle type.
        /// </summary>
        public bool TryGetResizeHandle(Rect targetRect, Vector2 mouseGraphPosition, out ResizeHandleType handleType)
        {
            handleType = ResizeHandleType.None;
            var resizeHandles = GetResizeHandleRects(targetRect);

            if (resizeHandles.topLeftRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.TopLeft;
                return true;
            }
            if (resizeHandles.topRightRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.TopRight;
                return true;
            }
            if (resizeHandles.bottomLeftRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.BottomLeft;
                return true;
            }
            if (resizeHandles.bottomRightRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.BottomRight;
                return true;
            }
            if (resizeHandles.topRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.Top;
                return true;
            }
            if (resizeHandles.bottomRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.Bottom;
                return true;
            }
            if (resizeHandles.leftRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.Left;
                return true;
            }
            if (resizeHandles.rightRect.Contains(mouseGraphPosition))
            {
                handleType = ResizeHandleType.Right;
                return true;
            }
            return false;
        }

        #endregion

        #region Private Methods

        private void StartResize(ConversationNode node, ConversationNode optionParentNode, ConversationOption option, ResizeHandleType handleType, Vector2 mouseWorldPos, Rect targetWorldRect)
        {
            if (node != null)
            {
                isResizingNode = true;
                isResizingOption = false;
                resizingNode = node;
                resizingOptionParentNode = null;
                resizingOption = null;
            }
            else
            {
                isResizingNode = false;
                isResizingOption = true;
                resizingNode = null;
                resizingOptionParentNode = optionParentNode;
                resizingOption = option;
            }

            activeResizeHandle = handleType;
            resizeStartMouseWorldPosition = mouseWorldPos;
            resizeStartWorldRect = targetWorldRect;
        }

        private Rect CalculateResizedRect(Rect startRect, Vector2 worldDelta, ResizeHandleType handleType)
        {
            float xMin = startRect.xMin;
            float xMax = startRect.xMax;
            float yMin = startRect.yMin;
            float yMax = startRect.yMax;

            switch (handleType)
            {
                case ResizeHandleType.Top:
                    yMin += worldDelta.y;
                    break;
                case ResizeHandleType.Bottom:
                    yMax += worldDelta.y;
                    break;
                case ResizeHandleType.Left:
                    xMin += worldDelta.x;
                    break;
                case ResizeHandleType.Right:
                    xMax += worldDelta.x;
                    break;
                case ResizeHandleType.TopLeft:
                    xMin += worldDelta.x;
                    yMin += worldDelta.y;
                    break;
                case ResizeHandleType.TopRight:
                    xMax += worldDelta.x;
                    yMin += worldDelta.y;
                    break;
                case ResizeHandleType.BottomLeft:
                    xMin += worldDelta.x;
                    yMax += worldDelta.y;
                    break;
                case ResizeHandleType.BottomRight:
                    xMax += worldDelta.x;
                    yMax += worldDelta.y;
                    break;
            }

            float width = xMax - xMin;
            float height = yMax - yMin;
            bool modifiesLeft = handleType == ResizeHandleType.Left || handleType == ResizeHandleType.TopLeft || handleType == ResizeHandleType.BottomLeft;
            bool modifiesTop = handleType == ResizeHandleType.Top || handleType == ResizeHandleType.TopLeft || handleType == ResizeHandleType.TopRight;

            if (width < minEditorNodeSize)
            {
                if (modifiesLeft) xMin = xMax - minEditorNodeSize;
                else xMax = xMin + minEditorNodeSize;
            }
            if (height < minEditorNodeSize)
            {
                if (modifiesTop) yMin = yMax - minEditorNodeSize;
                else yMax = yMin + minEditorNodeSize;
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private (Rect topRect, Rect bottomRect, Rect leftRect, Rect rightRect, Rect topLeftRect, Rect topRightRect, Rect bottomLeftRect, Rect bottomRightRect) GetResizeHandleRects(Rect targetRect)
        {
            float cornerSize = resizeHandleThickness;
            float horizontalLength = targetRect.width * 0.75f;
            float verticalLength = targetRect.height * 0.75f;

            Rect topRect = new Rect(targetRect.center.x - horizontalLength * 0.5f, targetRect.yMin - cornerSize * 0.5f, horizontalLength, cornerSize);
            Rect bottomRect = new Rect(targetRect.center.x - horizontalLength * 0.5f, targetRect.yMax - cornerSize * 0.5f, horizontalLength, cornerSize);
            Rect leftRect = new Rect(targetRect.xMin - cornerSize * 0.5f, targetRect.center.y - verticalLength * 0.5f, cornerSize, verticalLength);
            Rect rightRect = new Rect(targetRect.xMax - cornerSize * 0.5f, targetRect.center.y - verticalLength * 0.5f, cornerSize, verticalLength);
            Rect topLeftRect = new Rect(targetRect.xMin - cornerSize * 0.5f, targetRect.yMin - cornerSize * 0.5f, cornerSize, cornerSize);
            Rect topRightRect = new Rect(targetRect.xMax - cornerSize * 0.5f, targetRect.yMin - cornerSize * 0.5f, cornerSize, cornerSize);
            Rect bottomLeftRect = new Rect(targetRect.xMin - cornerSize * 0.5f, targetRect.yMax - cornerSize * 0.5f, cornerSize, cornerSize);
            Rect bottomRightRect = new Rect(targetRect.xMax - cornerSize * 0.5f, targetRect.yMax - cornerSize * 0.5f, cornerSize, cornerSize);

            return (topRect, bottomRect, leftRect, rightRect, topLeftRect, topRightRect, bottomLeftRect, bottomRightRect);
        }

        private void AddResizeCursor(Rect graphRect, MouseCursor cursor, System.Func<Rect, Rect> toWindowRect)
        {
            EditorGUIUtility.AddCursorRect(toWindowRect(graphRect), cursor);
        }

        private Rect GetCenterCursorRect(Rect targetRect)
        {
            float inset = resizeHandleThickness;
            return new Rect(targetRect.x + inset, targetRect.y + inset, targetRect.width - inset * 2f, targetRect.height - inset * 2f);
        }

        private void DrawInvisibleResizeBoxes((Rect topRect, Rect bottomRect, Rect leftRect, Rect rightRect, Rect topLeftRect, Rect topRightRect, Rect bottomLeftRect, Rect bottomRightRect) resizeHandles)
        {
            GUI.Box(resizeHandles.topRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.bottomRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.leftRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.rightRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.topLeftRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.topRightRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.bottomLeftRect, GUIContent.none, GUIStyle.none);
            GUI.Box(resizeHandles.bottomRightRect, GUIContent.none, GUIStyle.none);
        }

        private Vector2 ClampEditorSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(minEditorNodeSize, size.x), Mathf.Max(minEditorNodeSize, size.y));
        }

        private Rect GetNodeWorldRect(ConversationNode node)
        {
            return ConversationEditorHelpers.GetNodeWorldRect(node.EditorPosition, node.EditorSize);
        }

        #endregion
    }
}