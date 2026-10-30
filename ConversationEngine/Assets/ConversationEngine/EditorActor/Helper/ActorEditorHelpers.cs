using System.Collections.Generic;
using System.IO;
using System.Linq;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
using BodyPartData = ConversationScheme.BodyPart;
namespace ConversationEditorActor.Helper
{
    public static class ActorEditorHelpers
    {
        #region Actor Data
        public static Actor CreateDefaultActor()
        {
            Actor actor = new Actor
            {
                Id = "new_actor",
                Path = "",
                IconPath = "",
                SoundEffectPaths = new List<string>(),
                BodyParts = new List<BodyPartData>(),
                Size = new Vector2(300f, 400f)
            };
            actor.BodyParts.Add(new BodyPartData
            {
                Id = 1,
                Name = "main",
                AttachToPivotId = 0,
                CurrentResourceId = "",
                NestedResources = new List<BodyPartResource>(),
                PivotPoints = new List<PivotPoint>()
            });
            return actor;
        }
        public static string SerializeActor(Actor actor)
        {
            EnsureActorDefaults(actor);
            return JsonUtility.ToJson(actor, true);
        }
        public static Actor DeserializeActor(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return CreateDefaultActor();
            Actor actor = JsonUtility.FromJson<Actor>(json);
            if (actor == null) return CreateDefaultActor();
            EnsureActorDefaults(actor);
            return actor;
        }
        public static void EnsureActorDefaults(Actor actor)
        {
            if (actor == null) return;
            if (actor.SoundEffectPaths == null) actor.SoundEffectPaths = new List<string>();
            if (actor.BodyParts == null) actor.BodyParts = new List<BodyPartData>();
            foreach (var bodyPart in actor.BodyParts)
            {
                if (bodyPart == null) continue;
                if (bodyPart.Name == null) bodyPart.Name = "";
                if (bodyPart.CurrentResourceId == null) bodyPart.CurrentResourceId = "";
                if (bodyPart.NestedResources == null) bodyPart.NestedResources = new List<BodyPartResource>();
                if (bodyPart.PivotPoints == null) bodyPart.PivotPoints = new List<PivotPoint>();
                foreach (var nestedResource in bodyPart.NestedResources)
                {
                    if (nestedResource == null) continue;
                    if (nestedResource.PivotPoints == null) nestedResource.PivotPoints = new List<PivotPoint>();
                }
            }
            if (actor.Size == Vector2.zero) actor.Size = new Vector2(300f, 400f);
            if (actor.BodyParts.Count == 0)
            {
                actor.BodyParts.Add(new BodyPartData
                {
                    Id = 1,
                    Name = "main",
                    AttachToPivotId = 0,
                    CurrentResourceId = "",
                    NestedResources = new List<BodyPartResource>(),
                    PivotPoints = new List<PivotPoint>()
                });
            }
        }
        public static BodyPartResource GetActiveResource(BodyPartData bodyPart)
        {
            if (bodyPart == null || bodyPart.NestedResources == null || bodyPart.NestedResources.Count == 0) return null;
            if (!string.IsNullOrEmpty(bodyPart.CurrentResourceId))
            {
                var resource = bodyPart.NestedResources.FirstOrDefault(r => r != null && r.Id == bodyPart.CurrentResourceId);
                if (resource != null) return resource;
            }
            return bodyPart.NestedResources.FirstOrDefault(r => r != null);
        }
        public static List<PivotPoint> GetEffectivePivotPoints(BodyPartData bodyPart, BodyPartResource activeResource)
        {
            List<PivotPoint> effectivePivots = new List<PivotPoint>();
            if (bodyPart?.PivotPoints != null)
            {
                foreach (var pivot in bodyPart.PivotPoints)
                {
                    if (pivot == null) continue;
                    effectivePivots.Add(new PivotPoint { Id = pivot.Id, Name = pivot.Name, X = pivot.X, Y = pivot.Y });
                }
            }
            if (activeResource?.PivotPoints == null) return effectivePivots;
            foreach (var overridePivot in activeResource.PivotPoints)
            {
                if (overridePivot == null) continue;
                int index = effectivePivots.FindIndex(p => p.Id == overridePivot.Id);
                if (index >= 0)
                {
                    effectivePivots[index].Name = overridePivot.Name;
                    effectivePivots[index].X = overridePivot.X;
                    effectivePivots[index].Y = overridePivot.Y;
                }
                else
                {
                    effectivePivots.Add(new PivotPoint { Id = overridePivot.Id, Name = overridePivot.Name, X = overridePivot.X, Y = overridePivot.Y });
                }
            }
            return effectivePivots;
        }
        #endregion

        #region Path Utilities
        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";
            foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) path = Path.GetDirectoryName(path);
                break;
            }
            return path;
        }
        public static string GetUniqueFilePath(string folderPath, string baseName, string extension)
        {
            string fileName = baseName + extension;
            string fullPath = Path.Combine(folderPath, fileName);
            int counter = 1;
            while (File.Exists(fullPath))
            {
                fileName = baseName + counter + extension;
                fullPath = Path.Combine(folderPath, fileName);
                counter++;
            }
            return fullPath;
        }
        public static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return "";
            string dataPath = Application.dataPath.Replace("\\", "/");
            string normalizedPath = absolutePath.Replace("\\", "/");
            if (!normalizedPath.StartsWith(dataPath)) return "";
            return "Assets" + normalizedPath.Substring(dataPath.Length);
        }
        public static string ResolveResourceAssetPath(string actorAssetPath, string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return "";
            string normalizedResourcePath = resourcePath.Replace("\\", "/");
            if (normalizedResourcePath.StartsWith("Assets/")) return normalizedResourcePath;
            if (string.IsNullOrWhiteSpace(actorAssetPath)) return normalizedResourcePath;
            string actorFolderPath = Path.GetDirectoryName(actorAssetPath);
            if (string.IsNullOrWhiteSpace(actorFolderPath)) return normalizedResourcePath;
            string combinedPath = Path.Combine(actorFolderPath, normalizedResourcePath);
            return combinedPath.Replace("\\", "/");
        }
        public static bool IsSupportedResourceFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return false;
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".tga":
                case ".bmp":
                case ".gif":
                case ".psd":
                case ".asset":
                    return true;
                default:
                    return false;
            }
        }
        public static bool TryGetResourcePathRelativeToActor(string actorAssetPath, string absoluteResourcePath, out string relativePath, out string errorMessage)
        {
            relativePath = "";
            errorMessage = "";
            if (string.IsNullOrWhiteSpace(actorAssetPath))
            {
                errorMessage = "Save the .actor file first so resource paths can be validated against its root folder.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(absoluteResourcePath) || !File.Exists(absoluteResourcePath))
            {
                errorMessage = "Selected file does not exist.";
                return false;
            }
            if (!IsSupportedResourceFile(absoluteResourcePath))
            {
                errorMessage = "Only image files or .asset files are allowed.";
                return false;
            }
            string actorAbsolutePath = Path.GetFullPath(actorAssetPath).Replace("\\", "/");
            string actorRootFolder = Path.GetDirectoryName(actorAbsolutePath);
            if (string.IsNullOrWhiteSpace(actorRootFolder))
            {
                errorMessage = "Could not resolve actor root folder.";
                return false;
            }
            actorRootFolder = actorRootFolder.Replace("\\", "/");
            string normalizedFilePath = Path.GetFullPath(absoluteResourcePath).Replace("\\", "/");
            if (!normalizedFilePath.StartsWith(actorRootFolder + "/") && normalizedFilePath != actorRootFolder)
            {
                errorMessage = "Selected file must be inside the same root folder as the .actor file.";
                return false;
            }
            relativePath = normalizedFilePath.Substring(actorRootFolder.Length).TrimStart('/');
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                errorMessage = "Could not resolve a valid relative resource path.";
                return false;
            }
            return true;
        }
        public static string BuildResourceIdFromPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return "resource";
            string sanitized = new string(relativePath.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
            while (sanitized.Contains("__")) sanitized = sanitized.Replace("__", "_");
            sanitized = sanitized.Trim('_');
            if (string.IsNullOrWhiteSpace(sanitized)) return "resource";
            return sanitized;
        }
        #endregion
    }
}
