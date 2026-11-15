using ConversationEngine.ConversationScheme;
using System.Collections.Generic;
using System.IO;
using ConversationEngine.ConversationEditor.Helper;
using UnityEditor;
using UnityEngine;
namespace ConversationEngine.ConversationEditor.Panel
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
        private const float propertiesMaxComponentWidth = 350f;
        private const float propertiesMinComponentWidth = 350f;
        private float propertiesCurrentContentWidth = propertiesMaxComponentWidth;
        private const float compactPathLabelWidth = 48f;
        private const float actorPathLabelWidth = 84f;
        #endregion

        #region Lifecycle
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
        #endregion

        #region Resource Manager UI
        public void DrawResourceManager()
        {
            if (GUILayout.Button("Hide", GUILayout.Width(80f)))
            {
                OnResourceManagerVisibility.Invoke();
            }
            if (conversationData?.ResourceManager == null) return;
            resourceScrollPos = EditorGUILayout.BeginScrollView(resourceScrollPos);
            propertiesCurrentContentWidth = Mathf.Clamp(EditorGUIUtility.currentViewWidth - 18f, propertiesMinComponentWidth, propertiesMaxComponentWidth);
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GetResponsiveLabelWidth();
            EditorGUILayout.BeginVertical(GUILayout.Width(propertiesCurrentContentWidth), GUILayout.ExpandWidth(false));
            EditorGUI.BeginDisabledGroup(isReadOnly);
            DrawConversationMetadataEditor();
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Scene Backgrounds", "Conversation background assets."), EditorStyles.boldLabel);
            if (!isReadOnly && DrawAddButton("Add a new background resource."))
            {
                RegisterUndoState("Add Background");
                conversationData.ResourceManager.SceneBackgrounds.Add(new SceneBackground());
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            DrawResourceList(conversationData.ResourceManager.SceneBackgrounds, "Background");
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Audio Backgrounds", "Conversation audio resources."), EditorStyles.boldLabel);
            if (!isReadOnly && DrawAddButton("Add a new audio background resource."))
            {
                RegisterUndoState("Add Audio Background");
                conversationData.ResourceManager.AudioBackgrounds.Add(new AudioBackground());
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            DrawAudioBackgroundList(conversationData.ResourceManager.AudioBackgrounds);
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Actors", "Conversation actor assets."), EditorStyles.boldLabel);
            if (!isReadOnly && DrawAddButton("Add a new actor resource."))
            {
                RegisterUndoState("Add Actor");
                conversationData.ResourceManager.Actors.Add(new Actor());
                MarkDirty();
            }
            EditorGUILayout.EndHorizontal();
            DrawActorList(conversationData.ResourceManager.Actors);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
            EditorGUIUtility.labelWidth = oldLabelWidth;
            EditorGUILayout.EndScrollView();
        }
        #endregion

        #region Metadata UI
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
        #endregion

        #region Resource Lists
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
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                resource.Id = EditorGUILayout.TextField("ID", resource.Id);
                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoState($"Edit {typeName} Id");
                    MarkDirty();
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Path", "Resource file path from project Assets root."), GUILayout.Width(compactPathLabelWidth));
                string resourcePathTooltip = "Read-only resource path from Assets root. GUID: " + (string.IsNullOrWhiteSpace(resource.GUID) ? "(empty)" : resource.GUID);
                string resourcePathLabel = string.IsNullOrWhiteSpace(resource.Path) ? "(no file selected)" : resource.Path;
                EditorGUILayout.LabelField(new GUIContent(GetCompactPathText(resourcePathLabel), resourcePathTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button(new GUIContent("...", "Select resource file."), GUILayout.Width(28f)))
                {
                    string changedPath = SelectAssetPathFromProject("Select Resource File", resource.Path, new[] { "Image or Asset", "png,jpg,jpeg,tga,bmp,gif,psd,asset" });
                    if (!string.IsNullOrWhiteSpace(changedPath))
                    {
                        RegisterUndoState($"Set {typeName} Path");
                        resource.Path = changedPath;
                        resource.GUID = AssetDatabase.AssetPathToGUID(changedPath);
                        MarkDirty();
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (ConversationEditorHelpers.IsMissingResource(resource, conversationEditorCore.currentFilePath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: resource was not found by GUID or Path.", "The resource cannot be resolved using GUID or Path."), missingStyle);
                }
                if (DrawDeleteButton($"Remove {typeName.ToLowerInvariant()} resource."))
                {
                    if (!EditorUtility.DisplayDialog($"Remove {typeName}", $"Remove this {typeName.ToLowerInvariant()} resource?", "Remove", "Cancel"))
                    {
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    RegisterUndoState("Remove Resource");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndVertical();
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
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                resource.Id = EditorGUILayout.TextField("ID", resource.Id);
                resource.AudioType = EditorGUILayout.EnumPopup("Audio Type", ConversationNodeUtility.ParseAudioChannelType(resource.AudioType)).ToString();
                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoState("Edit Audio Background");
                    MarkDirty();
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Path", "Audio file path from project Assets root."), GUILayout.Width(compactPathLabelWidth));
                string resourcePathTooltip = "Read-only audio path from Assets root. GUID: " + (string.IsNullOrWhiteSpace(resource.GUID) ? "(empty)" : resource.GUID);
                string resourcePathLabel = string.IsNullOrWhiteSpace(resource.Path) ? "(no file selected)" : resource.Path;
                EditorGUILayout.LabelField(new GUIContent(GetCompactPathText(resourcePathLabel), resourcePathTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button(new GUIContent("...", "Select audio file."), GUILayout.Width(28f)))
                {
                    string changedPath = SelectAssetPathFromProject("Select Audio File", resource.Path, new[] { "Audio", "mp3,wav,ogg,aiff,aif,xm,mod,it,s3m" });
                    if (!string.IsNullOrWhiteSpace(changedPath))
                    {
                        RegisterUndoState("Set Audio Background Path");
                        resource.Path = changedPath;
                        resource.GUID = AssetDatabase.AssetPathToGUID(changedPath);
                        MarkDirty();
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (ConversationEditorHelpers.IsMissingResource(resource, conversationEditorCore.currentFilePath))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent("Missing file: audio was not found by GUID or Path.", "The audio cannot be resolved using GUID or Path."), missingStyle);
                }
                if (DrawDeleteButton("Remove audio background resource."))
                {
                    if (!EditorUtility.DisplayDialog("Remove Audio", "Remove this audio background resource?", "Remove", "Cancel"))
                    {
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    RegisterUndoState("Remove Audio");
                    resources.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndVertical();
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
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginChangeCheck();
                string newActorId = EditorGUILayout.TextField("ID", actor.Id);
                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoState("Edit Actor Resource");
                    actor.Id = newActorId;
                    MarkDirty();
                }
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Actor Path", "Actor file path from project Assets root."), GUILayout.Width(actorPathLabelWidth));
                string actorPathTooltip = "Read-only actor path from Assets root. GUID: " + (string.IsNullOrWhiteSpace(actor.GUID) ? "(empty)" : actor.GUID);
                string actorPathLabel = string.IsNullOrWhiteSpace(actor.Path) ? "(no file selected)" : actor.Path;
                EditorGUILayout.LabelField(new GUIContent(GetCompactPathText(actorPathLabel), actorPathTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button(new GUIContent("...", "Select actor file (.actor or .prefab with ActorBuiltActor root component)."), GUILayout.Width(28f)))
                {
                    string changedPath = SelectAssetPathFromProject("Select Actor File", actor.Path, new[] { "Actor Assets", "actor,prefab" });
                    if (!string.IsNullOrWhiteSpace(changedPath))
                    {
                        string actorPathGuid = AssetDatabase.AssetPathToGUID(changedPath);
                        if (!ConversationEditorHelpers.IsValidActorAssetReference(conversationEditorCore.currentFilePath, actorPathGuid, changedPath, out string actorPathError))
                        {
                            EditorUtility.DisplayDialog("Invalid Actor", actorPathError, "OK");
                        }
                        else
                        {
                            RegisterUndoState("Set Actor Path");
                            actor.Path = changedPath;
                            actor.GUID = actorPathGuid;
                            MarkDirty();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent("Icon Path", "Icon file path from project Assets root."), GUILayout.Width(actorPathLabelWidth));
                string actorIconTooltip = "Read-only icon path from Assets root. GUID: " + (string.IsNullOrWhiteSpace(actor.IconPathGUID) ? "(empty)" : actor.IconPathGUID);
                string actorIconLabel = string.IsNullOrWhiteSpace(actor.IconPath) ? "(no file selected)" : actor.IconPath;
                EditorGUILayout.LabelField(new GUIContent(GetCompactPathText(actorIconLabel), actorIconTooltip), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button(new GUIContent("...", "Select icon file."), GUILayout.Width(28f)))
                {
                    string changedIconPath = SelectAssetPathFromProject("Select Icon File", actor.IconPath, new[] { "Image", "png,jpg,jpeg,tga,bmp,gif,psd" });
                    if (!string.IsNullOrWhiteSpace(changedIconPath))
                    {
                        RegisterUndoState("Set Actor Icon Path");
                        actor.IconPath = changedIconPath;
                        actor.IconPathGUID = AssetDatabase.AssetPathToGUID(changedIconPath);
                        MarkDirty();
                    }
                }
                EditorGUILayout.EndHorizontal();
                if (!ConversationEditorHelpers.IsValidActorAssetReference(conversationEditorCore.currentFilePath, actor.GUID, actor.Path, out string actorValidationMessage))
                {
                    GUIStyle missingStyle = new GUIStyle(EditorStyles.miniLabel);
                    missingStyle.normal.textColor = Color.red;
                    EditorGUILayout.LabelField(new GUIContent(actorValidationMessage, actorValidationMessage), missingStyle);
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
                if (DrawDeleteButton("Remove actor resource."))
                {
                    if (!EditorUtility.DisplayDialog("Remove Actor", "Remove this actor resource?", "Remove", "Cancel"))
                    {
                        EditorGUILayout.EndVertical();
                        continue;
                    }
                    RegisterUndoState("Remove Actor");
                    actors.RemoveAt(i);
                    MarkDirty();
                    i--;
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUI.indentLevel--;
        }
        #endregion

        #region UI Buttons
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
        #endregion

        #region Asset Selection
        private string SelectAssetPathFromProject(string dialogTitle, string currentAssetPath, string[] filters)
        {
            string assetsAbsolutePath = Application.dataPath.Replace("\\", "/");
            string startFolder = assetsAbsolutePath;
            if (!string.IsNullOrWhiteSpace(currentAssetPath))
            {
                string normalizedCurrentPath = currentAssetPath.Replace("\\", "/");
                if (normalizedCurrentPath.StartsWith("Assets/"))
                {
                    string currentAbsolutePath = Path.Combine(Directory.GetParent(assetsAbsolutePath).FullName, normalizedCurrentPath).Replace("\\", "/");
                    if (File.Exists(currentAbsolutePath))
                    {
                        string currentDirectory = Path.GetDirectoryName(currentAbsolutePath);
                        if (!string.IsNullOrWhiteSpace(currentDirectory)) startFolder = currentDirectory.Replace("\\", "/");
                    }
                }
            }
            string absolutePath = EditorUtility.OpenFilePanelWithFilters(dialogTitle, startFolder, filters);
            if (string.IsNullOrWhiteSpace(absolutePath)) return "";
            string normalizedAbsolutePath = Path.GetFullPath(absolutePath).Replace("\\", "/");
            if (!normalizedAbsolutePath.StartsWith(assetsAbsolutePath + "/") && normalizedAbsolutePath != assetsAbsolutePath)
            {
                EditorUtility.DisplayDialog("Invalid File", "Selected file must be inside the Assets folder.", "OK");
                return "";
            }
            return "Assets" + normalizedAbsolutePath.Substring(assetsAbsolutePath.Length);
        }
        #endregion

        #region Layout Helpers
        private float GetResponsiveLabelWidth()
        {
            return Mathf.Clamp(propertiesCurrentContentWidth * 0.26f, 56f, 84f);
        }
        private string GetCompactPathText(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return "";
            const int maxLength = 38;
            if (path.Length <= maxLength) return path;
            string fileName = Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(fileName)) return "..." + path.Substring(path.Length - (maxLength - 3));
            int tailLength = Mathf.Clamp(fileName.Length + 12, 16, maxLength - 3);
            return "..." + path.Substring(path.Length - tailLength);
        }
        #endregion

        #region Editor State
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