using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
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
            sb.AppendLine("        \"ConditionalBranches\": [],");
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
            // Use Unity's JsonUtility as fallback
            try
            {
                return JsonUtility.FromJson<ConversationData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to deserialize conversation data: {ex.Message}");
                return null;
            }
        }
    }
}
