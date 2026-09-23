using UnityEngine;
using UnityEditor;
using System.Linq;

namespace ConversationEditor
{
    /// <summary>
    /// Setup verification tool for Conversation Editor
    /// Access via: Window > ConversationEngine > Setup Verification
    /// </summary>
    public class ConversationEditorSetup : EditorWindow
    {
        private Vector2 scrollPos;
        private bool newtonsoftInstalled = false;
        private bool assemblyDefsValid = false;
        private bool exampleFilesPresent = false;
        private string statusMessage = "";

        [MenuItem("Window/ConversationEngine/Setup Verification")]
        public static void ShowWindow()
        {
            var window = GetWindow<ConversationEditorSetup>("CE Setup");
            window.minSize = new Vector2(400, 300);
            window.CheckSetup();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Conversation Editor - Setup Verification", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            // Newtonsoft.Json Check
            DrawStatusBox("Newtonsoft.Json Package", newtonsoftInstalled,
                "Required for JSON serialization",
                "Install via Window > Package Manager > Add package by name: com.unity.nuget.newtonsoft-json");

            EditorGUILayout.Space();

            // Assembly Definitions Check
            DrawStatusBox("Assembly Definitions", assemblyDefsValid,
                "Ensures proper script compilation",
                "Assembly definition files should reference Unity.Newtonsoft.Json");

            EditorGUILayout.Space();

            // Example Files Check
            DrawStatusBox("Example Files", exampleFilesPresent,
                "Example conversation files for reference",
                "Examples should be in Assets/ConversationEngine/Examples/");

            EditorGUILayout.Space();

            // Overall Status
            EditorGUILayout.LabelField("Overall Status:", EditorStyles.boldLabel);
            if (newtonsoftInstalled && assemblyDefsValid)
            {
                EditorGUILayout.HelpBox("? Setup Complete! You can start using the Conversation Editor.", MessageType.Info);

                EditorGUILayout.Space();
                if (GUILayout.Button("Open Example Conversation", GUILayout.Height(30)))
                {
                    OpenExampleConversation();
                }

                if (GUILayout.Button("Create New Conversation", GUILayout.Height(30)))
                {
                    ConversationMenuItems.CreateConversationFile();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("? Setup Incomplete. Please follow the instructions above.", MessageType.Warning);

                if (!newtonsoftInstalled)
                {
                    EditorGUILayout.Space();
                    if (GUILayout.Button("Open Package Manager", GUILayout.Height(30)))
                    {
                        UnityEditor.PackageManager.UI.Window.Open("com.unity.nuget.newtonsoft-json");
                    }
                }
            }

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Refresh Status"))
            {
                CheckSetup();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Documentation:", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Installation Guide"))
            {
                OpenDocumentation("INSTALLATION.md");
            }
            if (GUILayout.Button("Open Editor Documentation"))
            {
                OpenDocumentation("README_EDITOR.md");
            }
            if (GUILayout.Button("Open Setup Summary"))
            {
                OpenDocumentation("SETUP_SUMMARY.md");
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawStatusBox(string title, bool status, string description, string helpText)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            GUIStyle statusStyle = new GUIStyle(EditorStyles.label);
            if (status)
            {
                statusStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("? OK", statusStyle);
            }
            else
            {
                statusStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("? Missing", statusStyle);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);

            if (!status)
            {
                EditorGUILayout.HelpBox(helpText, MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        private void CheckSetup()
        {
            statusMessage = "Checking setup...";
            Repaint();

            // Check Newtonsoft.Json
            newtonsoftInstalled = CheckNewtonsoftJson();

            // Check Assembly Definitions
            assemblyDefsValid = CheckAssemblyDefinitions();

            // Check Example Files
            exampleFilesPresent = CheckExampleFiles();

            statusMessage = $"Last checked: {System.DateTime.Now:HH:mm:ss}";
            Repaint();
        }

        private bool CheckNewtonsoftJson()
        {
            // Try to find Newtonsoft.Json assembly
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Any(a => a.FullName.Contains("Newtonsoft.Json"));
        }

        private bool CheckAssemblyDefinitions()
        {
            bool editorAsmdef = System.IO.File.Exists("Assets/ConversationEngine/Editor/ConversationEngine.Editor.asmdef");
            bool coreAsmdef = System.IO.File.Exists("Assets/ConversationEngine/ConversationScheme/ConversationEngine.asmdef");
            return editorAsmdef && coreAsmdef;
        }

        private bool CheckExampleFiles()
        {
            return System.IO.File.Exists("Assets/ConversationEngine/Examples/conversation_intro.json");
        }

        private void OpenExampleConversation()
        {
            string path = "Assets/ConversationEngine/Examples/conversation_intro.json";
            if (System.IO.File.Exists(path))
            {
                ConversationEditorWindow.OpenConversationFile(path);
            }
            else
            {
                EditorUtility.DisplayDialog("Example Not Found",
                    "Could not find the example conversation file at:\n" + path,
                    "OK");
            }
        }

        private void OpenDocumentation(string filename)
        {
            string path = $"Assets/ConversationEngine/{filename}";
            if (System.IO.File.Exists(path))
            {
                Application.OpenURL("file://" + System.IO.Path.GetFullPath(path));
            }
            else
            {
                EditorUtility.DisplayDialog("File Not Found",
                    $"Could not find documentation file:\n{path}",
                    "OK");
            }
        }
    }
}
