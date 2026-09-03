using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using ConversationScheme;

namespace ConversationEditor
{
    /// <summary>
    /// Helper methods for the Conversation Editor Window
    /// </summary>
    public static class ConversationEditorHelpers
    {
        /// <summary>
        /// Check if a point is near a bezier curve
        /// </summary>
        public static bool IsPointNearBezierCurve(Vector2 point, Vector2 start, Vector2 end, float threshold = 10f)
        {
            Vector2 startTangent = start + Vector2.right * 50;
            Vector2 endTangent = end + Vector2.left * 50;

            // Sample points along the curve and check distance
            int samples = 20;
            for (int i = 0; i <= samples; i++)
            {
                float t = i / (float)samples;
                Vector2 curvePoint = CalculateBezierPoint(t, start, startTangent, endTangent, end);
                if (Vector2.Distance(point, curvePoint) < threshold)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calculate a point on a cubic bezier curve
        /// </summary>
        public static Vector2 CalculateBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector2 p = uuu * p0;
            p += 3 * uu * t * p1;
            p += 3 * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        /// <summary>
        /// Auto-arrange nodes in a horizontal or vertical layout
        /// </summary>
        public static void AutoArrangeNodes(List<ConversationNode> nodes, float spacing, bool horizontal = true)
        {
            if (nodes == null || nodes.Count == 0) return;

            // Find start node
            var startNode = nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);
            if (startNode == null) return;

            // Build graph
            Dictionary<int, List<ConversationNode>> layers = new Dictionary<int, List<ConversationNode>>();
            HashSet<int> visited = new HashSet<int>();
            Queue<(ConversationNode node, int layer)> queue = new Queue<(ConversationNode, int)>();

            queue.Enqueue((startNode, 0));
            visited.Add(startNode.Id);

            int maxLayer = 0;

            while (queue.Count > 0)
            {
                var (currentNode, layer) = queue.Dequeue();

                if (!layers.ContainsKey(layer))
                    layers[layer] = new List<ConversationNode>();

                layers[layer].Add(currentNode);
                maxLayer = Mathf.Max(maxLayer, layer);

                // Get all connected nodes
                List<int> connectedIds = new List<int>();

                if (currentNode.NextNodeId > 0)
                    connectedIds.Add(currentNode.NextNodeId);

                if (currentNode.Options != null)
                {
                    foreach (var option in currentNode.Options)
                    {
                        if (option.NextNodeId > 0)
                            connectedIds.Add(option.NextNodeId);
                    }
                }

                if (currentNode.conditionalBranch != null)
                {
                    var branch = currentNode.conditionalBranch;
                    if (branch.NextNodeIdTrue > 0) connectedIds.Add(branch.NextNodeIdTrue);
                    if (branch.NextNodeIdFalse > 0) connectedIds.Add(branch.NextNodeIdFalse);
                }

                // Enqueue unvisited connected nodes
                foreach (var id in connectedIds)
                {
                    if (!visited.Contains(id))
                    {
                        var nextNode = nodes.FirstOrDefault(n => n.Id == id);
                        if (nextNode != null)
                        {
                            visited.Add(id);
                            queue.Enqueue((nextNode, layer + 1));
                        }
                    }
                }
            }

            // Position nodes
            float currentOffset = 0;
            foreach (var layer in layers.OrderBy(kvp => kvp.Key))
            {
                int layerIndex = layer.Key;
                var layerNodes = layer.Value;

                float layerHeight = layerNodes.Count * 120; // Approximate node height
                float startY = -layerHeight / 2;

                for (int i = 0; i < layerNodes.Count; i++)
                {
                    var node = layerNodes[i];

                    if (horizontal)
                    {
                        node.EditorPosition = new Vector2(layerIndex * spacing, startY + i * 120);
                    }
                    else
                    {
                        node.EditorPosition = new Vector2(startY + i * 120, layerIndex * spacing);
                    }
                }
            }
        }

        /// <summary>
        /// Get a formatted node description for dropdowns
        /// </summary>
        public static string GetNodeDropdownText(ConversationNode node)
        {
            if (node == null) return "NINGUNO";

            string actorPart = string.IsNullOrEmpty(node.SpeakerActorId) ? "" : $" - {node.SpeakerActorId}";
            string textPart = string.IsNullOrEmpty(node.Text) ? "" : $" - {(node.Text.Length > 30 ? node.Text.Substring(0, 30) + "..." : node.Text)}";
            string nodeTypePart = "";

            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    nodeTypePart = " [START]";
                    break;
                case ConversationNodeType.End:
                    nodeTypePart = " [END]";
                    break;
                case ConversationNodeType.Function:
                    nodeTypePart = " [FUNC]";
                    break;
                case ConversationNodeType.Conditional:
                    nodeTypePart = " [COND]";
                    break;
            }

            return $"{node.Id}{actorPart}{textPart}{nodeTypePart}";
        }

        /// <summary>
        /// Check if a rect contains a point, accounting for zoom and pan
        /// </summary>
        public static bool RectContainsPoint(Rect rect, Vector2 point, Vector2 panOffset, float zoom)
        {
            Rect transformedRect = new Rect(
                rect.x * zoom + panOffset.x,
                rect.y * zoom + panOffset.y,
                rect.width * zoom,
                rect.height * zoom
            );
            return transformedRect.Contains(point);
        }
    }
}
