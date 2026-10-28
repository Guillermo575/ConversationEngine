using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
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
        public bool Draw(Actor actor, ref int selectedBodyPartId)
        {
            if (actor == null) return false;
            bool changed = false;
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            changed |= DrawActorFields(actor);
            EditorGUILayout.Space(6f);
            changed |= DrawBodyPartList(actor, ref selectedBodyPartId);
            EditorGUILayout.Space(6f);
            changed |= DrawSelectedBodyPart(actor, selectedBodyPartId);
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
        private bool DrawBodyPartList(Actor actor, ref int selectedBodyPartId)
        {
            bool changed = false;
            if (actor.BodyParts == null) actor.BodyParts = new List<BodyPartData>();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(new GUIContent("BodyParts", "Create, select and delete body parts."), EditorStyles.boldLabel);
            foreach (var bodyPart in actor.BodyParts)
            {
                if (bodyPart == null) continue;
                bool isSelected = bodyPart.Id == selectedBodyPartId;
                GUIStyle buttonStyle = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                string title = string.IsNullOrEmpty(bodyPart.Name) ? "bodypart" : bodyPart.Name;
                if (GUILayout.Button(new GUIContent(title + " [" + bodyPart.Id + "]", "Select this body part."), buttonStyle)) selectedBodyPartId = bodyPart.Id;
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
                    actor.BodyParts.Remove(selectedBodyPart);
                    selectedBodyPartId = actor.BodyParts.Count > 0 ? actor.BodyParts[0].Id : 0;
                    changed = true;
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return changed;
        }
        private bool DrawSelectedBodyPart(Actor actor, int selectedBodyPartId)
        {
            BodyPartData bodyPart = actor.BodyParts.FirstOrDefault(x => x != null && x.Id == selectedBodyPartId);
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
            EditorGUI.BeginChangeCheck();
            bodyPart.Id = EditorGUILayout.IntField(new GUIContent("Id", "Unique body part id inside actor."), bodyPart.Id);
            bodyPart.Name = EditorGUILayout.TextField(new GUIContent("Name", "Display name for this body part."), bodyPart.Name);
            bodyPart.AttachToPivotId = EditorGUILayout.IntField(new GUIContent("Attach To Pivot Id", "Pivot id used as attach point. 0 means actor main pivot."), bodyPart.AttachToPivotId);
            bodyPart.CurrentResourceId = EditorGUILayout.TextField(new GUIContent("Current Resource Id", "Active resource id for preview and runtime."), bodyPart.CurrentResourceId);
            if (EditorGUI.EndChangeCheck()) changed = true;
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
                if (GUILayout.Button(new GUIContent("X", "Delete this pivot point."), GUILayout.Width(24f)))
                {
                    pivotPoints.RemoveAt(i);
                    changed = true;
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.BeginChangeCheck();
                pivotPoint.Id = EditorGUILayout.IntField(new GUIContent("Id", "Pivot id. Resource pivots can override by same id."), pivotPoint.Id);
                pivotPoint.Name = EditorGUILayout.TextField(new GUIContent("Name", "Pivot descriptive name."), pivotPoint.Name);
                pivotPoint.X = EditorGUILayout.FloatField(new GUIContent("X", "Horizontal local pivot position."), pivotPoint.X);
                pivotPoint.Y = EditorGUILayout.FloatField(new GUIContent("Y", "Vertical local pivot position."), pivotPoint.Y);
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
        #endregion
    }
}
