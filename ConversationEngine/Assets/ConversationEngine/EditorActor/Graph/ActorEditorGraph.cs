using System.Collections.Generic;
using System.Linq;
using ConversationEditorActor.Helper;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
using BodyPartData = ConversationScheme.BodyPart;
namespace ConversationEditorActor.Graph
{
    public class ActorEditorGraph
    {
        #region Constants
        private const float bodyPartSelectTriangleSize = 30f;
        private const float pivotHitSize = 28f;
        private const float minZoom = 0.25f;
        private const float maxZoom = 2.5f;
        private const float zoomStep = 0.1f;
        #endregion

        #region Inner Types
        private class BodyPartRenderInfo
        {
            public BodyPartData BodyPart;
            public BodyPartResource ActiveResource;
            public Rect Rect;
            public Vector2 Center;
            public Texture2D Texture;
        }
        private class PivotHandleInfo
        {
            public PivotPoint PivotPoint;
            public int BodyPartId;
            public Vector2 Position;
            public Rect HitRect;
        }
        #endregion

        #region State
        private float zoom = 1f;
        private PivotPoint draggingPivot;
        private Vector2 lastMousePosition;
        private bool hasPivotDragMoved;
        private bool undoSnapshotRequested;
        #endregion

        #region Undo Helpers
        public bool IsPivotDragging()
        {
            return draggingPivot != null;
        }
        public bool ConsumeUndoSnapshotRequest()
        {
            if (!undoSnapshotRequested) return false;
            undoSnapshotRequested = false;
            return true;
        }
        #endregion

        #region Draw
        public bool Draw(Rect contentRect, Actor actor, ref int selectedBodyPartId, string actorAssetPath, HashSet<int> hiddenBodyPartIds)
        {
            return DrawInternal(contentRect, actor, ref selectedBodyPartId, actorAssetPath, hiddenBodyPartIds, false, true);
        }
        public void DrawReadOnlyPreview(Rect contentRect, Actor actor, string actorAssetPath)
        {
            int previewSelection = 0;
            DrawInternal(contentRect, actor, ref previewSelection, actorAssetPath, new HashSet<int>(), true, false);
        }
        private bool DrawInternal(Rect contentRect, Actor actor, ref int selectedBodyPartId, string actorAssetPath, HashSet<int> hiddenBodyPartIds, bool isReadOnly, bool showSupportVisuals)
        {
            if (actor == null) return false;
            if (hiddenBodyPartIds == null) hiddenBodyPartIds = new HashSet<int>();
            bool changed = false;
            Rect controlsRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 28f);
            Rect graphRect = new Rect(contentRect.x, contentRect.y + 30f, contentRect.width, contentRect.height - 30f);
            DrawControls(controlsRect);
            HandleZoomKeyboardInput();
            GUI.Box(graphRect, GUIContent.none);
            if (graphRect.width <= 10f || graphRect.height <= 10f) return false;
            HandleZoomScrollWheel(graphRect);
            DrawVerticalZoomBar(graphRect);
            Vector2 actorSize = actor.Size;
            if (actorSize.x <= 0f) actorSize.x = 300f;
            if (actorSize.y <= 0f) actorSize.y = 400f;
            Vector2 scaledActorSize = actorSize * zoom;
            Rect actorRect = new Rect(graphRect.center - scaledActorSize * 0.5f, scaledActorSize);
            EditorGUI.DrawRect(actorRect, new Color(1f, 1f, 1f, 0.03f));
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2f, new Vector3(actorRect.xMin, actorRect.yMin), new Vector3(actorRect.xMax, actorRect.yMin), new Vector3(actorRect.xMax, actorRect.yMax), new Vector3(actorRect.xMin, actorRect.yMax), new Vector3(actorRect.xMin, actorRect.yMin));
            Vector2 actorMainPivot = actorRect.center;
            //DrawMainPivot(actorMainPivot);
            Dictionary<int, BodyPartRenderInfo> renderMap = BuildRenderMap(actor, actorMainPivot, actorAssetPath);
            List<BodyPartRenderInfo> visibleRenderInfos = new List<BodyPartRenderInfo>();
            foreach (var renderInfo in renderMap.Values)
            {
                if (hiddenBodyPartIds.Contains(renderInfo.BodyPart.Id)) continue;
                visibleRenderInfos.Add(renderInfo);
                DrawBodyPart(renderInfo, selectedBodyPartId == renderInfo.BodyPart.Id, showSupportVisuals);
            }
            if (showSupportVisuals) foreach (var renderInfo in visibleRenderInfos) DrawBodyPartSelectionTriangle(renderInfo, ref selectedBodyPartId, isReadOnly);
            List<PivotHandleInfo> pivotHandles = new List<PivotHandleInfo>();
            if (showSupportVisuals)
            {
                foreach (var renderInfo in visibleRenderInfos)
                {
                    var effectivePivots = ActorEditorHelpers.GetEffectivePivotPoints(renderInfo.BodyPart, renderInfo.ActiveResource);
                    foreach (var pivot in effectivePivots)
                    {
                        bool isOverridePivot = IsOverridePivot(renderInfo.ActiveResource, pivot.Id);
                        Vector2 pivotPosition = new Vector2(renderInfo.Center.x + pivot.X * zoom, renderInfo.Center.y - pivot.Y * zoom);
                        float halfHitSize = pivotHitSize * 0.5f;
                        Rect pivotRect = new Rect(pivotPosition.x - halfHitSize, pivotPosition.y - halfHitSize, pivotHitSize, pivotHitSize);
                        PivotPoint editablePivot = FindEditablePivot(renderInfo.BodyPart, renderInfo.ActiveResource, pivot.Id);
                        bool isActivePivot = selectedBodyPartId == renderInfo.BodyPart.Id;
                        if (editablePivot != null && draggingPivot == editablePivot) isActivePivot = true;
                        pivotHandles.Add(new PivotHandleInfo
                        {
                            PivotPoint = editablePivot,
                            BodyPartId = renderInfo.BodyPart.Id,
                            Position = pivotPosition,
                            HitRect = pivotRect
                        });
                        if (isActivePivot) DrawPivotRelationLine(renderInfo.Center, pivotPosition);
                        DrawPivotDiamond(pivotPosition, isActivePivot, isOverridePivot);
                    }
                }
            }
            if (!isReadOnly && showSupportVisuals) changed |= HandlePivotDrag(graphRect, pivotHandles);
            return changed;
        }
        private void DrawControls(Rect controlsRect)
        {
            GUILayout.BeginArea(controlsRect);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("-", "Zoom out."), GUILayout.Width(22f))) zoom = Mathf.Clamp(zoom - zoomStep, minZoom, maxZoom);
            EditorGUILayout.LabelField(new GUIContent("Zoom", "Scale each graph component individually."), GUILayout.Width(36f));
            zoom = GUILayout.HorizontalSlider(zoom, minZoom, maxZoom, GUILayout.Width(220f));
            if (GUILayout.Button(new GUIContent("+", "Zoom in."), GUILayout.Width(22f))) zoom = Mathf.Clamp(zoom + zoomStep, minZoom, maxZoom);
            if (GUILayout.Button(new GUIContent("1x", "Reset zoom to 1.0."), GUILayout.Width(30f))) zoom = 1f;
            EditorGUILayout.LabelField(new GUIContent(zoom.ToString("0.00") + "x", "Current zoom."), GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        private void DrawVerticalZoomBar(Rect graphRect)
        {
            Rect barBackgroundRect = new Rect(graphRect.xMax - 30f, graphRect.y + 8f, 22f, 160f);
            EditorGUI.DrawRect(barBackgroundRect, new Color(0f, 0f, 0f, 0.25f));
            Rect sliderRect = new Rect(barBackgroundRect.x + 4f, barBackgroundRect.y + 22f, 14f, barBackgroundRect.height - 28f);
            float newZoom = GUI.VerticalSlider(sliderRect, zoom, maxZoom, minZoom);
            if (!Mathf.Approximately(newZoom, zoom)) zoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
            GUI.Label(new Rect(barBackgroundRect.x - 2f, barBackgroundRect.y + 2f, barBackgroundRect.width + 4f, 18f), new GUIContent(zoom.ToString("0.0") + "x", "Current zoom."), EditorStyles.centeredGreyMiniLabel);
        }
        private void HandleZoomKeyboardInput()
        {
            Event currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type != EventType.KeyDown) return;
            if (currentEvent.keyCode == KeyCode.Equals || currentEvent.keyCode == KeyCode.Plus || currentEvent.keyCode == KeyCode.KeypadPlus)
            {
                zoom = Mathf.Clamp(zoom + zoomStep, minZoom, maxZoom);
                currentEvent.Use();
                return;
            }
            if (currentEvent.keyCode == KeyCode.Minus || currentEvent.keyCode == KeyCode.KeypadMinus)
            {
                zoom = Mathf.Clamp(zoom - zoomStep, minZoom, maxZoom);
                currentEvent.Use();
                return;
            }
            if (currentEvent.keyCode == KeyCode.Alpha1 || currentEvent.keyCode == KeyCode.Keypad1)
            {
                zoom = 1f;
                currentEvent.Use();
            }
        }
        private void HandleZoomScrollWheel(Rect graphRect)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type != EventType.ScrollWheel) return;
            if (!graphRect.Contains(currentEvent.mousePosition)) return;
            float zoomDelta = -currentEvent.delta.y * 0.05f;
            float newZoom = Mathf.Clamp(zoom + zoomDelta, minZoom, maxZoom);
            if (Mathf.Approximately(newZoom, zoom)) return;
            zoom = newZoom;
            currentEvent.Use();
        }
        private void DrawMainPivot(Vector2 position)
        {
            DrawPivotDiamond(position, true, false);
            Handles.color = new Color(1f, 0f, 0f, 0.6f);
            Handles.DrawAAPolyLine(2f, new Vector3(position.x - 30f, position.y), new Vector3(position.x + 30f, position.y));
            Handles.DrawAAPolyLine(2f, new Vector3(position.x, position.y - 30f), new Vector3(position.x, position.y + 30f));
        }
        private void DrawBodyPart(BodyPartRenderInfo renderInfo, bool selected, bool showSupportVisuals)
        {
            if (renderInfo.Texture != null) GUI.DrawTexture(renderInfo.Rect, renderInfo.Texture, ScaleMode.StretchToFill, true);
            if (!showSupportVisuals) return;
            Color borderColor = selected ? new Color(0.1f, 1f, 0.1f, 1f) : new Color(0.1f, 1f, 0.1f, 0.5f);
            Handles.color = borderColor;
            Handles.DrawAAPolyLine(2f, new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMin), new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMin), new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMax), new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMax), new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMin));
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = borderColor;
            GUI.Label(new Rect(renderInfo.Rect.xMin + 4f, renderInfo.Rect.yMin + 2f, renderInfo.Rect.width - 8f, 16f), renderInfo.BodyPart.Name + " [" + renderInfo.BodyPart.Id + "]", labelStyle);
        }
        private void DrawBodyPartSelectionTriangle(BodyPartRenderInfo renderInfo, ref int selectedBodyPartId, bool isReadOnly)
        {
            float triangleSize = Mathf.Min(bodyPartSelectTriangleSize, Mathf.Min(renderInfo.Rect.width, renderInfo.Rect.height));
            Vector3 corner = new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMax);
            Vector3 left = new Vector3(renderInfo.Rect.xMax - triangleSize, renderInfo.Rect.yMax);
            Vector3 top = new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMax - triangleSize);
            bool isSelected = selectedBodyPartId == renderInfo.BodyPart.Id;
            Color triangleColor = isSelected ? new Color(0.1f, 1f, 0.1f, 1f) : new Color(0.1f, 1f, 0.1f, 0.7f);
            Handles.color = triangleColor;
            Handles.DrawAAConvexPolygon(corner, left, top);
            Rect hitRect = new Rect(renderInfo.Rect.xMax - triangleSize, renderInfo.Rect.yMax - triangleSize, triangleSize, triangleSize);
            Event currentEvent = Event.current;
            if (currentEvent == null) return;
            if (isReadOnly) return;
            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0) return;
            if (!hitRect.Contains(currentEvent.mousePosition)) return;
            selectedBodyPartId = renderInfo.BodyPart.Id;
            currentEvent.Use();
        }
        private void DrawPivotDiamond(Vector2 position, bool selected, bool isOverride)
        {
            float size = selected ? 13f : 11f;
            Vector3 top = new Vector3(position.x, position.y - size);
            Vector3 right = new Vector3(position.x + size, position.y);
            Vector3 bottom = new Vector3(position.x, position.y + size);
            Vector3 left = new Vector3(position.x - size, position.y);
            Color selectedColor = isOverride ? new Color(1f, 0.3f, 0.3f, 1f) : new Color(0.2f, 1f, 0.2f, 1f);
            Color defaultColor = isOverride ? new Color(1f, 0.3f, 0.3f, 0.85f) : new Color(0.2f, 1f, 0.2f, 0.85f);
            Handles.color = selected ? selectedColor : defaultColor;
            Handles.DrawAAConvexPolygon(top, right, bottom, left);
        }
        private void DrawPivotRelationLine(Vector2 bodyPartCenter, Vector2 pivotPosition)
        {
            Handles.color = Color.white;
            Handles.DrawAAPolyLine(6f, bodyPartCenter, pivotPosition);
        }
        private bool IsOverridePivot(BodyPartResource activeResource, int pivotId)
        {
            if (activeResource?.PivotPoints == null) return false;
            return activeResource.PivotPoints.Any(x => x != null && x.Id == pivotId);
        }
        #endregion

        #region Render Mapping
        private Dictionary<int, BodyPartRenderInfo> BuildRenderMap(Actor actor, Vector2 actorMainPivot, string actorAssetPath)
        {
            Dictionary<int, BodyPartRenderInfo> renderMap = new Dictionary<int, BodyPartRenderInfo>();
            Dictionary<int, Vector2> resolvedPivots = new Dictionary<int, Vector2> { { 0, actorMainPivot } };
            var bodyParts = actor.BodyParts == null ? new List<BodyPartData>() : actor.BodyParts.Where(x => x != null).ToList();
            int maxIterations = bodyParts.Count + 1;
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool changed = false;
                foreach (var bodyPart in bodyParts)
                {
                    if (renderMap.ContainsKey(bodyPart.Id)) continue;
                    if (!resolvedPivots.TryGetValue(bodyPart.AttachToPivotId, out var attachPosition)) continue;
                    BodyPartResource activeResource = ActorEditorHelpers.GetActiveResource(bodyPart);
                    Texture2D texture = LoadTexture(activeResource, actorAssetPath);
                    Vector2 size = GetBodyPartSize(texture);
                    size *= zoom;
                    Rect rect = new Rect(attachPosition - size * 0.5f, size);
                    BodyPartRenderInfo renderInfo = new BodyPartRenderInfo
                    {
                        BodyPart = bodyPart,
                        ActiveResource = activeResource,
                        Rect = rect,
                        Center = attachPosition,
                        Texture = texture
                    };
                    renderMap[bodyPart.Id] = renderInfo;
                    foreach (var pivot in ActorEditorHelpers.GetEffectivePivotPoints(bodyPart, activeResource)) resolvedPivots[pivot.Id] = new Vector2(attachPosition.x + pivot.X * zoom, attachPosition.y - pivot.Y * zoom);
                    changed = true;
                }
                if (!changed) break;
            }
            foreach (var bodyPart in bodyParts)
            {
                if (renderMap.ContainsKey(bodyPart.Id)) continue;
                BodyPartResource activeResource = ActorEditorHelpers.GetActiveResource(bodyPart);
                Texture2D texture = LoadTexture(activeResource, actorAssetPath);
                Vector2 size = GetBodyPartSize(texture);
                size *= zoom;
                Rect rect = new Rect(actorMainPivot - size * 0.5f, size);
                renderMap[bodyPart.Id] = new BodyPartRenderInfo
                {
                    BodyPart = bodyPart,
                    ActiveResource = activeResource,
                    Rect = rect,
                    Center = actorMainPivot,
                    Texture = texture
                };
            }
            return renderMap;
        }
        private Texture2D LoadTexture(BodyPartResource resource, string actorAssetPath)
        {
            if (resource == null || string.IsNullOrWhiteSpace(resource.Path)) return null;
            string resolvedPath = ActorEditorHelpers.ResolveResourceAssetPath(actorAssetPath, resource.Path);
            if (string.IsNullOrWhiteSpace(resolvedPath)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedPath);
        }
        private Vector2 GetBodyPartSize(Texture2D texture)
        {
            if (texture == null) return new Vector2(160f, 160f);
            return new Vector2(texture.width, texture.height);
        }
        private PivotPoint FindEditablePivot(BodyPartData bodyPart, BodyPartResource activeResource, int pivotId)
        {
            if (activeResource?.PivotPoints != null)
            {
                PivotPoint overridePivot = activeResource.PivotPoints.FirstOrDefault(x => x != null && x.Id == pivotId);
                if (overridePivot != null) return overridePivot;
            }
            if (bodyPart?.PivotPoints == null) return null;
            return bodyPart.PivotPoints.FirstOrDefault(x => x != null && x.Id == pivotId);
        }
        #endregion

        #region Interaction
        private bool HandlePivotDrag(Rect graphRect, List<PivotHandleInfo> pivotHandles)
        {
            Event currentEvent = Event.current;
            if (currentEvent == null) return false;
            bool changed = false;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                PivotHandleInfo target = null;
                for (int i = pivotHandles.Count - 1; i >= 0; i--)
                {
                    if (!pivotHandles[i].HitRect.Contains(currentEvent.mousePosition)) continue;
                    if (pivotHandles[i].PivotPoint == null) continue;
                    target = pivotHandles[i];
                    break;
                }
                if (target != null)
                {
                    draggingPivot = target.PivotPoint;
                    lastMousePosition = currentEvent.mousePosition;
                    hasPivotDragMoved = false;
                    currentEvent.Use();
                }
            }
            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && draggingPivot != null)
            {
                if (!hasPivotDragMoved)
                {
                    hasPivotDragMoved = true;
                    undoSnapshotRequested = true;
                }
                Vector2 delta = currentEvent.mousePosition - lastMousePosition;
                draggingPivot.X += delta.x / zoom;
                draggingPivot.Y -= delta.y / zoom;
                lastMousePosition = currentEvent.mousePosition;
                changed = true;
                currentEvent.Use();
            }
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && draggingPivot != null)
            {
                draggingPivot = null;
                hasPivotDragMoved = false;
                currentEvent.Use();
            }
            return changed;
        }
        #endregion
    }
}
