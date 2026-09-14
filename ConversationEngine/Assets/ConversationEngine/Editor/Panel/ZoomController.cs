using System;
using UnityEngine;
using UnityEditor;
namespace ConversationEditor
{
    internal class ZoomController
    {
        #region Variables
        private readonly float zoomControlScale;
        private readonly float minZoom;
        private readonly float maxZoom;
        #endregion

        #region Constructor
        public ZoomController(float zoomControlScale, float minZoom, float maxZoom)
        {
            this.zoomControlScale = zoomControlScale;
            this.minZoom = minZoom;
            this.maxZoom = maxZoom;
        }
        #endregion

        #region Methods
        public void Draw(Rect area, ref float zoom, ref Vector2 panOffset, bool isReadOnly, Action saveEditorViewSettings, Action requestRepaint)
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
                if (!isReadOnly) saveEditorViewSettings?.Invoke();
                requestRepaint?.Invoke();
            }
        }

        public Rect GetZoomControlsRect(Rect area)
        {
            float width = 34f * zoomControlScale;
            float height = 180f * zoomControlScale;
            float marginRight = 8f;
            float marginTop = 8f;
            return new Rect(area.xMax - width - marginRight, area.y + marginTop, width, height);
        }

        public bool HandleScrollWheel(Event e, Rect graphRect, ref float zoom, ref Vector2 panOffset, bool isReadOnly, Action saveEditorViewSettings)
        {
            if (e.type != EventType.ScrollWheel) return false;

            float oldZoom = zoom;
            float zoomDelta = -e.delta.y * 0.05f;
            float newZoom = Mathf.Clamp(zoom + zoomDelta, minZoom, maxZoom);
            if (Mathf.Approximately(newZoom, oldZoom)) return false;

            Vector2 graphLocalMouse = e.mousePosition - graphRect.position;
            Vector2 worldMouse = (graphLocalMouse / oldZoom) - panOffset;
            zoom = newZoom;
            panOffset = (graphLocalMouse / zoom) - worldMouse;
            if (!isReadOnly) saveEditorViewSettings?.Invoke();
            return true;
        }
        #endregion
    }
}