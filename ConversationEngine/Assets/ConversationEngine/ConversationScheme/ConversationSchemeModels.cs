using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace ConversationScheme
{
    [Serializable]
    public class ConversationData
    {
        public string Title = "";
        public string Description = "";
        public ResourceManager ResourceManager = new ResourceManager();
        public ConversationManager ConversationManager = new ConversationManager();
        public ConversationEditorSettings EditorSettings = new ConversationEditorSettings();
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
        public string IconPath;
        public List<string> SoundEffectPaths = new List<string>();
        public List<BodyPart> BodyParts = new List<BodyPart>();
        public Vector2 Size; // Actor canvas size; the center is the main actor pivot point (for example, feet for full body characters or neck for bust characters)
    }

    [Serializable]
    public class BodyPart
    {
        public int Id; // Unique BodyPart id inside the Actor (used by the editor)
        public string Name; // Descriptive BodyPart name
        public int AttachToPivotId; // PivotPoint id where this BodyPart will be attached
        public List<BodyPartResource> NestedResources = new List<BodyPartResource>(); // Resource variants for this BodyPart (for example, different facial expressions)
        public string CurrentResourceId; // Active resource id for this BodyPart (if missing or not found, the first resource is used)
        public List<PivotPoint> PivotPoints = new List<PivotPoint>(); // Pivot points used to attach other BodyParts; PivotPoint (0,0) is relative to the active BodyPartResource center, not the Actor
    }

    [Serializable]
    public class BodyPartResource : Resource
    {
        public List<PivotPoint> PivotPoints = new List<PivotPoint>(); // Pivot points that can override parent BodyPart pivots when this resource is active (for example, hat offset adjustments)
    }

    [Serializable]
    public class PivotPoint
    {
        public int Id; // Unique PivotPoint id inside the BodyPart (used by the editor)
        public string Name; // Descriptive PivotPoint name
        public float X; // PivotPoint X position relative to the BodyPart
        public float Y; // PivotPoint Y position relative to the BodyPart
    }

    [Serializable]
    public class ConversationManager
    {
        public List<ConversationNode> Nodes = new List<ConversationNode>();
        public List<ObservationNode> Observations = new List<ObservationNode>();
    }

    [Serializable]
    public class Node
    {
        public int Id;
        public ConversationNodeType NodeType = ConversationNodeType.Dialogue;
        public string Text;
        public string Comment;
        public Vector2 EditorPosition;
        public Vector2 EditorSize = new Vector2(200, 100);
    }

    [Serializable]
    public class ConversationNode: Node
    {
        public string SpeakerActorId;
        public int NextNodeId;
        [JsonIgnore][XmlIgnore]public ConversationNode NextNode;
        public List<ConversationOption> Options = new List<ConversationOption>();
        public List<ConversationFunction> Functions = new List<ConversationFunction>();
        public ConditionalBranch conditionalBranch = null;
        public int DefaultBranchNodeId;
    }

    [Serializable]
    public class ConversationOption : Node
    {
        public ConversationOption() { NodeType = ConversationNodeType.Option; }
        public int NextNodeId;
        [JsonIgnore][XmlIgnore] public ConversationNode NextNode;
        public List<ConditionRule> Conditions = new List<ConditionRule>();
        public bool HideOption = false;
        public string BlockText;
    }

    [Serializable]
    public class ObservationNode : Node
    {
        public ObservationNode() { NodeType = ConversationNodeType.Observation; }
    }

    public enum ConversationNodeType
    {
        Start,
        Dialogue,
        Conditional,
        Function,
        End,
        Option,
        Observation
    }

    [Serializable]
    public class ConditionalBranch
    {
        public List<ConditionRule> Conditions = new List<ConditionRule>();
        public int NextNodeIdTrue;
        [JsonIgnore][XmlIgnore] public ConversationNode NextNodeTrue;
        public int NextNodeIdFalse;
        [JsonIgnore][XmlIgnore] public ConversationNode NextNodeFalse;
    }

    [Serializable]
    public class ConditionRule
    {
        public string VariableName;
        public ComparisonOperator Operator = ComparisonOperator.Equal;
        public ValueType ValueDataType = ValueType.String;
        public string Value;
        public bool IsValueVariable = false;
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

    public enum ValueType
    {
        String,
        Integer,
        Decimal,
        Boolean
    }

    [Serializable]
    public class ConversationFunction
    {
        public string MethodName;
        public Dictionary<string, string> Parameters = new Dictionary<string, string>();
        public int Timestamp = 0;
    }

    [Serializable]
    public class ConversationEditorSettings
    {
        public float Zoom = 1f;
        public bool IsResourcePanelHidden = false;
        public bool IsReadOnly = false;
        public Vector2 CameraPosition = Vector2.zero;
    }
}