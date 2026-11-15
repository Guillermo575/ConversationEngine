using System;
using System.Collections.Generic;
using ConversationEngine.ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEngine.EditorActor.Export
{
    public class ActorExportWindow : EditorWindow
    {
        #region State
        private Actor actorData;
        private Action<ActorExportRequest> onExport;
        private ActorExportTargetType targetType = ActorExportTargetType.Canvas;
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
            window.minSize = new Vector2(390f, 250f);
            window.maxSize = new Vector2(390f, 340f);
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
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel(new GUIContent("Canvas", "Select an existing canvas or choose NEW to create one."));
                    selectedCanvasIndex = EditorGUILayout.Popup(Mathf.Clamp(selectedCanvasIndex, 0, Mathf.Max(0, canvasLabels.Count - 1)), canvasLabels.ToArray());
                    if (GUILayout.Button(new GUIContent("Refresh", "Refresh available canvas objects in active scenes."), GUILayout.Width(80f))) RefreshCanvasOptions();
                    EditorGUILayout.EndHorizontal();
                    if (selectedCanvasIndex == 0) newCanvasName = EditorGUILayout.TextField(new GUIContent("New Canvas", "Name for the canvas that will be created."), newCanvasName);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(new GUIContent("Anchored Position", "UI local anchored position of the exported actor root."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                    EditorGUILayout.LabelField(new GUIContent("X", "Anchored Position X value."), GUILayout.Width(14f));
                    canvasPosition.x = EditorGUILayout.FloatField(canvasPosition.x);
                    EditorGUILayout.LabelField(new GUIContent("Y", "Anchored Position Y value."), GUILayout.Width(14f));
                    canvasPosition.y = EditorGUILayout.FloatField(canvasPosition.y);
                    EditorGUILayout.EndHorizontal();
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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Actor Size", "Root actor size used by canvas mode and metadata."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
            EditorGUILayout.LabelField(new GUIContent("X", "Actor Size X value."), GUILayout.Width(14f));
            actorSize.x = EditorGUILayout.FloatField(actorSize.x);
            EditorGUILayout.LabelField(new GUIContent("Y", "Actor Size Y value."), GUILayout.Width(14f));
            actorSize.y = EditorGUILayout.FloatField(actorSize.y);
            EditorGUILayout.LabelField(new GUIContent("Scale", "Scale multiplier applied to exported visual resources."), GUILayout.Width(36f));
            exportScale = EditorGUILayout.FloatField(exportScale);
            EditorGUILayout.EndHorizontal();
            if (exportScale <= 0f) exportScale = 1f;
            EditorGUILayout.EndVertical();
        }
        private void DrawActionButtons()
        {
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