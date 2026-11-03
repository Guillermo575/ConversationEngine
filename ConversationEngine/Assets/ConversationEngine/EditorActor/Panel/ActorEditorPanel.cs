using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using ConversationEditorActor.Helper;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
using BodyPartData = ConversationScheme.BodyPart;
namespace ConversationEditorActor.Panel
{
    public class ActorEditorPanel
    {
        #region State
        private Vector2 scrollPosition;
        #endregion

        #region Draw
        public bool Draw(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds, string actorAssetPath, System.Action<int, int> onPreviewResourceRequested)
        {
            if (actor == null) return false;
            if (hiddenBodyPartIds == null) hiddenBodyPartIds = new HashSet<int>();
            bool changed = false;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            changed |= DrawActorFields(actor, actorAssetPath);
            EditorGUILayout.Space(6f);
            changed |= DrawBodyPartList(actor, ref selectedBodyPartId, hiddenBodyPartIds);
            EditorGUILayout.Space(6f);
            changed |= DrawSelectedBodyPart(actor, ref selectedBodyPartId, hiddenBodyPartIds, actorAssetPath, onPreviewResourceRequested);
            EditorGUILayout.EndScrollView();
            return changed;
        }
        private bool DrawActorFields(Actor actor, string actorAssetPath)
        {
            bool changed = false;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Actor", "Main actor properties."), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            actor.Id = EditorGUILayout.TextField(new GUIContent("Id", "Unique actor id."), actor.Id);
            actor.Size = EditorGUILayout.Vector2Field(new GUIContent("Canvas Size", "Actor canvas size. Its center is the main pivot point."), actor.Size);
            if (EditorGUI.EndChangeCheck()) changed = true;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Icon Path", "Icon path used by conversation resources."), GUILayout.Width(60f));
            string iconPathLabel = string.IsNullOrEmpty(actor.IconPath) ? "(no file selected)" : actor.IconPath;
            string iconPathTooltip = "Read-only icon path relative to the .actor root folder. GUID: " + (string.IsNullOrWhiteSpace(actor.IconPathGUID) ? "(empty)" : actor.IconPathGUID);
            EditorGUILayout.LabelField(new GUIContent(iconPathLabel, iconPathTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (GUILayout.Button(new GUIContent("...", "Change icon file path."), GUILayout.Width(28f)))
            {
                string changedPath = SelectSingleResourcePath(actorAssetPath, actor.IconPath);
                if (!string.IsNullOrEmpty(changedPath))
                {
                    actor.IconPath = changedPath;
                    actor.IconPathGUID = ActorEditorHelpers.GetResourceGuidFromPath(actorAssetPath, changedPath);
                    changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            if (ActorEditorHelpers.IsMissingResource(actorAssetPath, actor.IconPathGUID, actor.IconPath))
            {
                GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                missingStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField(new GUIContent("Missing file: icon was not found by GUID or Path.", "The icon cannot be resolved using GUID or Path."), missingStyle);
            }
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Sound Effects", "Audio cues associated with this actor."), EditorStyles.miniBoldLabel);
            if (DrawAddButton("Add a new sound effect path."))
            {
                actor.SoundEffectPaths.Add("");
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            if (actor.SoundEffectPaths == null) actor.SoundEffectPaths = new List<string>();
            for (int i = 0; i < actor.SoundEffectPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                actor.SoundEffectPaths[i] = EditorGUILayout.TextField(new GUIContent("Path " + i, "Sound effect path."), actor.SoundEffectPaths[i]);
                if (EditorGUI.EndChangeCheck()) changed = true;
                if (DrawDeleteButton("Remove sound effect path."))
                {
                    if (!EditorUtility.DisplayDialog("Remove Sound Effect", "Remove this sound effect path?", "Remove", "Cancel"))
                    {
                        EditorGUILayout.EndHorizontal();
                        continue;
                    }
                    actor.SoundEffectPaths.RemoveAt(i);
                    changed = true;
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawBodyPartList(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds)
        {
            bool changed = false;
            if (actor.BodyParts == null) actor.BodyParts = new List<BodyPartData>();
            if (EnsureMainBodyPartFirst(actor.BodyParts)) changed = true;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("BodyParts", "Create, select and delete body parts."), EditorStyles.boldLabel);
            if (DrawAddButton("Create a new body part."))
            {
                int maxId = 0;
                foreach (var existingBodyPart in actor.BodyParts)
                {
                    if (existingBodyPart == null) continue;
                    if (existingBodyPart.Id > maxId) maxId = existingBodyPart.Id;
                }
                int nextId = maxId + 1;
                if (nextId <= 0) nextId = 1;
                actor.BodyParts.Add(new BodyPartData
                {
                    Id = nextId,
                    Name = "bodypart_" + nextId,
                    AttachToPivotId = 0,
                    CurrentResourceId = "",
                    NestedResources = new List<BodyPartResource>(),
                    PivotPoints = new List<PivotPoint>()
                });
                selectedBodyPartId = nextId;
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < actor.BodyParts.Count; i++)
            {
                BodyPartData bodyPart = actor.BodyParts[i];
                if (bodyPart == null) continue;
                bool isMainBodyPart = IsMainBodyPart(bodyPart);
                bool isSelected = bodyPart.Id == selectedBodyPartId;
                bool isVisible = !hiddenBodyPartIds.Contains(bodyPart.Id);
                bool canDeleteBodyPart = !isMainBodyPart;
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                bool newVisible = EditorGUILayout.ToggleLeft(new GUIContent("", "Show or hide this body part in graph preview."), isVisible, GUILayout.Width(20f));
                if (EditorGUI.EndChangeCheck())
                {
                    if (newVisible) hiddenBodyPartIds.Remove(bodyPart.Id);
                    else hiddenBodyPartIds.Add(bodyPart.Id);
                    changed = true;
                }
                GUIStyle buttonStyle = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                string title = string.IsNullOrEmpty(bodyPart.Name) ? "bodypart" : bodyPart.Name;
                string hiddenSuffix = isVisible ? "" : " (Hidden)";
                if (GUILayout.Button(new GUIContent(title + " [" + bodyPart.Id + "]" + hiddenSuffix, "Select this body part."), buttonStyle)) selectedBodyPartId = bodyPart.Id;
                if (!isMainBodyPart)
                {
                    bool canMoveUp = i > 1;
                    bool canMoveDown = i < actor.BodyParts.Count - 1;
                    using (new EditorGUI.DisabledScope(!canMoveUp))
                    {
                        if (DrawReorderButton("Up", "Move this body part one position up."))
                        {
                            SwapBodyParts(actor.BodyParts, i, i - 1);
                            changed = true;
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                    }
                    using (new EditorGUI.DisabledScope(!canMoveDown))
                    {
                        if (DrawReorderButton("Down", "Move this body part one position down."))
                        {
                            SwapBodyParts(actor.BodyParts, i, i + 1);
                            changed = true;
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                    }
                }
                else GUILayout.Space(88f);
                using (new EditorGUI.DisabledScope(!canDeleteBodyPart))
                {
                    if (DrawDeleteButton("Delete this body part."))
                    {
                        if (!EditorUtility.DisplayDialog("Delete BodyPart", "Delete this body part?", "Delete", "Cancel"))
                        {
                            EditorGUILayout.EndHorizontal();
                            continue;
                        }
                        actor.BodyParts.RemoveAt(i);
                        hiddenBodyPartIds.Remove(bodyPart.Id);
                        if (selectedBodyPartId == bodyPart.Id) selectedBodyPartId = actor.BodyParts.Count > 0 ? actor.BodyParts[0].Id : 0;
                        changed = true;
                        EditorGUILayout.EndHorizontal();
                        i--;
                        continue;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawSelectedBodyPart(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds, string actorAssetPath, System.Action<int, int> onPreviewResourceRequested)
        {
            int selectedId = selectedBodyPartId;
            BodyPartData bodyPart = null;
            foreach (var existingBodyPart in actor.BodyParts)
            {
                if (existingBodyPart == null) continue;
                if (existingBodyPart.Id != selectedId) continue;
                bodyPart = existingBodyPart;
                break;
            }
            if (bodyPart == null)
            {
                EditorGUILayout.HelpBox("Select a BodyPart to edit its properties.", MessageType.Info);
                return false;
            }
            bool changed = false;
            if (bodyPart.PivotPoints == null) bodyPart.PivotPoints = new List<PivotPoint>();
            if (bodyPart.NestedResources == null) bodyPart.NestedResources = new List<BodyPartResource>();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("BodyPart: " + bodyPart.Name, "Selected body part settings."), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(new GUIContent("Id: " + bodyPart.Id, "Unique body part id inside actor."), EditorStyles.miniLabel);
            EditorGUI.BeginChangeCheck();
            bodyPart.Name = EditorGUILayout.TextField(new GUIContent("Name", "Display name for this body part."), bodyPart.Name);
            List<int> attachPivotIds = new List<int>();
            List<string> attachPivotLabels = new List<string>();
            BuildAttachPivotOptions(actor, attachPivotIds, attachPivotLabels);
            int attachPivotIndex = Mathf.Max(0, attachPivotIds.IndexOf(bodyPart.AttachToPivotId));
            int newAttachPivotIndex = EditorGUILayout.Popup(new GUIContent("Attach To Pivot", "Pivot id used as attach point. 0 means actor main pivot."), attachPivotIndex, attachPivotLabels.ToArray());
            bodyPart.AttachToPivotId = attachPivotIds[newAttachPivotIndex];
            List<string> resourceIds = new List<string> { "" };
            List<string> resourceLabels = new List<string> { "Auto (First Resource)" };
            for (int resourceIndex = 0; resourceIndex < bodyPart.NestedResources.Count; resourceIndex++)
            {
                BodyPartResource nestedResource = bodyPart.NestedResources[resourceIndex];
                if (nestedResource == null) continue;
                string nestedResourceId = string.IsNullOrEmpty(nestedResource.Id) ? "resource_" + resourceIndex : nestedResource.Id;
                resourceIds.Add(nestedResourceId);
                resourceLabels.Add(nestedResourceId);
            }
            int currentResourceIndex = Mathf.Max(0, resourceIds.IndexOf(bodyPart.CurrentResourceId ?? ""));
            using (new EditorGUI.DisabledScope(resourceLabels.Count <= 1))
            {
                int newResourceIndex = EditorGUILayout.Popup(new GUIContent("Current Resource", "Active resource used for preview and runtime."), currentResourceIndex, resourceLabels.ToArray());
                bodyPart.CurrentResourceId = resourceIds[newResourceIndex];
            }
            if (EditorGUI.EndChangeCheck()) changed = true;
            if (resourceLabels.Count <= 1) EditorGUILayout.HelpBox("Add Nested Resources to enable Current Resource selection.", MessageType.None);
            bool isVisible = !hiddenBodyPartIds.Contains(bodyPart.Id);
            EditorGUI.BeginChangeCheck();
            bool newVisible = EditorGUILayout.Toggle(new GUIContent("Visible In Graph", "Show or hide this body part and its pivots in graph preview."), isVisible);
            if (EditorGUI.EndChangeCheck())
            {
                if (newVisible) hiddenBodyPartIds.Remove(bodyPart.Id);
                else hiddenBodyPartIds.Add(bodyPart.Id);
                changed = true;
            }
            EditorGUILayout.Space(4f);
            changed |= DrawPivotPointList(bodyPart.PivotPoints, "BodyPart PivotPoints", "Add Pivot", "Base pivots for this body part.");
            EditorGUILayout.Space(4f);
            changed |= DrawNestedResources(bodyPart, actorAssetPath, onPreviewResourceRequested);
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawPivotPointList(List<PivotPoint> pivotPoints, string title, string addButtonText, string tooltip)
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent(title, tooltip), EditorStyles.miniBoldLabel);
            if (DrawAddButton(addButtonText))
            {
                int nextId = pivotPoints.Count == 0 ? 1 : pivotPoints.Max(x => x.Id) + 1;
                pivotPoints.Add(new PivotPoint { Id = nextId, Name = "pivot_" + nextId, X = 0f, Y = 0f });
                changed = true;
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < pivotPoints.Count; i++)
            {
                PivotPoint pivotPoint = pivotPoints[i];
                if (pivotPoint == null)
                {
                    pivotPoints[i] = new PivotPoint();
                    pivotPoint = pivotPoints[i];
                }
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Pivot " + i, "Editable pivot point."), EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField(new GUIContent("Id: " + pivotPoint.Id, "Pivot id used for attachments and overrides."), EditorStyles.miniLabel, GUILayout.Width(56f));
                if (DrawDeleteButton("Delete this pivot point."))
                {
                    if (!EditorUtility.DisplayDialog("Delete Pivot", "Delete this pivot point?", "Delete", "Cancel"))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    pivotPoints.RemoveAt(i);
                    changed = true;
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginChangeCheck();
                pivotPoint.Name = EditorGUILayout.TextField(new GUIContent("Name", "Pivot descriptive name."), pivotPoint.Name);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("X", "Horizontal local pivot position."), GUILayout.Width(14f));
                pivotPoint.X = EditorGUILayout.FloatField(pivotPoint.X);
                EditorGUILayout.LabelField(new GUIContent("Y", "Vertical local pivot position."), GUILayout.Width(14f));
                pivotPoint.Y = EditorGUILayout.FloatField(pivotPoint.Y);
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck()) changed = true;
                EditorGUILayout.EndVertical();
            }
            return changed;
        }
        private bool DrawNestedResources(BodyPartData bodyPart, string actorAssetPath, System.Action<int, int> onPreviewResourceRequested)
        {
            bool changed = false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Nested Resources", "Resources available for this body part."), EditorStyles.miniBoldLabel);
            if (DrawAddButton("Add a new nested resource for this body part."))
            {
                List<string> selectedPaths = SelectMultipleResourcePaths(actorAssetPath);
                foreach (string selectedPath in selectedPaths)
                {
                    bodyPart.NestedResources.Add(new BodyPartResource
                    {
                        Id = ActorEditorHelpers.BuildResourceIdFromPath(selectedPath),
                        Path = selectedPath,
                        GUID = ActorEditorHelpers.GetResourceGuidFromPath(actorAssetPath, selectedPath),
                        PivotPoints = new List<PivotPoint>()
                    });
                    changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < bodyPart.NestedResources.Count; i++)
            {
                BodyPartResource nestedResource = bodyPart.NestedResources[i];
                if (nestedResource == null)
                {
                    nestedResource = new BodyPartResource();
                    bodyPart.NestedResources[i] = nestedResource;
                }
                if (nestedResource.PivotPoints == null) nestedResource.PivotPoints = new List<PivotPoint>();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Resource " + i, "Preview this nested resource in graph."), EditorStyles.miniButtonLeft)) onPreviewResourceRequested?.Invoke(bodyPart.Id, i);
                if (DrawDeleteButton("Delete this resource."))
                {
                    if (!EditorUtility.DisplayDialog("Delete Resource", "Delete this nested resource?", "Delete", "Cancel"))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    bodyPart.NestedResources.RemoveAt(i);
                    changed = true;
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginChangeCheck();
                nestedResource.Id = EditorGUILayout.TextField(new GUIContent("Id", "Resource id."), nestedResource.Id);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Path", "Resource path relative to the .actor root folder."), GUILayout.Width(36f));
                string resourcePathLabel = string.IsNullOrEmpty(nestedResource.Path) ? "(no file selected)" : nestedResource.Path;
                string resourcePathTooltip = "Read-only resource path relative to the .actor root folder. GUID: " + (string.IsNullOrWhiteSpace(nestedResource.GUID) ? "(empty)" : nestedResource.GUID);
                EditorGUILayout.LabelField(new GUIContent(resourcePathLabel, resourcePathTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button(new GUIContent("...", "Change resource file path."), GUILayout.Width(28f)))
                {
                    string changedPath = SelectSingleResourcePath(actorAssetPath, nestedResource.Path);
                    if (!string.IsNullOrEmpty(changedPath))
                    {
                        nestedResource.Path = changedPath;
                        nestedResource.GUID = ActorEditorHelpers.GetResourceGuidFromPath(actorAssetPath, changedPath);
                        changed = true;
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (ActorEditorHelpers.IsMissingResource(nestedResource, actorAssetPath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: resource was not found by GUID or Path.", "The resource cannot be resolved using GUID or Path."), missingStyle);
                }
                if (EditorGUI.EndChangeCheck()) changed = true;
                changed |= DrawPivotPointList(nestedResource.PivotPoints, "Override PivotPoints", "Add Override Pivot", "Pivots that override the base body part pivots by id.");
                EditorGUILayout.EndVertical();
            }
            return changed;
        }
        private bool DrawAddButton(string tooltip)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.35f, 0.9f, 0.35f, 1f);
            GUIStyle addStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = EditorStyles.miniButton.fontSize + 2,
                fontStyle = FontStyle.Bold
            };
            bool clicked = GUILayout.Button(new GUIContent("+", tooltip), addStyle, GUILayout.Width(30f), GUILayout.Height(22f));
            GUI.color = previousColor;
            return clicked;
        }
        private bool DrawDeleteButton(string tooltip)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.9f, 0.3f, 0.3f, 1f);
            bool clicked = GUILayout.Button(new GUIContent("X", tooltip), GUILayout.Width(24f));
            GUI.color = previousColor;
            return clicked;
        }
        private bool DrawReorderButton(string label, string tooltip)
        {
            return GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Width(42f));
        }
        private bool IsMainBodyPart(BodyPartData bodyPart)
        {
            if (bodyPart == null) return false;
            return string.Equals(bodyPart.Name, "main", System.StringComparison.OrdinalIgnoreCase);
        }
        private bool EnsureMainBodyPartFirst(List<BodyPartData> bodyParts)
        {
            if (bodyParts == null || bodyParts.Count <= 1) return false;
            int mainIndex = -1;
            for (int i = 0; i < bodyParts.Count; i++)
            {
                if (!IsMainBodyPart(bodyParts[i])) continue;
                mainIndex = i;
                break;
            }
            if (mainIndex <= 0) return false;
            BodyPartData mainBodyPart = bodyParts[mainIndex];
            bodyParts.RemoveAt(mainIndex);
            bodyParts.Insert(0, mainBodyPart);
            return true;
        }
        private void SwapBodyParts(List<BodyPartData> bodyParts, int indexA, int indexB)
        {
            if (bodyParts == null) return;
            if (indexA < 0 || indexB < 0 || indexA >= bodyParts.Count || indexB >= bodyParts.Count) return;
            BodyPartData temp = bodyParts[indexA];
            bodyParts[indexA] = bodyParts[indexB];
            bodyParts[indexB] = temp;
        }
        private List<string> SelectMultipleResourcePaths(string actorAssetPath)
        {
            List<string> selectedPaths = new List<string>();
            if (string.IsNullOrWhiteSpace(actorAssetPath))
            {
                EditorUtility.DisplayDialog("Actor File Required", "Save the .actor file first so resources can be validated relative to its root folder.", "OK");
                return selectedPaths;
            }
            string actorFolderAbsolutePath = Path.GetDirectoryName(Path.GetFullPath(actorAssetPath));
            if (string.IsNullOrWhiteSpace(actorFolderAbsolutePath))
            {
                EditorUtility.DisplayDialog("Invalid Actor Path", "Could not resolve the .actor root folder.", "OK");
                return selectedPaths;
            }
            string absolutePath = EditorUtility.OpenFilePanelWithFilters("Select Resource File", actorFolderAbsolutePath, new[] { "Image or Asset", "png,jpg,jpeg,tga,bmp,gif,psd,asset" });
            if (string.IsNullOrEmpty(absolutePath)) return selectedPaths;
            if (!ActorEditorHelpers.TryGetResourcePathRelativeToActor(actorAssetPath, absolutePath, out string relativePath, out string errorMessage))
            {
                EditorUtility.DisplayDialog("Invalid Resource", errorMessage, "OK");
                return selectedPaths;
            }
            selectedPaths.Add(relativePath);
            return selectedPaths;
        }
        private string SelectSingleResourcePath(string actorAssetPath, string currentRelativePath)
        {
            if (string.IsNullOrWhiteSpace(actorAssetPath))
            {
                EditorUtility.DisplayDialog("Actor File Required", "Save the .actor file first so resources can be validated relative to its root folder.", "OK");
                return "";
            }
            string actorFolderAbsolutePath = Path.GetDirectoryName(Path.GetFullPath(actorAssetPath));
            if (string.IsNullOrWhiteSpace(actorFolderAbsolutePath))
            {
                EditorUtility.DisplayDialog("Invalid Actor Path", "Could not resolve the .actor root folder.", "OK");
                return "";
            }
            string startFolder = actorFolderAbsolutePath;
            if (!string.IsNullOrWhiteSpace(currentRelativePath))
            {
                string currentFullPath = Path.Combine(actorFolderAbsolutePath, currentRelativePath);
                if (File.Exists(currentFullPath))
                {
                    string currentDirectory = Path.GetDirectoryName(currentFullPath);
                    if (!string.IsNullOrWhiteSpace(currentDirectory)) startFolder = currentDirectory;
                }
            }
            string absolutePath = EditorUtility.OpenFilePanelWithFilters("Select Resource File", startFolder, new[] { "Image or Asset", "png,jpg,jpeg,tga,bmp,gif,psd,asset" });
            if (string.IsNullOrEmpty(absolutePath)) return "";
            if (!ActorEditorHelpers.TryGetResourcePathRelativeToActor(actorAssetPath, absolutePath, out string relativePath, out string errorMessage))
            {
                EditorUtility.DisplayDialog("Invalid Resource", errorMessage, "OK");
                return "";
            }
            return relativePath;
        }
        private void BuildAttachPivotOptions(Actor actor, List<int> pivotIds, List<string> pivotLabels)
        {
            pivotIds.Clear();
            pivotLabels.Clear();
            pivotIds.Add(0);
            pivotLabels.Add("Actor Main Pivot [0]");
            if (actor?.BodyParts == null) return;
            HashSet<int> usedIds = new HashSet<int> { 0 };
            foreach (var bodyPart in actor.BodyParts)
            {
                if (bodyPart == null) continue;
                if (bodyPart.PivotPoints != null)
                {
                    foreach (var pivotPoint in bodyPart.PivotPoints)
                    {
                        if (pivotPoint == null) continue;
                        if (usedIds.Contains(pivotPoint.Id)) continue;
                        usedIds.Add(pivotPoint.Id);
                        string pivotName = string.IsNullOrEmpty(pivotPoint.Name) ? "pivot" : pivotPoint.Name;
                        pivotIds.Add(pivotPoint.Id);
                        pivotLabels.Add(pivotName + " [" + pivotPoint.Id + "]");
                    }
                }
                if (bodyPart.NestedResources == null) continue;
                foreach (var nestedResource in bodyPart.NestedResources)
                {
                    if (nestedResource?.PivotPoints == null) continue;
                    foreach (var pivotPoint in nestedResource.PivotPoints)
                    {
                        if (pivotPoint == null) continue;
                        if (usedIds.Contains(pivotPoint.Id)) continue;
                        usedIds.Add(pivotPoint.Id);
                        string pivotName = string.IsNullOrEmpty(pivotPoint.Name) ? "pivot" : pivotPoint.Name;
                        pivotIds.Add(pivotPoint.Id);
                        pivotLabels.Add(pivotName + " [" + pivotPoint.Id + "]");
                    }
                }
            }
        }
        #endregion
    }
}
