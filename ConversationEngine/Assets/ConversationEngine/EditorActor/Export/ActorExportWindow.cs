using System;
using System.Collections.Generic;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEditorActor.Export
{
    public class ActorExportWindow : EditorWindow
    {
        #region State
        private Actor actorData;
        private Action<ActorExportRequest> onExport;
        private ActorExportTargetType targetType = ActorExportTargetType.World;
        private string rootName = "";
        private Vector2 actorSize = Vector2.zero;
        private float exportScale = 1f;
        private Vector3 worldPosition = Vector3.zero;
        private Vector2 canvasPosition = Vector2.zero;
        private string newCanvasName = "ActorCanvas";
        private int selectedCanvasIndex;
        private readonly List<Canvas> canvasOptions = new List<Canvas>();
        private readonly List<string> canvasLabels = new List<string>();
        #endregion

        #region Open
        public static void Open(Actor actorData, Action<ActorExportRequest> onExport)
        {
            ActorExportWindow window = CreateInstance<ActorExportWindow>();
            window.actorData = actorData;
            window.onExport = onExport;
            window.rootName = actorData == null ? "" : actorData.Id;
            window.actorSize = actorData == null ? new Vector2(300f, 400f) : actorData.Size;
            if (window.actorSize.x <= 0f) window.actorSize.x = 300f;
            if (window.actorSize.y <= 0f) window.actorSize.y = 400f;
            window.titleContent = new GUIContent("Actor Export", "Configure export options for scene build.");
            window.minSize = new Vector2(420f, 320f);
            window.maxSize = new Vector2(420f, 460f);
            window.RefreshCanvasOptions();
            window.ShowUtility();
        }
        #endregion

        #region Unity
        private void OnEnable()
        {
            RefreshCanvasOptions();
        }
        private void OnGUI()
        {
            DrawTargetOptions();
            DrawLayoutOptions();
            DrawActionButtons();
        }
        #endregion

        #region Draw
        private void DrawTargetOptions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Target", "Where actor objects will be exported."), EditorStyles.boldLabel);
            targetType = (ActorExportTargetType)EditorGUILayout.EnumPopup(new GUIContent("Render Target", "Choose between world objects or UI canvas objects."), targetType);
            switch (targetType)
            {
                case ActorExportTargetType.Canvas:
                    if (GUILayout.Button(new GUIContent("Refresh Canvas List", "Refresh available canvas objects in active scenes."), GUILayout.Height(20f))) RefreshCanvasOptions();
                    selectedCanvasIndex = EditorGUILayout.Popup(new GUIContent("Canvas", "Select an existing canvas or choose NEW to create one."), Mathf.Clamp(selectedCanvasIndex, 0, Mathf.Max(0, canvasLabels.Count - 1)), canvasLabels.ToArray());
                    if (selectedCanvasIndex == 0) newCanvasName = EditorGUILayout.TextField(new GUIContent("New Canvas", "Name for the canvas that will be created."), newCanvasName);
                    canvasPosition = EditorGUILayout.Vector2Field(new GUIContent("Anchored Position", "UI local anchored position of the exported actor root."), canvasPosition);
                    break;
                default:
                    worldPosition = EditorGUILayout.Vector3Field(new GUIContent("World Position", "World position used by the exported actor root."), worldPosition);
                    break;
            }
            EditorGUILayout.EndVertical();
        }
        private void DrawLayoutOptions()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Layout", "Size and root options for export."), EditorStyles.boldLabel);
            rootName = EditorGUILayout.TextField(new GUIContent("Root Name", "Name used for the generated actor root object."), rootName);
            actorSize = EditorGUILayout.Vector2Field(new GUIContent("Actor Size", "Root actor size used by canvas mode and metadata."), actorSize);
            exportScale = EditorGUILayout.FloatField(new GUIContent("Scale", "Scale multiplier applied to exported visual resources."), exportScale);
            if (exportScale <= 0f) exportScale = 1f;
            EditorGUILayout.EndVertical();
        }
        private void DrawActionButtons()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Cancel", "Close this export window."), GUILayout.Height(24f))) Close();
            if (GUILayout.Button(new GUIContent("Export", "Build actor objects in scene with current settings."), GUILayout.Height(24f))) ExecuteExport();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region Actions
        private void ExecuteExport()
        {
            ActorExportRequest options = new ActorExportRequest
            {
                TargetType = targetType,
                RootName = rootName,
                ActorSize = actorSize,
                Scale = exportScale,
                WorldPosition = worldPosition,
                CanvasAnchoredPosition = canvasPosition,
                NewCanvasName = string.IsNullOrWhiteSpace(newCanvasName) ? "ActorCanvas" : newCanvasName
            };
            if (targetType == ActorExportTargetType.Canvas && selectedCanvasIndex > 0 && selectedCanvasIndex - 1 < canvasOptions.Count) options.TargetCanvas = canvasOptions[selectedCanvasIndex - 1];
            onExport?.Invoke(options);
            Close();
        }
        private void RefreshCanvasOptions()
        {
            canvasOptions.Clear();
            canvasLabels.Clear();
            canvasLabels.Add("NEW");
            Canvas[] sceneCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < sceneCanvases.Length; i++)
            {
                Canvas canvas = sceneCanvases[i];
                if (canvas == null) continue;
                canvasOptions.Add(canvas);
                canvasLabels.Add(canvas.name);
            }
            if (selectedCanvasIndex >= canvasLabels.Count) selectedCanvasIndex = 0;
        }
        #endregion
    }
}
