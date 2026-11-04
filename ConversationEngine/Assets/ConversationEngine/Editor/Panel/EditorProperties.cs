using ConversationScheme;
using System.Collections.Generic;
using ConversationEditor.Helper;
using ConversationScheme;
using UnityEditor;
using UnityEngine;
namespace ConversationEditor.Panel
{
    public class EditorProperties
    {
        #region Core Data
        private bool useGlobalCore = true;
        private ConversationEditorCore conversationEditorCore = ConversationEditorCore.GetSingleton();
        private ConversationData internalConversationData;
        private ConversationData conversationData
        {
            set { if (useGlobalCore) conversationEditorCore.conversationData = value; else internalConversationData = value; }
            get { return useGlobalCore ? conversationEditorCore.conversationData : internalConversationData; }
        }
        public bool isReadOnly { get; private set; }
        private readonly EditorWindow ownerWindow;
        #endregion

        #region Events
        public System.Action OnDirty;
        public System.Action OnResourceManagerVisibility;
        #endregion

        #region UI State
        private Vector2 resourceScrollPos;
        #endregion

        #region Resource Manager
        public EditorProperties(EditorWindow ownerWindow, bool isReadOnly = false, bool useGlobalCore = true)
        {
            this.ownerWindow = ownerWindow;
            this.useGlobalCore = useGlobalCore;
            this.isReadOnly = isReadOnly;
        }
        public void SetReadOnlyMode(bool readOnly)
        {
            isReadOnly = readOnly;
        }
        public void DrawResourceManager()
        {
            if (GUILayout.Button("Hide", GUILayout.Width(80f)))
            {
                OnResourceManagerVisibility.Invoke();
            }
            if (conversationData?.ResourceManager == null) return;
            resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos);
            EditorGUI.BeginDisabledGroup(isReadOnly);
            DrawConversationMetadataEditor();
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Scene Backgrounds", EditorStyles.boldLabel);
            DrawResourceList(conversationData.ResourceManager.SceneBackgrounds, "Background");
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Audio Backgrounds", EditorStyles.boldLabel);
            DrawAudioBackgroundList(conversationData.ResourceManager.AudioBackgrounds);
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Actors", EditorStyles.boldLabel);
            DrawActorList(conversationData.ResourceManager.Actors);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndScrollView();
        }

        private void DrawConversationMetadataEditor()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Conversation", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newTitulo = EditorGUILayout.TextField(new GUIContent("Title", ""), conversationData.Title ?? "", GUILayout.ExpandWidth(true));
            Language newDefaultLanguage = (Language)EditorGUILayout.EnumPopup(new GUIContent("Default Language", "Language used when no alternate text is available."), ConversationNodeUtility.ParseLanguage(conversationData.language));
            EditorGUILayout.LabelField(new GUIContent("Description", ""));
            var descriptionFieldStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            string newDescripcion = EditorGUILayout.TextArea(conversationData.Description ?? "", descriptionFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(70f));
            if (EditorGUI.EndChangeCheck())
            {
                RegisterUndoState("Edit Conversation Metadata");
                conversationData.Title = newTitulo;
                conversationData.language = newDefaultLanguage.ToString();
                conversationData.Description = newDescripcion;
                MarkDirty();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawResourceList<T>(List<T> resources, string typeName) where T : Resource, new()
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < resources.Count; i++)
            {
                T resource = resources[i];
                if (resource == null)
                {
                    resource = new T();
                    resources[i] = resource;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                resource.Id = EditorGUILayout.TextField("ID", resource.Id);
                string resourcePathTooltip = "Resource path. GUID: " + (string.IsNullOrWhiteSpace(resource.GUID) ? "(empty)" : resource.GUID);
                resource.Path = EditorGUILayout.TextField(new GUIContent("Path", resourcePathTooltip), resource.Path);
                if (EditorGUI.EndChangeCheck())
                {
                    resource.GUID = ConversationEditorHelpers.GetResourceGuidFromPath(conversationEditorCore.currentFilePath, resource.Path);
                    MarkDirty();
                }
                if (ConversationEditorHelpers.IsMissingResource(resource, conversationEditorCore.currentFilePath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: resource was not found by GUID or Path.", "The resource cannot be resolved using GUID or Path."), missingStyle);
                }
                EditorGUILayout.EndVertical();
                if (!isReadOnly && GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RegisterUndoState("Remove Resource");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (!isReadOnly && GUILayout.Button($"Add {typeName}"))
            {
                RegisterUndoState($"Add {typeName}");
                resources.Add(new T());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }
        private void DrawAudioBackgroundList(List<AudioBackground> resources)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < resources.Count; i++)
            {
                AudioBackground resource = resources[i];
                if (resource == null)
                {
                    resource = new AudioBackground();
                    resources[i] = resource;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                resource.Id = EditorGUILayout.TextField("ID", resource.Id);
                string resourcePathTooltip = "Audio path. GUID: " + (string.IsNullOrWhiteSpace(resource.GUID) ? "(empty)" : resource.GUID);
                resource.Path = EditorGUILayout.TextField(new GUIContent("Path", resourcePathTooltip), resource.Path);
                resource.AudioType = EditorGUILayout.EnumPopup("Audio Type", ConversationNodeUtility.ParseAudioChannelType(resource.AudioType)).ToString();
                if (EditorGUI.EndChangeCheck())
                {
                    resource.GUID = ConversationEditorHelpers.GetResourceGuidFromPath(conversationEditorCore.currentFilePath, resource.Path);
                    MarkDirty();
                }
                if (ConversationEditorHelpers.IsMissingResource(resource, conversationEditorCore.currentFilePath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: audio was not found by GUID or Path.", "The audio cannot be resolved using GUID or Path."), missingStyle);
                }
                EditorGUILayout.EndVertical();
                if (!isReadOnly && GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RegisterUndoState("Remove Audio");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (!isReadOnly && GUILayout.Button("Add Audio Background"))
            {
                RegisterUndoState("Add Audio Background");
                resources.Add(new AudioBackground());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }
        private void DrawActorList(List<Actor> actors)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < actors.Count; i++)
            {
                Actor actor = actors[i];
                if (actor == null)
                {
                    actor = new Actor();
                    actors[i] = actor;
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                actor.Id = EditorGUILayout.TextField("ID", actor.Id);
                string actorPathTooltip = "Actor file path. GUID: " + (string.IsNullOrWhiteSpace(actor.GUID) ? "(empty)" : actor.GUID);
                actor.Path = EditorGUILayout.TextField(new GUIContent("Actor JSON Path", actorPathTooltip), actor.Path);
                string actorIconTooltip = "Actor icon path. GUID: " + (string.IsNullOrWhiteSpace(actor.IconPathGUID) ? "(empty)" : actor.IconPathGUID);
                actor.IconPath = EditorGUILayout.TextField(new GUIContent("Icon Path", actorIconTooltip), actor.IconPath);
                if (EditorGUI.EndChangeCheck())
                {
                    actor.GUID = ConversationEditorHelpers.GetResourceGuidFromPath(conversationEditorCore.currentFilePath, actor.Path);
                    actor.IconPathGUID = ConversationEditorHelpers.GetResourceGuidFromPath(conversationEditorCore.currentFilePath, actor.IconPath);
                    MarkDirty();
                }
                if (ConversationEditorHelpers.IsMissingResource(actor, conversationEditorCore.currentFilePath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: actor was not found by GUID or Path.", "The actor file cannot be resolved using GUID or Path."), missingStyle);
                }
                if (ConversationEditorHelpers.IsMissingResource(conversationEditorCore.currentFilePath, actor.IconPathGUID, actor.IconPath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: icon was not found by GUID or Path.", "The icon cannot be resolved using GUID or Path."), missingStyle);
                }
                EditorGUILayout.EndVertical();
                if (!isReadOnly && GUILayout.Button("X", GUILayout.Width(25)))
                {
                    RegisterUndoState("Remove Actor");
                    actors.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (!isReadOnly && GUILayout.Button("Add Actor"))
            {
                RegisterUndoState("Add Actor");
                actors.Add(new Actor());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }

        private void RegisterUndoState(string actionName)
        {
            if (ownerWindow is ConversationEditorWindow editorWindow) editorWindow.RegisterUndoState(actionName);
        }

        private void MarkDirty()
        {
            OnDirty?.Invoke();
        }
        #endregion
    }
}