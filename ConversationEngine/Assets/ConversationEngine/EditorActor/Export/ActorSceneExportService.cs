using ConversationScheme;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace ConversationEditorActor.Export
{
    public enum ActorExportTargetType
    {
        World,
        Canvas
    }
    public class ActorExportRequest
    {
        public ActorExportTargetType TargetType = ActorExportTargetType.Canvas;
        public Canvas TargetCanvas;
        public string NewCanvasName = "ActorCanvas";
        public string RootName = "";
        public Vector2 ActorSize = Vector2.zero;
        public float Scale = 1f;
        public Vector3 WorldPosition = Vector3.zero;
        public Vector2 CanvasAnchoredPosition = Vector2.zero;
    }
    public static class ActorSceneExportService
    {
        #region Export
        public static GameObject ExportToScene(ConversationScheme.Actor actorData, string actorAssetPath, ActorExportRequest request)
        {
            if (actorData == null) return null;
            GameObject actorRoot = ExportWithReflection(actorData, actorAssetPath, request);
            if (actorRoot == null) return null;
            RegisterUndoObjects(actorRoot);
            Selection.activeObject = actorRoot;
            EditorGUIUtility.PingObject(actorRoot);
            if (actorRoot.scene.IsValid()) EditorSceneManager.MarkSceneDirty(actorRoot.scene);
            return actorRoot;
        }
        private static GameObject ExportWithReflection(ConversationScheme.Actor actorData, string actorAssetPath, ActorExportRequest request)
        {
            Type builderType = Type.GetType("ConversationActorBuilder.ActorSceneBuilder, ConversationEngine.ActorBuilder");
            if (builderType == null) return null;
            MethodInfo buildMethod = builderType.GetMethod("BuildActorRoot", BindingFlags.Public | BindingFlags.Static);
            if (buildMethod == null) return null;
            Func<string, string, Sprite> spriteResolver = (guid, path) => LoadSprite(actorAssetPath, guid, path);
            object[] args = new object[]
            {
                actorData,
                request.TargetType == ActorExportTargetType.Canvas,
                request.TargetCanvas,
                request.NewCanvasName,
                request.RootName,
                request.ActorSize,
                request.Scale,
                request.WorldPosition,
                request.CanvasAnchoredPosition,
                spriteResolver
            };
            object result = buildMethod.Invoke(null, args);
            return result as GameObject;
        }
        private static void RegisterUndoObjects(GameObject actorRoot)
        {
            if (actorRoot == null) return;
            Undo.RegisterCreatedObjectUndo(actorRoot, "Export Actor To Scene");
            Canvas parentCanvas = actorRoot.GetComponentInParent<Canvas>();
            if (parentCanvas == null) return;
            Transform parentTransform = parentCanvas.transform.parent;
            if (parentTransform != null) return;
            Undo.RegisterCreatedObjectUndo(parentCanvas.gameObject, "Create Export Canvas");
        }
        private static Sprite LoadSprite(string actorAssetPath, string resourceGuid, string resourcePath)
        {
            if (!global::ConversationEditorActor.Helper.ActorEditorHelpers.TryResolveResourceAssetPath(actorAssetPath, resourceGuid, resourcePath, out string resolvedAssetPath, out bool _)) return null;
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            if (Path.IsPathRooted(resolvedAssetPath)) resolvedAssetPath = global::ConversationEditorActor.Helper.ActorEditorHelpers.ToAssetPath(resolvedAssetPath);
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(resolvedAssetPath);
            if (sprite != null) return sprite;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedAssetPath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        #endregion
    }
}
