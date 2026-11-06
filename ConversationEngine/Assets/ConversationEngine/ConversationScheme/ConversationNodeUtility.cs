using System.Collections.Generic;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEngine;
namespace ConversationScheme
{
    public static class ConversationNodeUtility
    {
        #region Id
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
        #endregion

        #region Validation Helper
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
            var startNode = nodes.FirstOrDefault(n => ConversationNodeUtility.ParseConversationNodeType(n.NodeType) == ConversationNodeType.Start);
            if (startNode == null)
            {
                var firstNonStartNode = nodes.FirstOrDefault(n => ConversationNodeUtility.ParseConversationNodeType(n.NodeType) != ConversationNodeType.Start);
                startNode = new ConversationNode
                {
                    Id = GetNextAvailableId(nodes.Cast<Node>().ToList()),
                    NodeType = ConversationNodeType.Start.ToString(),
                    Text = "",
                    SpeakerActorId = "",
                    NextNodeId = firstNonStartNode?.Id ?? 0,
                    EditorPosition = new UnityEngine.Vector2(0, 0),
                    EditorSize = new UnityEngine.Vector2(150, 80)
                };
                nodes.Insert(0, startNode);
            }
        }
        public static void ValidateEmptyElements(ConversationData conversationData)
        {
            if (conversationData?.ConversationManager == null) return;
            var conversationManager = conversationData.ConversationManager;
            var nodesToValidate = Enumerable.Empty<Node>();
            if (conversationManager.Nodes != null)
            {
                nodesToValidate = nodesToValidate
                    .Concat(conversationManager.Nodes.Cast<Node>())
                    .Concat(conversationManager.Nodes.Where(node => node?.Options != null).SelectMany(node => node.Options).Cast<Node>());
            }
            if (conversationManager.Observations != null)
                nodesToValidate = nodesToValidate.Concat(conversationManager.Observations.Cast<Node>());
            foreach (var node in nodesToValidate.Where(node => node?.lstAlternateText != null))
                node.lstAlternateText.RemoveAll(alternateText => alternateText == null || string.IsNullOrWhiteSpace(alternateText.Text));
            foreach (var node in conversationManager.Nodes.Where(node => node?.lstAlternateTitle != null))
                node.lstAlternateTitle.RemoveAll(alternateTitle => alternateTitle == null || string.IsNullOrWhiteSpace(alternateTitle.Text));
        }
        public static void ValidateEmptyActorElements(Actor actor)
        {
            if (actor?.lstAlternateName == null) return;
            actor.lstAlternateName.RemoveAll(alternateName => alternateName == null || string.IsNullOrWhiteSpace(alternateName.Text));
        }
        #endregion

        #region Parsing Helper
        public static bool ValidateStartNode(List<Node> nodes)
        {
            if (nodes == null) return false;
            return nodes.Count(n => ConversationNodeUtility.ParseConversationNodeType(n.NodeType) == ConversationNodeType.Start) == 1;
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
        public static Language ParseLanguage(string languageValue)
        {
            if (string.IsNullOrWhiteSpace(languageValue)) return Language.English;
            return System.Enum.TryParse(languageValue, true, out Language parsedLanguage) ? parsedLanguage : Language.English;
        }
        public static ValueType ParseValueType(string valueTypeValue)
        {
            if (string.IsNullOrWhiteSpace(valueTypeValue)) return ValueType.String;
            return System.Enum.TryParse(valueTypeValue, true, out ValueType parsedValueType) ? parsedValueType : ValueType.String;
        }
        public static ComparisonOperator ParseComparisonOperator(string comparisonOperatorValue)
        {
            if (string.IsNullOrWhiteSpace(comparisonOperatorValue)) return ComparisonOperator.Equal;
            return System.Enum.TryParse(comparisonOperatorValue, true, out ComparisonOperator parsedComparisonOperator) ? parsedComparisonOperator : ComparisonOperator.Equal;
        }
        public static ConversationNodeType ParseConversationNodeType(string conversationNodeTypeValue)
        {
            if (string.IsNullOrWhiteSpace(conversationNodeTypeValue)) return ConversationNodeType.Start;
            return System.Enum.TryParse(conversationNodeTypeValue, true, out ConversationNodeType parsedConversationNodeType) ? parsedConversationNodeType : ConversationNodeType.Start;
        }
        public static AudioChannelType ParseAudioChannelType(string audioChannelTypeValue)
        {
            if (string.IsNullOrWhiteSpace(audioChannelTypeValue)) return AudioChannelType.BackgroundMusic;
            return System.Enum.TryParse(audioChannelTypeValue, true, out AudioChannelType parsedAudioChannelType) ? parsedAudioChannelType : AudioChannelType.BackgroundMusic;
        }
        #endregion

        #region JSON Serialization Helper
        public static string SerializeWithoutEmptyValues(object obj)
        {
            string json = JsonUtility.ToJson(obj, true);
            if (string.IsNullOrEmpty(json)) return json;
            JToken rootToken = JToken.Parse(json);
            PruneEmptyValues(rootToken);
            return rootToken.ToString(Formatting.Indented);
        }
        private static bool PruneEmptyValues(JToken token)
        {
            if (token == null) return true;
            switch (token.Type)
            {
                case JTokenType.Object:
                    JObject tokenObject = (JObject)token;
                    var properties = tokenObject.Properties().ToList();
                    foreach (var property in properties)
                    {
                        bool mustRemove = PruneEmptyValues(property.Value);
                        if (mustRemove) property.Remove();
                    }
                    return !tokenObject.Properties().Any();
                case JTokenType.Array:
                    JArray tokenArray = (JArray)token;
                    for (int index = tokenArray.Count - 1; index >= 0; index--)
                    {
                        bool mustRemove = PruneEmptyValues(tokenArray[index]);
                        if (mustRemove) tokenArray[index].Remove();
                    }
                    return !tokenArray.Any();
                case JTokenType.String:
                    string stringValue = token.Value<string>();
                    return string.IsNullOrEmpty(stringValue);
                case JTokenType.Null:
                case JTokenType.Undefined:
                    return true;
                default:
                    return false;
            }
        }
        #endregion
    }
}