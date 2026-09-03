using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Reflection;
using UnityEngine;
using ConversationScheme;

namespace ConversationEditor
{
    /// <summary>
    /// Simple JSON serializer/deserializer for ConversationData
    /// This is a workaround to avoid Newtonsoft.Json dependency
    /// </summary>
    public static class SimpleJsonSerializer
    {
        public static string Serialize(ConversationData data)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");

            // ResourceManager
            sb.AppendLine("  \"ResourceManager\": {");
            SerializeResourceManager(sb, data.ResourceManager);
            sb.AppendLine("  },");

            // ConversationManager
            sb.AppendLine("  \"ConversationManager\": {");
            SerializeConversationManager(sb, data.ConversationManager);
            sb.AppendLine("  },");

            // EditorSettings
            sb.AppendLine("  \"EditorSettings\": {");
            float zoom = data.EditorSettings != null ? data.EditorSettings.Zoom : 1f;
            sb.AppendLine($"    \"Zoom\": {zoom}");
            sb.AppendLine("  }");

            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void SerializeResourceManager(StringBuilder sb, ResourceManager rm)
        {
            // SceneBackgrounds
            sb.AppendLine("    \"SceneBackgrounds\": [");
            for (int i = 0; i < rm.SceneBackgrounds.Count; i++)
            {
                var bg = rm.SceneBackgrounds[i];
                sb.AppendLine("      {");
                sb.AppendLine($"        \"Id\": \"{EscapeString(bg.Id)}\",");
                sb.AppendLine($"        \"Path\": \"{EscapeString(bg.Path)}\"");
                sb.Append("      }");
                if (i < rm.SceneBackgrounds.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("    ],");

            // AudioBackgrounds
            sb.AppendLine("    \"AudioBackgrounds\": [");
            for (int i = 0; i < rm.AudioBackgrounds.Count; i++)
            {
                var audio = rm.AudioBackgrounds[i];
                sb.AppendLine("      {");
                sb.AppendLine($"        \"Id\": \"{EscapeString(audio.Id)}\",");
                sb.AppendLine($"        \"Path\": \"{EscapeString(audio.Path)}\",");
                sb.AppendLine($"        \"AudioType\": \"{audio.AudioType}\"");
                sb.Append("      }");
                if (i < rm.AudioBackgrounds.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("    ],");

            // Actors
            sb.AppendLine("    \"Actors\": [");
            for (int i = 0; i < rm.Actors.Count; i++)
            {
                var actor = rm.Actors[i];
                sb.AppendLine("      {");
                sb.AppendLine($"        \"Id\": \"{EscapeString(actor.Id)}\",");
                sb.AppendLine($"        \"Path\": \"{EscapeString(actor.Path)}\",");
                sb.AppendLine($"        \"IconPath\": \"{EscapeString(actor.IconPath)}\",");
                sb.AppendLine("        \"SoundEffectPaths\": [],");
                sb.AppendLine("        \"BodyParts\": []");
                sb.Append("      }");
                if (i < rm.Actors.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("    ]");
        }

        private static void SerializeConversationManager(StringBuilder sb, ConversationManager cm)
        {
            sb.AppendLine("    \"Nodes\": [");
            for (int i = 0; i < cm.Nodes.Count; i++)
            {
                SerializeNode(sb, cm.Nodes[i], i < cm.Nodes.Count - 1);
            }
            sb.AppendLine("    ]");
        }

        private static void SerializeNode(StringBuilder sb, ConversationNode node, bool addComma)
        {
            sb.AppendLine("      {");
            sb.AppendLine($"        \"Id\": {node.Id},");
            sb.AppendLine($"        \"NodeType\": \"{node.NodeType}\",");
            sb.AppendLine($"        \"SpeakerActorId\": \"{EscapeString(node.SpeakerActorId)}\",");
            sb.AppendLine($"        \"Text\": \"{EscapeString(node.Text)}\",");
            sb.AppendLine($"        \"NextNodeId\": {node.NextNodeId},");

            // Options
            sb.AppendLine("        \"Options\": [");
            if (node.Options != null)
            {
                for (int i = 0; i < node.Options.Count; i++)
                {
                    var opt = node.Options[i];
                    sb.AppendLine("          {");
                    sb.AppendLine($"            \"Text\": \"{EscapeString(opt.Text)}\",");
                    sb.AppendLine($"            \"NextNodeId\": {opt.NextNodeId},");
                    sb.AppendLine("            \"EditorPosition\": {");
                    sb.AppendLine($"              \"X\": {opt.EditorPosition.x},");
                    sb.AppendLine($"              \"Y\": {opt.EditorPosition.y}");
                    sb.AppendLine("            },");
                    sb.AppendLine("            \"EditorSize\": {");
                    sb.AppendLine($"              \"X\": {opt.EditorSize.x},");
                    sb.AppendLine($"              \"Y\": {opt.EditorSize.y}");
                    sb.AppendLine("            },");
                    sb.AppendLine("            \"Conditions\": []");
                    sb.Append("          }");
                    if (i < node.Options.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("        ],");

            // Functions
            sb.AppendLine("        \"Functions\": [");
            if (node.Functions != null)
            {
                for (int i = 0; i < node.Functions.Count; i++)
                {
                    var func = node.Functions[i];
                    sb.AppendLine("          {");
                    sb.AppendLine($"            \"MethodName\": \"{EscapeString(func.MethodName)}\",");
                    sb.AppendLine("            \"Parameters\": {");
                    if (func.Parameters != null && func.Parameters.Count > 0)
                    {
                        var keys = func.Parameters.Keys.ToList();
                        for (int j = 0; j < keys.Count; j++)
                        {
                            sb.Append($"              \"{EscapeString(keys[j])}\": \"{EscapeString(func.Parameters[keys[j]])}\"");
                            if (j < keys.Count - 1) sb.Append(",");
                            sb.AppendLine();
                        }
                    }
                    sb.AppendLine("            },");
                    sb.AppendLine($"            \"Timestamp\": {func.Timestamp}");
                    sb.Append("          }");
                    if (i < node.Functions.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
            }
            sb.AppendLine("        ],");

            // ConditionalBranches
            // conditionalBranch (singular) - only for conditional nodes
            if (node.NodeType == ConversationNodeType.Conditional && node.conditionalBranch != null)
            {
                sb.AppendLine("        \"conditionalBranch\": {");
                // Conditions (we currently serialize as empty list placeholder)
                sb.AppendLine("          \"Conditions\": [],");
                sb.AppendLine($"          \"NextNodeIdTrue\": {node.conditionalBranch.NextNodeIdTrue},");
                sb.AppendLine($"          \"NextNodeIdFalse\": {node.conditionalBranch.NextNodeIdFalse}");
                sb.AppendLine("        },");
            }
            else
            {
                sb.AppendLine("        \"conditionalBranch\": {},");
            }
            sb.AppendLine($"        \"DefaultBranchNodeId\": {node.DefaultBranchNodeId},");
            sb.AppendLine("        \"EditorPosition\": {");
            sb.AppendLine($"          \"X\": {node.EditorPosition.x},");
            sb.AppendLine($"          \"Y\": {node.EditorPosition.y}");
            sb.AppendLine("        },");
            sb.AppendLine("        \"EditorSize\": {");
            sb.AppendLine($"          \"X\": {node.EditorSize.x},");
            sb.AppendLine($"          \"Y\": {node.EditorSize.y}");
            sb.AppendLine("        }");
            sb.Append("      }");
            if (addComma) sb.Append(",");
            sb.AppendLine();
        }

        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("\\", "\\\\")
                     .Replace("\"", "\\\"")
                     .Replace("\n", "\\n")
                     .Replace("\r", "\\r")
                     .Replace("\t", "\\t");
        }

        public static ConversationData Deserialize(string json)
        {
            // First, normalize the JSON to handle both "X"/"Y" (old format) and "x"/"y" (Unity format)
            // and convert enum strings to numbers for Unity's JsonUtility
            json = NormalizeJsonForUnity(json);

            // Use Unity's JsonUtility
            try
            {
                var data = JsonUtility.FromJson<ConversationData>(json);
                if (data != null && data.EditorSettings == null)
                {
                    data.EditorSettings = new ConversationEditorSettings();
                }
                return data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize conversation data: {ex.Message}");
                return null;
            }
        }

        private static string NormalizeJsonForUnity(string json)
        {
            // Replace "X": with "x": and "Y": with "y": for EditorPosition and EditorSize
            json = json.Replace("\"X\":", "\"x\":");
            json = json.Replace("\"Y\":", "\"y\":");
            // Convert old ConditionalBranches array to new singular conditionalBranch object
            try
            {
                string patternWithObject = "\"ConditionalBranches\"\\s*:\\s*\\[(?<inner>\\{.*?\\})\\s*\\]";
                json = System.Text.RegularExpressions.Regex.Replace(json, patternWithObject, "\"conditionalBranch\": ${inner}", System.Text.RegularExpressions.RegexOptions.Singleline);
                // empty array case
                json = System.Text.RegularExpressions.Regex.Replace(json, "\"ConditionalBranches\"\\s*:\\s*\\[\\s*\\]", "\"conditionalBranch\": {}");
            }
            catch { }
            json = ConvertEnumsToNumbers(json);
            return json;
        }

        private static string ConvertEnumsToNumbers(string json)
        {
            var enumMappings = new Dictionary<Type, string>
            {
                { typeof(ConversationNodeType), "NodeType" },
                { typeof(AudioChannelType), "AudioType" },
                { typeof(ComparisonOperator), "Operator" },
                { typeof(ConversationScheme.ValueType), "ValueDataType" }
            };

            foreach (var mapping in enumMappings)
            {
                var enumType = mapping.Key;
                var jsonPropertyName = mapping.Value;
                var enumNames = Enum.GetNames(enumType);
                var enumValues = Enum.GetValues(enumType);

                for (int i = 0; i < enumNames.Length; i++)
                {
                    string enumName = enumNames[i];
                    int enumValue = (int)enumValues.GetValue(i);
                    string pattern = $"\"{jsonPropertyName}\":\\s*\"{enumName}\"";
                    string replacement = $"\"{jsonPropertyName}\": {enumValue}";
                    json = System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);
                }
            }
            return json;
        }
    }
}
