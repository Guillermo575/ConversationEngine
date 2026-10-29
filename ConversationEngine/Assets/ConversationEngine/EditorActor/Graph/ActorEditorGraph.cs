using System.Collections.Generic;
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
        #endregion

        #region Draw
        public bool Draw(Rect contentRect, Actor actor, int selectedBodyPartId, string actorAssetPath, HashSet<int> hiddenBodyPartIds)
        {
            if (actor == null) return false;
            if (hiddenBodyPartIds == null) hiddenBodyPartIds = new HashSet<int>();
            bool changed = false;
            Rect controlsRect = new Rect(contentRect.x, contentRect.y, contentRect.width, 26f);
            Rect graphRect = new Rect(contentRect.x, contentRect.y + 28f, contentRect.width, contentRect.height - 28f);
            DrawControls(controlsRect);
            GUI.Box(graphRect, GUIContent.none);
            if (graphRect.width <= 10f || graphRect.height <= 10f) return false;
            Vector2 actorSize = actor.Size;
            if (actorSize.x <= 0f) actorSize.x = 300f;
            if (actorSize.y <= 0f) actorSize.y = 400f;
            Vector2 scaledActorSize = actorSize * zoom;
            Rect actorRect = new Rect(graphRect.center - scaledActorSize * 0.5f, scaledActorSize);
            EditorGUI.DrawRect(actorRect, new Color(1f, 1f, 1f, 0.03f));
            Handles.color = Color.black;
            Handles.DrawAAPolyLine(2f, new Vector3(actorRect.xMin, actorRect.yMin), new Vector3(actorRect.xMax, actorRect.yMin), new Vector3(actorRect.xMax, actorRect.yMax), new Vector3(actorRect.xMin, actorRect.yMax), new Vector3(actorRect.xMin, actorRect.yMin));
            Vector2 actorMainPivot = actorRect.center;
            DrawMainPivot(actorMainPivot);
            Dictionary<int, BodyPartRenderInfo> renderMap = BuildRenderMap(actor, actorMainPivot, actorAssetPath);
            List<BodyPartRenderInfo> visibleRenderInfos = new List<BodyPartRenderInfo>();
            foreach (var renderInfo in renderMap.Values)
            {
                if (hiddenBodyPartIds.Contains(renderInfo.BodyPart.Id)) continue;
                visibleRenderInfos.Add(renderInfo);
                DrawBodyPart(renderInfo, selectedBodyPartId == renderInfo.BodyPart.Id);
            }
            List<PivotHandleInfo> pivotHandles = new List<PivotHandleInfo>();
            foreach (var renderInfo in visibleRenderInfos)
            {
                var effectivePivots = ActorEditorHelpers.GetEffectivePivotPoints(renderInfo.BodyPart, renderInfo.ActiveResource);
                foreach (var pivot in effectivePivots)
                {
                    Vector2 pivotPosition = new Vector2(renderInfo.Center.x + pivot.X * zoom, renderInfo.Center.y - pivot.Y * zoom);
                    Rect pivotRect = new Rect(pivotPosition.x - 14f, pivotPosition.y - 14f, 28f, 28f);
                    pivotHandles.Add(new PivotHandleInfo
                    {
                        PivotPoint = FindEditablePivot(renderInfo.BodyPart, pivot.Id),
                        BodyPartId = renderInfo.BodyPart.Id,
                        Position = pivotPosition,
                        HitRect = pivotRect
                    });
                    DrawPivotDiamond(pivotPosition, selectedBodyPartId == renderInfo.BodyPart.Id);
                }
            }
            changed |= HandlePivotDrag(graphRect, pivotHandles);
            return changed;
        }
        private void DrawControls(Rect controlsRect)
        {
            GUILayout.BeginArea(controlsRect);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Zoom", "Preview zoom."), GUILayout.Width(40f));
            zoom = GUILayout.HorizontalSlider(zoom, 0.25f, 2.5f, GUILayout.Width(180f));
            EditorGUILayout.LabelField(new GUIContent(zoom.ToString("0.00") + "x", "Current zoom."), GUILayout.Width(60f));
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        private void DrawMainPivot(Vector2 position)
        {
            DrawPivotDiamond(position, true);
            Handles.color = new Color(1f, 0f, 0f, 0.6f);
            Handles.DrawAAPolyLine(2f, new Vector3(position.x - 30f, position.y), new Vector3(position.x + 30f, position.y));
            Handles.DrawAAPolyLine(2f, new Vector3(position.x, position.y - 30f), new Vector3(position.x, position.y + 30f));
        }
        private void DrawBodyPart(BodyPartRenderInfo renderInfo, bool selected)
        {
            if (renderInfo.Texture != null) GUI.DrawTexture(renderInfo.Rect, renderInfo.Texture, ScaleMode.StretchToFill, true);
            Color borderColor = selected ? new Color(0.1f, 1f, 0.1f, 1f) : new Color(0.1f, 1f, 0.1f, 0.5f);
            Handles.color = borderColor;
            Handles.DrawAAPolyLine(2f, new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMin), new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMin), new Vector3(renderInfo.Rect.xMax, renderInfo.Rect.yMax), new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMax), new Vector3(renderInfo.Rect.xMin, renderInfo.Rect.yMin));
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.normal.textColor = borderColor;
            GUI.Label(new Rect(renderInfo.Rect.xMin + 4f, renderInfo.Rect.yMin + 2f, renderInfo.Rect.width - 8f, 16f), renderInfo.BodyPart.Name + " [" + renderInfo.BodyPart.Id + "]", labelStyle);
        }
        private void DrawPivotDiamond(Vector2 position, bool selected)
        {
            float size = selected ? 13f : 11f;
            Vector3 top = new Vector3(position.x, position.y - size);
            Vector3 right = new Vector3(position.x + size, position.y);
            Vector3 bottom = new Vector3(position.x, position.y + size);
            Vector3 left = new Vector3(position.x - size, position.y);
            Handles.color = selected ? new Color(0.2f, 1f, 0.2f, 1f) : new Color(0.2f, 1f, 0.2f, 0.85f);
            Handles.DrawAAConvexPolygon(top, right, bottom, left);
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
        private PivotPoint FindEditablePivot(BodyPartData bodyPart, int pivotId)
        {
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
                    currentEvent.Use();
                }
            }
            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && draggingPivot != null)
            {
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
                currentEvent.Use();
            }
            return changed;
        }
        #endregion
    }
}
