using ConversationEngine.ConversationScheme;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using ConversationEngine.EditorActor.Helper;
namespace ConversationEngine.EditorActor.Export
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
        public bool UnpackPrefabInstances;
        public string NewCanvasName = "ActorCanvas";
        public string RootName = "";
        public Vector2 ActorSize = Vector2.zero;
        public float Scale = 1f;
        public Vector3 WorldPosition = Vector3.zero;
        public Vector2 CanvasAnchoredPosition = Vector2.zero;
        public float WorldBodyPartZOffset = 0f;
    }
    public static class ActorSceneExportService
    {
        #region Export
        public static GameObject ExportToScene(Actor actorData, string actorAssetPath, ActorExportRequest request)
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
        private static GameObject ExportWithReflection(Actor actorData, string actorAssetPath, ActorExportRequest request)
        {
            Type builderType = Type.GetType("ConversationEngine.ActorBuilder.ActorSceneBuilder, ConversationEngine.ActorBuilder");
            if (builderType == null) return null;
            MethodInfo buildMethod = builderType.GetMethod("BuildActorRoot", BindingFlags.Public | BindingFlags.Static);
            if (buildMethod == null) return null;
            Func<string, string, Sprite> spriteResolver = (guid, path) => LoadSprite(actorAssetPath, guid, path);
            Func<string, string, Transform, string, GameObject> prefabFactory = (guid, path, parent, objectName) => CreatePrefabInstance(actorAssetPath, guid, path, parent, objectName, request.UnpackPrefabInstances);
            Func<string, string, UnityEngine.Object> objectResolver = (guid, path) => LoadObject(actorAssetPath, guid, path);
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
                request.WorldBodyPartZOffset,
                spriteResolver,
                prefabFactory,
                objectResolver
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
            if (!global::ConversationEngine.EditorActor.Helper.ActorEditorHelpers.TryResolveResourceAssetPath(actorAssetPath, resourceGuid, resourcePath, out string resolvedAssetPath, out bool _)) return null;
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            if (Path.IsPathRooted(resolvedAssetPath)) resolvedAssetPath = global::ConversationEngine.EditorActor.Helper.ActorEditorHelpers.ToAssetPath(resolvedAssetPath);
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            string extension = Path.GetExtension(resolvedAssetPath).ToLowerInvariant();
            if (extension == ".prefab") return LoadSpriteFromPrefabAsset(resolvedAssetPath);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(resolvedAssetPath);
            if (sprite != null) return sprite;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedAssetPath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        private static UnityEngine.Object LoadObject(string actorAssetPath, string resourceGuid, string resourcePath)
        {
            if (!ActorEditorHelpers.TryResolveResourceAssetPath(actorAssetPath, resourceGuid, resourcePath, out string resolvedAssetPath, out bool _)) return null;
            if (Path.IsPathRooted(resolvedAssetPath)) resolvedAssetPath = ActorEditorHelpers.ToAssetPath(resolvedAssetPath);
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            string extension = Path.GetExtension(resolvedAssetPath).ToLowerInvariant();
            switch (extension)
            {
                case ".prefab":
                    return AssetDatabase.LoadAssetAtPath<GameObject>(resolvedAssetPath);
                default:
                    UnityEngine.Object assetObject = AssetDatabase.LoadAssetAtPath<AudioClip>(resolvedAssetPath);
                    if (assetObject != null) return assetObject;
                    assetObject = AssetDatabase.LoadAssetAtPath<Sprite>(resolvedAssetPath);
                    if (assetObject != null) return assetObject;
                    assetObject = AssetDatabase.LoadAssetAtPath<Texture2D>(resolvedAssetPath);
                    if (assetObject != null) return assetObject;
                    return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(resolvedAssetPath);
            }
        }
        private static GameObject CreatePrefabInstance(string actorAssetPath, string resourceGuid, string resourcePath, Transform parent, string objectName, bool unpackPrefab)
        {
            if (!ActorEditorHelpers.TryResolveResourceAssetPath(actorAssetPath, resourceGuid, resourcePath, out string resolvedAssetPath, out bool _)) return null;
            if (Path.IsPathRooted(resolvedAssetPath)) resolvedAssetPath = ActorEditorHelpers.ToAssetPath(resolvedAssetPath);
            if (string.IsNullOrWhiteSpace(resolvedAssetPath)) return null;
            if (!string.Equals(Path.GetExtension(resolvedAssetPath), ".prefab", StringComparison.OrdinalIgnoreCase)) return null;
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(resolvedAssetPath);
            if (prefabAsset == null) return null;
            GameObject instance = PrefabUtility.InstantiatePrefab(prefabAsset) as GameObject;
            if (instance == null) return null;
            if (parent != null) instance.transform.SetParent(parent, false);
            instance.name = objectName;
            if (unpackPrefab && PrefabUtility.IsPartOfPrefabInstance(instance)) PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            return instance;
        }
        private static Sprite LoadSpriteFromPrefabAsset(string prefabAssetPath)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
            if (prefabAsset == null) return null;
            SpriteRenderer rootRenderer = prefabAsset.GetComponent<SpriteRenderer>();
            if (rootRenderer != null && rootRenderer.sprite != null) return rootRenderer.sprite;
            UnityEngine.UI.Image rootImage = prefabAsset.GetComponent<UnityEngine.UI.Image>();
            if (rootImage != null && rootImage.sprite != null) return rootImage.sprite;
            SpriteRenderer[] renderers = prefabAsset.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || renderer.sprite == null) continue;
                return renderer.sprite;
            }
            UnityEngine.UI.Image[] images = prefabAsset.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                UnityEngine.UI.Image image = images[i];
                if (image == null || image.sprite == null) continue;
                return image.sprite;
            }
            return null;
        }
        #endregion
    }
}
