using System.Collections.Generic;
using System.Linq;
using System;
using ConversationScheme;
using UnityEngine;
using UnityEngine.UI;
using BodyPartData = ConversationScheme.BodyPart;
namespace ConversationActorBuilder
{
    public static class ActorSceneBuilder
    {
        private class DelegateSpriteProvider : IActorSpriteProvider
        {
            private readonly Func<string, Sprite> spriteResolver;
            public DelegateSpriteProvider(Func<string, Sprite> spriteResolver)
            {
                this.spriteResolver = spriteResolver;
            }
            public Sprite LoadSprite(string resourcePath)
            {
                if (spriteResolver == null) return null;
                return spriteResolver(resourcePath);
            }
        }
        #region Build Entry
        public static GameObject BuildActorRoot(Actor actorData, bool useCanvas, Canvas targetCanvas, string newCanvasName, string rootName, Vector2 actorSize, float scale, Vector3 worldPosition, Vector2 canvasAnchoredPosition, Func<string, Sprite> spriteResolver)
        {
            ActorBuildOptions options = new ActorBuildOptions
            {
                TargetType = useCanvas ? ActorRenderTargetType.Canvas : ActorRenderTargetType.World,
                TargetCanvas = targetCanvas,
                NewCanvasName = newCanvasName,
                RootName = rootName,
                ActorSize = actorSize,
                Scale = scale,
                WorldPosition = worldPosition,
                CanvasAnchoredPosition = canvasAnchoredPosition
            };
            IActorSpriteProvider provider = new DelegateSpriteProvider(spriteResolver);
            ActorBuiltActor builtActor = BuildActor(actorData, options, provider);
            if (builtActor == null) return null;
            return builtActor.gameObject;
        }
        public static ActorBuiltActor BuildActor(Actor actorData, ActorBuildOptions options, IActorSpriteProvider spriteProvider)
        {
            if (actorData == null) return null;
            if (options == null) options = new ActorBuildOptions();
            float scale = options.Scale <= 0f ? 1f : options.Scale;
            Transform rootParent = ResolveRootParent(options);
            GameObject rootObject = CreateRootObject(actorData, options, rootParent);
            if (rootObject == null) return null;
            ActorBuiltActor builtActor = rootObject.GetComponent<ActorBuiltActor>();
            if (builtActor == null) builtActor = rootObject.AddComponent<ActorBuiltActor>();
            Dictionary<int, Transform> attachTargets = new Dictionary<int, Transform> { { 0, rootObject.transform } };
            List<BodyPartData> pendingBodyParts = actorData.BodyParts == null ? new List<BodyPartData>() : actorData.BodyParts.Where(x => x != null).ToList();
            int maxIterations = pendingBodyParts.Count + 1;
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                bool progressed = false;
                for (int bodyPartIndex = pendingBodyParts.Count - 1; bodyPartIndex >= 0; bodyPartIndex--)
                {
                    BodyPartData bodyPart = pendingBodyParts[bodyPartIndex];
                    if (!attachTargets.TryGetValue(bodyPart.AttachToPivotId, out Transform attachTarget)) continue;
                    ActorBuiltBodyPart builtBodyPart = CreateBodyPart(bodyPart, attachTarget, actorData, options, spriteProvider, scale, bodyPartIndex);
                    if (builtBodyPart == null) continue;
                    builtActor.RegisterBodyPart(builtBodyPart);
                    List<ActorBuiltPivotPoint> pivots = builtBodyPart.GetPivots();
                    for (int i = 0; i < pivots.Count; i++)
                    {
                        ActorBuiltPivotPoint pivot = pivots[i];
                        if (pivot == null) continue;
                        if (attachTargets.ContainsKey(pivot.PivotId)) continue;
                        attachTargets[pivot.PivotId] = pivot.transform;
                    }
                    pendingBodyParts.RemoveAt(bodyPartIndex);
                    progressed = true;
                }
                if (!progressed) break;
            }
            for (int i = 0; i < pendingBodyParts.Count; i++)
            {
                BodyPartData bodyPart = pendingBodyParts[i];
                ActorBuiltBodyPart builtBodyPart = CreateBodyPart(bodyPart, rootObject.transform, actorData, options, spriteProvider, scale, i);
                if (builtBodyPart == null) continue;
                builtActor.RegisterBodyPart(builtBodyPart);
            }
            builtActor.RebuildCache();
            return builtActor;
        }
        #endregion

        #region Root Build
        private static Transform ResolveRootParent(ActorBuildOptions options)
        {
            switch (options.TargetType)
            {
                case ActorRenderTargetType.Canvas:
                    Canvas canvas = options.TargetCanvas;
                    if (canvas == null)
                    {
                        GameObject canvasObject = new GameObject(string.IsNullOrWhiteSpace(options.NewCanvasName) ? "ActorCanvas" : options.NewCanvasName);
                        canvas = canvasObject.AddComponent<Canvas>();
                        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        canvasObject.AddComponent<CanvasScaler>();
                        canvasObject.AddComponent<GraphicRaycaster>();
                    }
                    return canvas.transform;
                default:
                    return null;
            }
        }
        private static GameObject CreateRootObject(Actor actorData, ActorBuildOptions options, Transform rootParent)
        {
            string actorName = string.IsNullOrWhiteSpace(options.RootName) ? actorData.Id : options.RootName;
            if (string.IsNullOrWhiteSpace(actorName)) actorName = "Actor";
            GameObject rootObject = new GameObject(actorName);
            if (rootParent != null) rootObject.transform.SetParent(rootParent, false);
            switch (options.TargetType)
            {
                case ActorRenderTargetType.Canvas:
                    RectTransform rectTransform = rootObject.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = options.CanvasAnchoredPosition;
                    rectTransform.sizeDelta = ResolveActorSize(actorData, options);
                    rectTransform.localScale = Vector3.one * (options.Scale <= 0f ? 1f : options.Scale);
                    break;
                default:
                    rootObject.transform.position = options.WorldPosition;
                    rootObject.transform.localScale = Vector3.one;
                    break;
            }
            return rootObject;
        }
        private static Vector2 ResolveActorSize(Actor actorData, ActorBuildOptions options)
        {
            if (options.ActorSize.x > 0f && options.ActorSize.y > 0f) return options.ActorSize;
            Vector2 actorSize = actorData.Size;
            if (actorSize.x <= 0f) actorSize.x = 300f;
            if (actorSize.y <= 0f) actorSize.y = 400f;
            return actorSize;
        }
        #endregion

        #region BodyPart Build
        private static ActorBuiltBodyPart CreateBodyPart(BodyPartData bodyPart, Transform attachTarget, Actor actorData, ActorBuildOptions options, IActorSpriteProvider spriteProvider, float scale, int sortingOrder)
        {
            if (bodyPart == null || attachTarget == null) return null;
            GameObject bodyPartObject = new GameObject(string.IsNullOrWhiteSpace(bodyPart.Name) ? "bodypart_" + bodyPart.Id : bodyPart.Name);
            bodyPartObject.transform.SetParent(attachTarget, false);
            bodyPartObject.transform.localPosition = Vector3.zero;
            bodyPartObject.transform.localRotation = Quaternion.identity;
            bodyPartObject.transform.localScale = Vector3.one;
            ActorBuiltBodyPart builtBodyPart = bodyPartObject.AddComponent<ActorBuiltBodyPart>();
            builtBodyPart.ConfigureMetadata(bodyPart.Id, bodyPart.Name, bodyPart.AttachToPivotId);
            float worldPivotMultiplier = ResolveWorldPivotMultiplier(bodyPart, spriteProvider, options.TargetType, scale);
            if (bodyPart.NestedResources == null) bodyPart.NestedResources = new List<BodyPartResource>();
            for (int i = 0; i < bodyPart.NestedResources.Count; i++)
            {
                BodyPartResource nestedResource = bodyPart.NestedResources[i];
                if (nestedResource == null) continue;
                GameObject resourceObject = new GameObject(string.IsNullOrWhiteSpace(nestedResource.Id) ? "resource_" + i : nestedResource.Id);
                resourceObject.transform.SetParent(bodyPartObject.transform, false);
                resourceObject.transform.localPosition = Vector3.zero;
                resourceObject.transform.localRotation = Quaternion.identity;
                resourceObject.transform.localScale = Vector3.one;
                Sprite sprite = spriteProvider == null ? null : spriteProvider.LoadSprite(nestedResource.Path ?? "");
                ConfigureVisual(resourceObject, sprite, options.TargetType, scale, sortingOrder);
                List<PivotPoint> overridePivots = nestedResource.PivotPoints == null ? new List<PivotPoint>() : nestedResource.PivotPoints.Where(x => x != null).Select(x => new PivotPoint { Id = x.Id, Name = x.Name, X = x.X, Y = x.Y }).ToList();
                builtBodyPart.RegisterResource(nestedResource.Id, resourceObject, overridePivots);
            }
            List<PivotPoint> allPivots = CollectAllPivotPoints(bodyPart);
            for (int i = 0; i < allPivots.Count; i++)
            {
                PivotPoint pivotPoint = allPivots[i];
                if (pivotPoint == null) continue;
                GameObject pivotObject = new GameObject(string.IsNullOrWhiteSpace(pivotPoint.Name) ? "pivot_" + pivotPoint.Id : pivotPoint.Name);
                pivotObject.transform.SetParent(bodyPartObject.transform, false);
                ActorBuiltPivotPoint builtPivot = pivotObject.AddComponent<ActorBuiltPivotPoint>();
                builtPivot.ConfigureBase(pivotPoint.Id, pivotPoint.Name, new Vector2(pivotPoint.X, pivotPoint.Y));
                if (options.TargetType == ActorRenderTargetType.World) builtPivot.SetPositionMultiplier(new Vector2(worldPivotMultiplier, worldPivotMultiplier));
                builtBodyPart.RegisterPivot(builtPivot);
            }
            builtBodyPart.SetCurrentResource(bodyPart.CurrentResourceId);
            return builtBodyPart;
        }
        private static List<PivotPoint> CollectAllPivotPoints(BodyPartData bodyPart)
        {
            Dictionary<int, PivotPoint> pivotById = new Dictionary<int, PivotPoint>();
            if (bodyPart?.PivotPoints != null)
            {
                for (int i = 0; i < bodyPart.PivotPoints.Count; i++)
                {
                    PivotPoint pivotPoint = bodyPart.PivotPoints[i];
                    if (pivotPoint == null) continue;
                    if (pivotById.ContainsKey(pivotPoint.Id)) continue;
                    pivotById[pivotPoint.Id] = new PivotPoint { Id = pivotPoint.Id, Name = pivotPoint.Name, X = pivotPoint.X, Y = pivotPoint.Y };
                }
            }
            if (bodyPart?.NestedResources != null)
            {
                for (int i = 0; i < bodyPart.NestedResources.Count; i++)
                {
                    BodyPartResource nestedResource = bodyPart.NestedResources[i];
                    if (nestedResource?.PivotPoints == null) continue;
                    for (int pivotIndex = 0; pivotIndex < nestedResource.PivotPoints.Count; pivotIndex++)
                    {
                        PivotPoint pivotPoint = nestedResource.PivotPoints[pivotIndex];
                        if (pivotPoint == null) continue;
                        if (pivotById.ContainsKey(pivotPoint.Id)) continue;
                        pivotById[pivotPoint.Id] = new PivotPoint { Id = pivotPoint.Id, Name = pivotPoint.Name, X = pivotPoint.X, Y = pivotPoint.Y };
                    }
                }
            }
            return pivotById.Values.ToList();
        }
        private static void ConfigureVisual(GameObject resourceObject, Sprite sprite, ActorRenderTargetType targetType, float scale, int sortingOrder)
        {
            switch (targetType)
            {
                case ActorRenderTargetType.Canvas:
                    RectTransform rectTransform = resourceObject.AddComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    Vector2 size = sprite == null ? new Vector2(160f, 160f) : new Vector2(sprite.rect.width, sprite.rect.height);
                    rectTransform.sizeDelta = size * scale;
                    Image image = resourceObject.AddComponent<Image>();
                    image.sprite = sprite;
                    image.raycastTarget = false;
                    break;
                default:
                    SpriteRenderer spriteRenderer = resourceObject.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.sortingOrder = sortingOrder;
                    resourceObject.transform.localPosition = ResolveWorldSpriteOffset(sprite, scale);
                    resourceObject.transform.localScale = Vector3.one * scale;
                    break;
            }
        }
        private static float ResolveWorldPivotMultiplier(BodyPartData bodyPart, IActorSpriteProvider spriteProvider, ActorRenderTargetType targetType, float scale)
        {
            if (targetType != ActorRenderTargetType.World) return 1f;
            Sprite referenceSprite = ResolveBodyPartReferenceSprite(bodyPart, spriteProvider);
            float pixelsPerUnit = referenceSprite == null || referenceSprite.pixelsPerUnit <= 0f ? 100f : referenceSprite.pixelsPerUnit;
            return scale / pixelsPerUnit;
        }
        private static Sprite ResolveBodyPartReferenceSprite(BodyPartData bodyPart, IActorSpriteProvider spriteProvider)
        {
            if (bodyPart?.NestedResources == null || spriteProvider == null) return null;
            BodyPartResource currentResource = bodyPart.NestedResources.FirstOrDefault(x => x != null && x.Id == (bodyPart.CurrentResourceId ?? ""));
            if (currentResource != null)
            {
                Sprite currentSprite = spriteProvider.LoadSprite(currentResource.Path ?? "");
                if (currentSprite != null) return currentSprite;
            }
            for (int i = 0; i < bodyPart.NestedResources.Count; i++)
            {
                BodyPartResource nestedResource = bodyPart.NestedResources[i];
                if (nestedResource == null) continue;
                Sprite nestedSprite = spriteProvider.LoadSprite(nestedResource.Path ?? "");
                if (nestedSprite != null) return nestedSprite;
            }
            return null;
        }
        private static Vector3 ResolveWorldSpriteOffset(Sprite sprite, float scale)
        {
            if (sprite == null) return Vector3.zero;
            float pixelsPerUnit = sprite.pixelsPerUnit <= 0f ? 100f : sprite.pixelsPerUnit;
            Vector2 center = sprite.rect.size * 0.5f;
            Vector2 offsetPixels = sprite.pivot - center;
            return new Vector3((offsetPixels.x / pixelsPerUnit) * scale, (offsetPixels.y / pixelsPerUnit) * scale, 0f);
        }
        #endregion
    }
}
