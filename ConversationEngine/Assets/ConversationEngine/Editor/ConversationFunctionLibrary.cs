using System.Collections.Generic;

namespace ConversationEditor
{
    /// <summary>
    /// Library of predefined conversation functions with parameter definitions
    /// </summary>
    public static class ConversationFunctionLibrary
    {
        private static Dictionary<string, Dictionary<string, string>> functionDefinitions;

        static ConversationFunctionLibrary()
        {
            InitializeFunctionDefinitions();
        }

        private static void InitializeFunctionDefinitions()
        {
            functionDefinitions = new Dictionary<string, Dictionary<string, string>>
            {
                // Background and Scene Management
                {
                    "SetBackground", new Dictionary<string, string>
                    {
                        { "backgroundId", "Background resource ID" }
                    }
                },
                {
                    "FadeBackground", new Dictionary<string, string>
                    {
                        { "backgroundId", "Background resource ID" },
                        { "duration", "Fade duration in seconds" }
                    }
                },
                {
                    "ClearBackground", new Dictionary<string, string>()
                },

                // Audio Management
                {
                    "PlayAudio", new Dictionary<string, string>
                    {
                        { "audioId", "Audio resource ID" }
                    }
                },
                {
                    "StopAudio", new Dictionary<string, string>
                    {
                        { "audioId", "Audio resource ID" }
                    }
                },
                {
                    "FadeAudio", new Dictionary<string, string>
                    {
                        { "audioId", "Audio resource ID" },
                        { "targetVolume", "Target volume (0-1)" },
                        { "duration", "Fade duration in seconds" }
                    }
                },
                {
                    "PlayBackgroundMusic", new Dictionary<string, string>
                    {
                        { "audioId", "Music resource ID" },
                        { "loop", "Loop music (true/false)" }
                    }
                },
                {
                    "PlaySoundEffect", new Dictionary<string, string>
                    {
                        { "audioId", "Sound effect resource ID" }
                    }
                },

                // Actor Management
                {
                    "ShowActor", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "position", "Position (left/center/right)" }
                    }
                },
                {
                    "HideActor", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" }
                    }
                },
                {
                    "MoveActor", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "position", "Target position (left/center/right)" },
                        { "duration", "Movement duration in seconds" }
                    }
                },
                {
                    "SetActorScale", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "scale", "Scale factor (0-2)" }
                    }
                },

                // Actor Body Part Management
                {
                    "SetBodyPart", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "bodyPartId", "Body part ID (head/body/eyes/mouth)" },
                        { "resourceId", "Resource ID for body part" }
                    }
                },
                {
                    "ShowBodyPart", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "bodyPartId", "Body part ID" }
                    }
                },
                {
                    "HideBodyPart", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "bodyPartId", "Body part ID" }
                    }
                },

                // Actor Expression/Emotion
                {
                    "SetActorExpression", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "expression", "Expression name (happy/sad/angry/neutral)" }
                    }
                },
                {
                    "PlayActorAnimation", new Dictionary<string, string>
                    {
                        { "actorId", "Actor resource ID" },
                        { "animationName", "Animation name" }
                    }
                },

                // Camera Control
                {
                    "CameraShake", new Dictionary<string, string>
                    {
                        { "intensity", "Shake intensity (0-1)" },
                        { "duration", "Shake duration in seconds" }
                    }
                },
                {
                    "CameraZoom", new Dictionary<string, string>
                    {
                        { "zoomLevel", "Zoom level (0.5-2)" },
                        { "duration", "Zoom duration in seconds" }
                    }
                },
                {
                    "CameraFocus", new Dictionary<string, string>
                    {
                        { "target", "Focus target (actor ID or position)" }
                    }
                },

                // Variable Management
                {
                    "SetVariable", new Dictionary<string, string>
                    {
                        { "variableName", "Variable name" },
                        { "value", "Variable value" }
                    }
                },
                {
                    "IncrementVariable", new Dictionary<string, string>
                    {
                        { "variableName", "Variable name" },
                        { "amount", "Amount to increment" }
                    }
                },
                {
                    "DecrementVariable", new Dictionary<string, string>
                    {
                        { "variableName", "Variable name" },
                        { "amount", "Amount to decrement" }
                    }
                },

                // UI and Effects
                {
                    "ShowTextBox", new Dictionary<string, string>
                    {
                        { "style", "Text box style (default/narration/thought)" }
                    }
                },
                {
                    "HideTextBox", new Dictionary<string, string>()
                },
                {
                    "ShowChoiceBox", new Dictionary<string, string>
                    {
                        { "layout", "Choice layout (vertical/horizontal)" }
                    }
                },
                {
                    "PlayScreenEffect", new Dictionary<string, string>
                    {
                        { "effectName", "Effect name (flash/fade/blur)" },
                        { "duration", "Effect duration in seconds" }
                    }
                },
                {
                    "FadeToBlack", new Dictionary<string, string>
                    {
                        { "duration", "Fade duration in seconds" }
                    }
                },
                {
                    "FadeFromBlack", new Dictionary<string, string>
                    {
                        { "duration", "Fade duration in seconds" }
                    }
                },

                // Game Flow
                {
                    "Wait", new Dictionary<string, string>
                    {
                        { "duration", "Wait duration in seconds" }
                    }
                },
                {
                    "LoadScene", new Dictionary<string, string>
                    {
                        { "sceneName", "Scene name to load" }
                    }
                },
                {
                    "SaveCheckpoint", new Dictionary<string, string>
                    {
                        { "checkpointName", "Checkpoint identifier" }
                    }
                },
                {
                    "TriggerEvent", new Dictionary<string, string>
                    {
                        { "eventName", "Event identifier" }
                    }
                },
                {
                    "EndConversation", new Dictionary<string, string>()
                },

                // Particle and Visual Effects
                {
                    "PlayParticleEffect", new Dictionary<string, string>
                    {
                        { "effectId", "Particle effect ID" },
                        { "position", "Effect position" }
                    }
                },
                {
                    "StopParticleEffect", new Dictionary<string, string>
                    {
                        { "effectId", "Particle effect ID" }
                    }
                },

                // Text Display
                {
                    "SetTextSpeed", new Dictionary<string, string>
                    {
                        { "speed", "Text display speed (characters per second)" }
                    }
                },
                {
                    "SetTextColor", new Dictionary<string, string>
                    {
                        { "color", "Text color (hex or name)" }
                    }
                },
                {
                    "ShowNameTag", new Dictionary<string, string>
                    {
                        { "name", "Character name to display" }
                    }
                },
                {
                    "HideNameTag", new Dictionary<string, string>()
                },

                // Quest and Achievement
                {
                    "StartQuest", new Dictionary<string, string>
                    {
                        { "questId", "Quest identifier" }
                    }
                },
                {
                    "CompleteQuest", new Dictionary<string, string>
                    {
                        { "questId", "Quest identifier" }
                    }
                },
                {
                    "UnlockAchievement", new Dictionary<string, string>
                    {
                        { "achievementId", "Achievement identifier" }
                    }
                },

                // Inventory
                {
                    "AddItem", new Dictionary<string, string>
                    {
                        { "itemId", "Item identifier" },
                        { "quantity", "Item quantity" }
                    }
                },
                {
                    "RemoveItem", new Dictionary<string, string>
                    {
                        { "itemId", "Item identifier" },
                        { "quantity", "Item quantity" }
                    }
                }
            };
        }

        /// <summary>
        /// Gets all available function names including "Custom" option
        /// </summary>
        public static string[] GetFunctionNames()
        {
            var names = new List<string>(functionDefinitions.Keys);
            names.Sort();
            names.Add("Custom");
            return names.ToArray();
        }

        /// <summary>
        /// Gets parameter definitions for a specific function
        /// </summary>
        /// <param name="functionName">Name of the function</param>
        /// <returns>Dictionary of parameter names and descriptions, or null if not found</returns>
        public static Dictionary<string, string> GetFunctionParameters(string functionName)
        {
            if (functionDefinitions.TryGetValue(functionName, out var parameters))
            {
                return parameters;
            }
            return null;
        }

        /// <summary>
        /// Checks if a function name is predefined
        /// </summary>
        public static bool IsPredefinedFunction(string functionName)
        {
            return functionDefinitions.ContainsKey(functionName);
        }

        /// <summary>
        /// Gets function categories for organizing in UI
        /// </summary>
        public static Dictionary<string, List<string>> GetFunctionsByCategory()
        {
            return new Dictionary<string, List<string>>
            {
                {
                    "Background & Scene", new List<string>
                    {
                        "SetBackground", "FadeBackground", "ClearBackground"
                    }
                },
                {
                    "Audio", new List<string>
                    {
                        "PlayAudio", "StopAudio", "FadeAudio", "PlayBackgroundMusic", "PlaySoundEffect"
                    }
                },
                {
                    "Actor", new List<string>
                    {
                        "ShowActor", "HideActor", "MoveActor", "SetActorScale",
                        "SetActorExpression", "PlayActorAnimation"
                    }
                },
                {
                    "Body Parts", new List<string>
                    {
                        "SetBodyPart", "ShowBodyPart", "HideBodyPart"
                    }
                },
                {
                    "Camera", new List<string>
                    {
                        "CameraShake", "CameraZoom", "CameraFocus"
                    }
                },
                {
                    "Variables", new List<string>
                    {
                        "SetVariable", "IncrementVariable", "DecrementVariable"
                    }
                },
                {
                    "UI & Effects", new List<string>
                    {
                        "ShowTextBox", "HideTextBox", "ShowChoiceBox", "PlayScreenEffect",
                        "FadeToBlack", "FadeFromBlack", "SetTextSpeed", "SetTextColor",
                        "ShowNameTag", "HideNameTag"
                    }
                },
                {
                    "Game Flow", new List<string>
                    {
                        "Wait", "LoadScene", "SaveCheckpoint", "TriggerEvent", "EndConversation"
                    }
                },
                {
                    "Effects", new List<string>
                    {
                        "PlayParticleEffect", "StopParticleEffect"
                    }
                },
                {
                    "Progression", new List<string>
                    {
                        "StartQuest", "CompleteQuest", "UnlockAchievement", "AddItem", "RemoveItem"
                    }
                }
            };
        }
    }
}
