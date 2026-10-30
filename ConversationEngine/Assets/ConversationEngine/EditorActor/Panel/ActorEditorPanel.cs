using System.Collections.Generic;
using System.Linq;
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
        public bool Draw(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds)
        {
            if (actor == null) return false;
            if (hiddenBodyPartIds == null) hiddenBodyPartIds = new HashSet<int>();
            bool changed = false;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            changed |= DrawActorFields(actor);
            EditorGUILayout.Space(6f);
            changed |= DrawBodyPartList(actor, ref selectedBodyPartId, hiddenBodyPartIds);
            EditorGUILayout.Space(6f);
            changed |= DrawSelectedBodyPart(actor, ref selectedBodyPartId, hiddenBodyPartIds);
            EditorGUILayout.EndScrollView();
            return changed;
        }
        private bool DrawActorFields(Actor actor)
        {
            bool changed = false;
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("Actor", "Main actor properties."), EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            actor.Id = EditorGUILayout.TextField(new GUIContent("Id", "Unique actor id."), actor.Id);
            actor.Path = EditorGUILayout.TextField(new GUIContent("Path", "Optional source path for this actor file."), actor.Path);
            actor.IconPath = EditorGUILayout.TextField(new GUIContent("Icon Path", "Icon path used by conversation resources."), actor.IconPath);
            actor.Size = EditorGUILayout.Vector2Field(new GUIContent("Canvas Size", "Actor canvas size. Its center is the main pivot point."), actor.Size);
            if (EditorGUI.EndChangeCheck()) changed = true;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(new GUIContent("Sound Effects", "Audio cues associated with this actor."), EditorStyles.miniBoldLabel);
            if (actor.SoundEffectPaths == null) actor.SoundEffectPaths = new List<string>();
            for (int i = 0; i < actor.SoundEffectPaths.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                actor.SoundEffectPaths[i] = EditorGUILayout.TextField(new GUIContent("Path " + i, "Sound effect path."), actor.SoundEffectPaths[i]);
                if (EditorGUI.EndChangeCheck()) changed = true;
                if (GUILayout.Button(new GUIContent("X", "Remove sound effect path."), GUILayout.Width(24f)))
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
            if (GUILayout.Button(new GUIContent("Add Sound Effect", "Add a new sound effect path.")))
            {
                actor.SoundEffectPaths.Add("");
                changed = true;
            }
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawBodyPartList(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds)
        {
            bool changed = false;
            if (actor.BodyParts == null) actor.BodyParts = new List<BodyPartData>();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("BodyParts", "Create, select and delete body parts."), EditorStyles.boldLabel);
            foreach (var bodyPart in actor.BodyParts)
            {
                if (bodyPart == null) continue;
                bool isSelected = bodyPart.Id == selectedBodyPartId;
                bool isVisible = !hiddenBodyPartIds.Contains(bodyPart.Id);
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
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Add BodyPart", "Create a new body part.")))
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
            int selectedId = selectedBodyPartId;
            BodyPartData selectedBodyPart = null;
            foreach (var existingBodyPart in actor.BodyParts)
            {
                if (existingBodyPart == null) continue;
                if (existingBodyPart.Id != selectedId) continue;
                selectedBodyPart = existingBodyPart;
                break;
            }
            bool canDelete = selectedBodyPart != null && !string.Equals(selectedBodyPart.Name, "main", System.StringComparison.OrdinalIgnoreCase);
            using (new EditorGUI.DisabledScope(!canDelete))
            {
                if (GUILayout.Button(new GUIContent("Delete Selected", "Delete selected body part except main.")))
                {
                    if (!EditorUtility.DisplayDialog("Delete BodyPart", "Delete selected BodyPart?", "Delete", "Cancel"))
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        return changed;
                    }
                    actor.BodyParts.Remove(selectedBodyPart);
                    hiddenBodyPartIds.Remove(selectedBodyPart.Id);
                    selectedBodyPartId = actor.BodyParts.Count > 0 ? actor.BodyParts[0].Id : 0;
                    changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawSelectedBodyPart(Actor actor, ref int selectedBodyPartId, HashSet<int> hiddenBodyPartIds)
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
            changed |= DrawNestedResources(bodyPart);
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawPivotPointList(List<PivotPoint> pivotPoints, string title, string addButtonText, string tooltip)
        {
            bool changed = false;
            EditorGUILayout.LabelField(new GUIContent(title, tooltip), EditorStyles.miniBoldLabel);
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
                if (GUILayout.Button(new GUIContent("X", "Delete this pivot point."), GUILayout.Width(24f)))
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
            if (GUILayout.Button(new GUIContent(addButtonText, "Create a pivot point.")))
            {
                int nextId = pivotPoints.Count == 0 ? 1 : pivotPoints.Max(x => x.Id) + 1;
                pivotPoints.Add(new PivotPoint { Id = nextId, Name = "pivot_" + nextId, X = 0f, Y = 0f });
                changed = true;
            }
            return changed;
        }
        private bool DrawNestedResources(BodyPartData bodyPart)
        {
            bool changed = false;
            EditorGUILayout.LabelField(new GUIContent("Nested Resources", "Resources available for this body part."), EditorStyles.miniBoldLabel);
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
                EditorGUILayout.LabelField(new GUIContent("Resource " + i, "Body part resource definition."), EditorStyles.miniBoldLabel);
                if (GUILayout.Button(new GUIContent("X", "Delete this resource."), GUILayout.Width(24f)))
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
                nestedResource.Path = EditorGUILayout.TextField(new GUIContent("Path", "Texture asset path for this resource."), nestedResource.Path);
                if (EditorGUI.EndChangeCheck()) changed = true;
                changed |= DrawPivotPointList(nestedResource.PivotPoints, "Override PivotPoints", "Add Override Pivot", "Pivots that override the base body part pivots by id.");
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button(new GUIContent("Add Resource", "Add a new nested resource for this body part.")))
            {
                bodyPart.NestedResources.Add(new BodyPartResource
                {
                    Id = "resource_" + (bodyPart.NestedResources.Count + 1),
                    Path = "",
                    PivotPoints = new List<PivotPoint>()
                });
                changed = true;
            }
            return changed;
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
