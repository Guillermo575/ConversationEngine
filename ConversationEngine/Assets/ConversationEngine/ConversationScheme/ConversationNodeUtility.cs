using System.Collections.Generic;
using System.Linq;

namespace ConversationScheme
{
    /// <summary>
    /// Utility class for managing conversation node IDs and references
    /// </summary>
    public static class ConversationNodeUtility
    {
        /// <summary>
        /// Finds the next available node ID in the conversation
        /// </summary>
        /// <param name="nodes">List of existing conversation nodes</param>
        /// <returns>Next available ID starting from 1</returns>
        public static int GetNextAvailableId(List<ConversationNode> nodes)
        {
            if (nodes == null || nodes.Count == 0)
                return 1;

            // Get the highest ID and try incrementing from there
            int maxId = nodes.Max(n => n.Id);
            int candidateId = maxId + 1;

            // Check if we've hit integer max, restart from 1 if needed
            if (candidateId <= 0)
                candidateId = 1;

            // Find the first available ID
            while (nodes.Any(n => n.Id == candidateId))
            {
                candidateId++;
                if (candidateId <= 0) // Overflow check
                    candidateId = 1;
            }

            return candidateId;
        }

        /// <summary>
        /// Validates that a node ID is unique in the conversation
        /// </summary>
        /// <param name="nodeId">ID to validate</param>
        /// <param name="nodes">List of existing conversation nodes</param>
        /// <param name="excludeNodeId">Optional node ID to exclude from check (for editing existing nodes)</param>
        /// <returns>True if the ID is unique</returns>
        public static bool IsIdUnique(int nodeId, List<ConversationNode> nodes, int excludeNodeId = 0)
        {
            if (nodes == null)
                return true;

            return !nodes.Any(n => n.Id == nodeId && n.Id != excludeNodeId);
        }

        /// <summary>
        /// Removes all references to a deleted node ID from the conversation
        /// </summary>
        /// <param name="deletedNodeId">ID of the node being deleted</param>
        /// <param name="nodes">List of all conversation nodes</param>
        public static void RemoveNodeReferences(int deletedNodeId, List<ConversationNode> nodes)
        {
            if (nodes == null)
                return;

            foreach (var node in nodes)
            {
                // Clear NextNodeId if it references the deleted node
                if (node.NextNodeId == deletedNodeId)
                    node.NextNodeId = 0;

                // Clear DefaultBranchNodeId if it references the deleted node
                if (node.DefaultBranchNodeId == deletedNodeId)
                    node.DefaultBranchNodeId = 0;

                // Remove references from Options
                if (node.Options != null)
                {
                    foreach (var option in node.Options)
                    {
                        if (option.NextNodeId == deletedNodeId)
                            option.NextNodeId = 0;
                    }
                }

                // Remove references from ConditionalBranches
                if (node.conditionalBranch != null)
                {
                    if (node.conditionalBranch.NextNodeIdTrue == deletedNodeId)
                        node.conditionalBranch.NextNodeIdTrue = 0;
                    if (node.conditionalBranch.NextNodeIdFalse == deletedNodeId)
                        node.conditionalBranch.NextNodeIdFalse = 0;
                }
            }
        }

        /// <summary>
        /// Ensures a Start node exists in the conversation. Creates one if missing.
        /// </summary>
        /// <param name="conversationData">The conversation data to validate</param>
        public static void EnsureStartNodeExists(ConversationData conversationData)
        {
            if (conversationData?.ConversationManager?.Nodes == null)
                return;

            var nodes = conversationData.ConversationManager.Nodes;
            var startNode = nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);

            // If no Start node exists, create one
            if (startNode == null)
            {
                var firstNonStartNode = nodes.FirstOrDefault(n => n.NodeType != ConversationNodeType.Start);

                startNode = new ConversationNode
                {
                    Id = GetNextAvailableId(nodes),
                    NodeType = ConversationNodeType.Start,
                    Text = "",
                    SpeakerActorId = "",
                    NextNodeId = firstNonStartNode?.Id ?? 0,
                    EditorPosition = new UnityEngine.Vector2(0, 0),
                    EditorSize = new UnityEngine.Vector2(150, 80)
                };

                nodes.Insert(0, startNode);
            }
        }

        /// <summary>
        /// Validates that only one Start node exists in the conversation
        /// </summary>
        /// <param name="nodes">List of conversation nodes</param>
        /// <returns>True if there is exactly one Start node</returns>
        public static bool ValidateStartNode(List<ConversationNode> nodes)
        {
            if (nodes == null)
                return false;

            return nodes.Count(n => n.NodeType == ConversationNodeType.Start) == 1;
        }

        /// <summary>
        /// Gets all node IDs that reference a specific node
        /// </summary>
        /// <param name="targetNodeId">The node ID to find references to</param>
        /// <param name="nodes">List of all conversation nodes</param>
        /// <returns>List of node IDs that reference the target node</returns>
        public static List<int> GetNodeReferences(int targetNodeId, List<ConversationNode> nodes)
        {
            var references = new List<int>();

            if (nodes == null)
                return references;

            foreach (var node in nodes)
            {
                bool hasReference = false;

                // Check NextNodeId
                if (node.NextNodeId == targetNodeId)
                    hasReference = true;

                // Check DefaultBranchNodeId
                if (node.DefaultBranchNodeId == targetNodeId)
                    hasReference = true;

                // Check Options
                if (node.Options != null && node.Options.Any(o => o.NextNodeId == targetNodeId))
                    hasReference = true;

                // Check conditionalBranch
                if (node.conditionalBranch != null &&
                    (node.conditionalBranch.NextNodeIdTrue == targetNodeId || node.conditionalBranch.NextNodeIdFalse == targetNodeId))
                    hasReference = true;

                if (hasReference)
                    references.Add(node.Id);
            }

            return references;
        }
    }
}
