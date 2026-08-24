using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace ConversationScheme
{
    [Serializable]
    public class ConversationData
    {
        public ResourceManager ResourceManager = new ResourceManager();
        public ConversationManager ConversationManager = new ConversationManager();
    }

    [Serializable]
    public class ResourceManager
    {
        public List<SceneBackground> SceneBackgrounds = new List<SceneBackground>();
        public List<AudioBackground> AudioBackgrounds = new List<AudioBackground>();
        public List<Actor> Actors = new List<Actor>();
    }

    [Serializable]
    public abstract class Resource
    {
        public string Id;
        public string Path;

        [XmlIgnore]
        [NonSerialized]
        public GameObject gameObject;
    }

    [Serializable]
    public class SceneBackground : Resource
    {
    }

    [Serializable]
    public class AudioBackground : Resource
    {
        public AudioChannelType AudioType = AudioChannelType.BackgroundMusic;
    }

    public enum AudioChannelType
    {
        BackgroundMusic,
        SoundEffect,
        Voice
    }

    [Serializable]
    public class Actor : Resource
    {
        public List<string> SoundEffectPaths = new List<string>();
        public List<BodyPart> BodyParts = new List<BodyPart>();
    }

    [Serializable]
    public class BodyPart
    {
        public string Id;
        public string AttachToPivotId;
        public List<BodyPartResource> NestedResources = new List<BodyPartResource>();
        public string CurrentResourceId;
        public List<PivotPoint> PivotPoints = new List<PivotPoint>();
    }

    [Serializable]
    public class BodyPartResource : Resource
    {
    }

    [Serializable]
    public class PivotPoint
    {
        public string Id;
        public float X;
        public float Y;
    }

    [Serializable]
    public class ConversationManager
    {
        public List<ConversationNode> Nodes = new List<ConversationNode>();
    }

    [Serializable]
    public class ConversationNode
    {
        public string Id;
        public ConversationNodeType NodeType = ConversationNodeType.Dialogue;
        public string ReferenceFlag;
        public string SpeakerActorId;
        public string Text;
        public string NextNodeId;
        public string NextReferenceFlag;
        public List<ConversationOption> Options = new List<ConversationOption>();
        public List<ConversationFunction> Functions = new List<ConversationFunction>();
        public List<ConditionalBranch> ConditionalBranches = new List<ConditionalBranch>();
        public string DefaultBranchNodeId;
        public string DefaultBranchReferenceFlag;
    }

    public enum ConversationNodeType
    {
        Dialogue,
        Conditional
    }

    [Serializable]
    public class ConversationOption
    {
        public string Text;
        public string TargetNodeId;
        public string TargetReferenceFlag;
        public List<ConditionRule> Conditions = new List<ConditionRule>();
    }

    [Serializable]
    public class ConditionalBranch
    {
        public List<ConditionRule> Conditions = new List<ConditionRule>();
        public string TargetNodeId;
        public string TargetReferenceFlag;
    }

    [Serializable]
    public class ConditionRule
    {
        public string VariableName;
        public ComparisonOperator Operator = ComparisonOperator.Equal;
        public string Value;
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual
    }

    [Serializable]
    public class ConversationFunction
    {
        public string MethodName;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public int Timestamp = 0;
    }
}
