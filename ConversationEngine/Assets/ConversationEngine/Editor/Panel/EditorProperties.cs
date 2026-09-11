using ConversationScheme;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace ConversationEditor
{
    public class EditorProperties
    {
        #region Core Data
        private ConversationEditorCore conversationEditorCore = ConversationEditorCore.GetSingleton();
        private ConversationData conversationData { set { conversationEditorCore.conversationData = value; } get { return conversationEditorCore.conversationData; } }
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
        public EditorProperties(EditorWindow ownerWindow)
        {
            this.ownerWindow = ownerWindow;
        }
        public void DrawResourceManager()
        {
            if (GUILayout.Button("Hide", GUILayout.Width(80f)))
            {
                OnResourceManagerVisibility.Invoke();
            }
            if (conversationData?.ResourceManager == null) return;
            resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos);
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
            EditorGUILayout.EndScrollView();
        }

        private void DrawConversationMetadataEditor()
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Conversation", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            string newTitulo = EditorGUILayout.TextField(new GUIContent("Title", ""), conversationData.Title ?? "", GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField(new GUIContent("Description", ""));
            var descriptionFieldStyle = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
            string newDescripcion = EditorGUILayout.TextArea(conversationData.Description ?? "", descriptionFieldStyle, GUILayout.ExpandWidth(true), GUILayout.MinHeight(70f));
            if (EditorGUI.EndChangeCheck())
            {
                conversationData.Title = newTitulo;
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
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                resources[i].Id = EditorGUILayout.TextField("ID", resources[i].Id);
                resources[i].Path = EditorGUILayout.TextField("Path", resources[i].Path);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Resource");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button($"Add {typeName}"))
            {
                Undo.RecordObject(ownerWindow, $"Add {typeName}");
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
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                resources[i].Id = EditorGUILayout.TextField("ID", resources[i].Id);
                resources[i].Path = EditorGUILayout.TextField("Path", resources[i].Path);
                resources[i].AudioType = (AudioChannelType)EditorGUILayout.EnumPopup("Audio Type", resources[i].AudioType);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Audio");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Audio Background"))
            {
                Undo.RecordObject(ownerWindow, "Add Audio Background");
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
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical("box");
                actors[i].Id = EditorGUILayout.TextField("ID", actors[i].Id);
                actors[i].Path = EditorGUILayout.TextField("Actor JSON Path", actors[i].Path);
                actors[i].IconPath = EditorGUILayout.TextField("Icon Path", actors[i].IconPath);
                EditorGUILayout.EndVertical();
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Actor");
                    actors.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("Add Actor"))
            {
                Undo.RecordObject(ownerWindow, "Add Actor");
                actors.Add(new Actor());
                MarkDirty();
            }
            EditorGUI.indentLevel--;
        }

        private void MarkDirty()
        {
            OnDirty?.Invoke();
        }
        #endregion
    }
}