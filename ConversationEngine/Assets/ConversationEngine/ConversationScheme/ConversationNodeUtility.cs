using System.Collections.Generic;
using System.Linq;
namespace ConversationScheme
{
    public static class ConversationNodeUtility
    {
        public static int GetNextAvailableId(List<Node> nodes)
        {
            if (nodes == null || nodes.Count == 0) return 1;
            int maxId = nodes.Max(n => n.Id);
            int candidateId = maxId + 1;
            if (candidateId <= 0) candidateId = 1;
            while (nodes.Any(n => n.Id == candidateId))
            {
                candidateId++;
                if (candidateId <= 0)
                    candidateId = 1;
            }
            return candidateId;
        }
        public static bool IsIdUnique(int nodeId, List<Node> nodes, int excludeNodeId = 0)
        {
            if (nodes == null) return true;
            return !nodes.Any(n => n.Id == nodeId && n.Id != excludeNodeId);
        }
        public static void RemoveNodeReferences(int deletedNodeId, List<ConversationNode> nodes)
        {
            if (nodes == null) return;
            foreach (var node in nodes)
            {
                if (node.NextNodeId == deletedNodeId) node.NextNodeId = 0;
                if (node.DefaultBranchNodeId == deletedNodeId) node.DefaultBranchNodeId = 0;
                if (node.Options != null)
                {
                    foreach (var option in node.Options)
                    {
                        if (option.NextNodeId == deletedNodeId) option.NextNodeId = 0;
                    }
                }
                if (node.conditionalBranch != null)
                {
                    if (node.conditionalBranch.NextNodeIdTrue == deletedNodeId) node.conditionalBranch.NextNodeIdTrue = 0;
                    if (node.conditionalBranch.NextNodeIdFalse == deletedNodeId) node.conditionalBranch.NextNodeIdFalse = 0;
                }
            }
        }
        public static void EnsureStartNodeExists(ConversationData conversationData)
        {
            if (conversationData?.ConversationManager?.Nodes == null) return;
            var nodes = conversationData.ConversationManager.Nodes;
            var startNode = nodes.FirstOrDefault(n => n.NodeType == ConversationNodeType.Start);
            if (startNode == null)
            {
                var firstNonStartNode = nodes.FirstOrDefault(n => n.NodeType != ConversationNodeType.Start);
                startNode = new ConversationNode
                {
                    Id = GetNextAvailableId(nodes.Cast<Node>().ToList()),
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
        public static bool ValidateStartNode(List<Node> nodes)
        {
            if (nodes == null) return false;
            return nodes.Count(n => n.NodeType == ConversationNodeType.Start) == 1;
        }
        public static List<int> GetNodeReferences(int targetNodeId, List<ConversationNode> nodes)
        {
            var references = new List<int>();
            if (nodes == null) return references;
            foreach (var node in nodes)
            {
                bool hasReference = false;
                if (node.NextNodeId == targetNodeId)
                    hasReference = true;
                if (node.DefaultBranchNodeId == targetNodeId)
                    hasReference = true;
                if (node.Options != null && node.Options.Any(o => o.NextNodeId == targetNodeId))
                    hasReference = true;
                if (node.conditionalBranch != null && (node.conditionalBranch.NextNodeIdTrue == targetNodeId || node.conditionalBranch.NextNodeIdFalse == targetNodeId))
                    hasReference = true;
                if (hasReference)
                    references.Add(node.Id);
            }
            return references;
        }
    }
}