using ConversationScheme;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
namespace ConversationEditor.Panel
{
    public class EditorInspector
    {
        #region Core Data
        private ConversationEditorCore conversationEditorCore = ConversationEditorCore.GetSingleton();
        private ConversationData conversationData { set { conversationEditorCore.conversationData = value; } get { return conversationEditorCore.conversationData; } }
        private readonly EditorWindow ownerWindow;
        private ConversationGraphView graphView;
        #endregion

        #region Inspector Drafts
        private string selectedFunctionCategory = "Custom";
        private string selectedFunctionName = "";
        private string customFunctionName = "";
        private Dictionary<string, string> pendingFunctionParameters = new Dictionary<string, string>();
        private string pendingCustomParameterName = "";
        private string pendingCustomParameterValue = "";
        private int pendingFunctionTimestamp = 0;
        private readonly Dictionary<ConversationFunction, bool> functionParameterFoldouts = new Dictionary<ConversationFunction, bool>();
        private readonly Dictionary<ConversationNode, ConversationOption> pendingOptionsByNode = new Dictionary<ConversationNode, ConversationOption>();
        private readonly Dictionary<ConditionalBranch, ConditionRule> pendingConditionsByBranch = new Dictionary<ConditionalBranch, ConditionRule>();
        private static readonly ComparisonOperator[] comparisonOperatorValues =
        {
            ComparisonOperator.Equal,
            ComparisonOperator.NotEqual,
            ComparisonOperator.GreaterThan,
            ComparisonOperator.GreaterOrEqual,
            ComparisonOperator.LessThan,
            ComparisonOperator.LessOrEqual
        };
        private static readonly string[] comparisonOperatorLabels =
        {
            "Equal (=)",
            "NotEqual (!=)",
            "GreaterThan (>)",
            "GreaterOrEqual (>=)",
            "LessThan (<)",
            "LessOrEqual (<=)"
        };
        #endregion

        #region Zoom Controls
        private const float optionDefaultWidth = ConversationEditorCore.optionDefaultWidth;
        private const float optionDefaultHeight = ConversationEditorCore.optionDefaultHeight;
        private const float optionDefaultSpacing = ConversationEditorCore.optionDefaultSpacing;
        private const float minEditorNodeSize = ConversationEditorCore.minEditorNodeSize;
        #endregion

        #region UI State
        private Vector2 inspectorScrollPos;
        #endregion

        #region Events
        public System.Action OnDirty;
        #endregion

        #region Inspector Panel
        public EditorInspector(EditorWindow ownerWindow, ConversationGraphView graphView)
        {
            this.ownerWindow = ownerWindow;
            this.graphView = graphView;
        }

        public void DrawInspectorPanel()
        {
            inspectorScrollPos = EditorGUILayout.BeginScrollView(inspectorScrollPos);
            var graphSelectedNode = graphView?.SelectedNode;
            var graphSelectedOption = graphView?.SelectedOption;
            var graphSelectedBranch = graphView?.SelectedBranch;
            // Prefer showing option inspector when an option is selected (options also reference a parent node)
            if (graphSelectedOption != null)
            {
                DrawOptionInspector(graphSelectedOption);
            }
            else if (graphSelectedNode != null)
            {
                DrawNodeInspector(graphSelectedNode);
            }
            else if (graphSelectedBranch != null)
            {
                DrawBranchInspector(graphSelectedBranch);
            }
            else
            {
                EditorGUILayout.HelpBox("Select a node to edit its properties", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNodeInspector(ConversationNode node)
        {
            EditorGUILayout.LabelField("Node Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(new GUIContent("ID", "Unique node identifier."), node.Id, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup(new GUIContent("Node Type", "Node behavior type (read-only)."), node.NodeType, GUILayout.ExpandWidth(true));
            EditorGUI.EndDisabledGroup();
            switch (node.NodeType)
            {
                case ConversationNodeType.Start:
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Connection", "Outgoing connection settings for start node."), EditorStyles.boldLabel);
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Target node for flow continuation.");
                    break;
                case ConversationNodeType.Conditional:
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Conditional Branches", "Condition-based outgoing branch settings."), EditorStyles.boldLabel);
                    DrawConditionalBranchSection(node);
                    break;
                case ConversationNodeType.End:
                    break;
                default:
                    DrawDialogueOrFunctionInspector(node);
                    break;
            }
            DrawSectionSeparator();
            EditorGUILayout.LabelField("Editor Properties", EditorStyles.boldLabel);
            node.EditorPosition = EditorGUILayout.Vector2Field(new GUIContent("Position", "Graph center position for this node."), node.EditorPosition, GUILayout.ExpandWidth(true));
            node.EditorSize = ClampEditorSize(EditorGUILayout.Vector2Field(new GUIContent("Size", "Graph size for this node. Minimum X/Y is 20."), node.EditorSize, GUILayout.ExpandWidth(true)));
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawDialogueOrFunctionInspector(ConversationNode node)
        {
            switch (node.NodeType)
            {
                case ConversationNodeType.Dialogue:
                    DrawSpeakerAndTextFields(node);
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Options", "Player options available from this dialogue node."), EditorStyles.boldLabel);
                    DrawOptionSection(node);
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Functions", "Timed functions executed while this node is active."), EditorStyles.boldLabel);
                    if (node.Functions == null) node.Functions = new List<ConversationFunction>();
                    DrawFunctionList(node.Functions);
                    return;
                case ConversationNodeType.Function:
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    DrawSectionSeparator();
                    EditorGUILayout.LabelField(new GUIContent("Functions", "Timed functions executed while this node is active."), EditorStyles.boldLabel);
                    if (node.Functions == null) node.Functions = new List<ConversationFunction>();
                    DrawFunctionList(node.Functions);
                    return;
                default:
                    node.NextNodeId = DrawNodeIdDropdown("Next Node", node.NextNodeId, node, "Default target node for flow continuation.");
                    return;
            }
        }

        private void DrawSpeakerAndTextFields(ConversationNode node)
        {
            if (conversationData.ResourceManager.Actors.Count > 0)
            {
                var actorIds = conversationData.ResourceManager.Actors.Select(a => a.Id).ToList();
                actorIds.Insert(0, "(None)");
                int currentIndex = string.IsNullOrEmpty(node.SpeakerActorId) ? 0 : actorIds.IndexOf(node.SpeakerActorId);
                if (currentIndex < 0) currentIndex = 0;
                int newIndex = EditorGUILayout.Popup(new GUIContent("Speaker Actor", "Actor speaking in this node."), currentIndex, actorIds.ToArray(), GUILayout.ExpandWidth(true));
                node.SpeakerActorId = newIndex == 0 ? "" : actorIds[newIndex];
            }
            else
            {
                node.SpeakerActorId = EditorGUILayout.TextField(new GUIContent("Speaker Actor ID", "Actor identifier for this node."), node.SpeakerActorId, GUILayout.ExpandWidth(true));
            }
            EditorGUILayout.LabelField(new GUIContent("Text", "Dialogue text shown to the player."));
            node.Text = EditorGUILayout.TextArea(node.Text, GUILayout.MinHeight(60), GUILayout.ExpandWidth(true));
        }

        private void DrawOptionInspector(ConversationOption option)
        {
            EditorGUILayout.LabelField("Option Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            int selectedOptionIndex = graphView?.SelectedNode?.Options?.IndexOf(option) ?? 0;
            EnsureOptionEditorData(graphView?.SelectedNode, option, Mathf.Max(0, selectedOptionIndex));
            option.Text = EditorGUILayout.TextField(new GUIContent("Text", "Option text shown to the player."), option.Text, GUILayout.ExpandWidth(true));
            option.NextNodeId = DrawNodeIdDropdown("Next Node", option.NextNodeId, graphView?.SelectedNode, "Target node for this option.");
            option.EditorPosition = EditorGUILayout.Vector2Field(new GUIContent("Position", "Local graph position relative to the parent node."), option.EditorPosition, GUILayout.ExpandWidth(true));
            option.EditorSize = ClampEditorSize(EditorGUILayout.Vector2Field(new GUIContent("Size", "Graph size for this option node. Minimum X/Y is 20."), option.EditorSize, GUILayout.ExpandWidth(true)));
            EditorGUILayout.Space();
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private void DrawBranchInspector(ConditionalBranch branch)
        {
            EditorGUILayout.LabelField("Branch Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;
            EditorGUI.BeginChangeCheck();
            branch.NextNodeIdTrue = DrawNodeIdDropdown("Next Node (True)", branch.NextNodeIdTrue, graphView?.SelectedNode, "Target node when branch evaluates true.");
            branch.NextNodeIdFalse = DrawNodeIdDropdown("Next Node (False)", branch.NextNodeIdFalse, graphView?.SelectedNode, "Target node when branch evaluates false.");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawConditionList(branch.Conditions);
            if (EditorGUI.EndChangeCheck()) MarkDirty();
            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        private int DrawNodeIdDropdown(string label, int currentNodeId, ConversationNode excludeNode, string tooltip = "")
        {
            var nodeOptions = new List<string>();
            var nodeIds = new List<int>();
            nodeOptions.Add("NINGUNO");
            nodeIds.Add(0);
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.NodeType == ConversationNodeType.Start || node == excludeNode) continue;
                nodeOptions.Add(ConversationEditorHelpers.GetNodeDropdownText(node));
                nodeIds.Add(node.Id);
            }
            int currentIndex = nodeIds.IndexOf(currentNodeId);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), currentIndex, nodeOptions.ToArray());
            int newNodeId = nodeIds[newIndex];
            return newNodeId;
        }

        private int DrawNodeIdDropdownCompact(string label, int currentNodeId, ConversationNode excludeNode, string tooltip)
        {
            var nodeOptions = new List<string>();
            var nodeIds = new List<int>();
            nodeOptions.Add("NINGUNO");
            nodeIds.Add(0);
            foreach (var node in conversationData.ConversationManager.Nodes)
            {
                if (node.NodeType == ConversationNodeType.Start || node == excludeNode) continue;
                nodeOptions.Add(ConversationEditorHelpers.GetNodeDropdownText(node));
                nodeIds.Add(node.Id);
            }
            int currentIndex = nodeIds.IndexOf(currentNodeId);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUILayout.Popup(new GUIContent(label, tooltip), currentIndex, nodeOptions.ToArray(), GUILayout.ExpandWidth(true));
            return nodeIds[newIndex];
        }

        private void DrawOptionSection(ConversationNode node)
        {
            if (node.Options == null) node.Options = new List<ConversationOption>();
            if (!pendingOptionsByNode.TryGetValue(node, out var pendingOption))
            {
                pendingOption = new ConversationOption { Text = "", NextNodeId = 0, Conditions = new List<ConditionRule>() };
                pendingOptionsByNode[node] = pendingOption;
            }
            EditorGUILayout.BeginHorizontal();
            pendingOption.Text = EditorGUILayout.TextField(new GUIContent("", "New option text."), pendingOption.Text ?? "", GUILayout.ExpandWidth(true));
            pendingOption.NextNodeId = DrawNodeIdDropdownCompact("", pendingOption.NextNodeId, node, "Target node for the new option.");
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button(new GUIContent("+", "Add option."), GUILayout.Width(28)))
            {
                if (string.IsNullOrWhiteSpace(pendingOption.Text)) EditorUtility.DisplayDialog("Invalid Option", "Option text cannot be empty.", "OK");
                else
                {
                    Undo.RecordObject(ownerWindow, "Add Option");
                    node.Options.Add(CreateOptionForNode(node, pendingOption.Text.Trim(), pendingOption.NextNodeId, node.Options.Count));
                    pendingOption.Text = "";
                    pendingOption.NextNodeId = 0;
                    MarkDirty();
                }
            }
            GUI.backgroundColor = oldColor;
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < node.Options.Count; i++)
            {
                if (node.Options[i].Conditions == null) node.Options[i].Conditions = new List<ConditionRule>();
                EnsureOptionEditorData(node, node.Options[i], i);
                EditorGUILayout.BeginHorizontal("box");
                node.Options[i].Text = EditorGUILayout.TextField(new GUIContent("", "Option text."), node.Options[i].Text ?? "", GUILayout.ExpandWidth(true));
                node.Options[i].NextNodeId = DrawNodeIdDropdownCompact("", node.Options[i].NextNodeId, node, "Target node for this option.");
                oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("X", "Remove this option."), GUILayout.Width(28)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Option");
                    node.Options.RemoveAt(i);
                    MarkDirty();
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                    break;
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
            }
        }

        private ConversationOption CreateOptionForNode(ConversationNode node, string text, int nextNodeId, int optionIndex)
        {
            return new ConversationOption
            {
                Text = text,
                NextNodeId = nextNodeId,
                Conditions = new List<ConditionRule>(),
                EditorPosition = GenerateOptionPosition(node, optionIndex),
                EditorSize = new Vector2(optionDefaultWidth, optionDefaultHeight)
            };
        }

        private void EnsureOptionEditorData(ConversationNode node, ConversationOption option, int optionIndex)
        {
            if (node == null || option == null) return;
            option.EditorSize = ClampEditorSize(option.EditorSize);
            if (option.EditorPosition == Vector2.zero) option.EditorPosition = GenerateOptionPosition(node, optionIndex);
        }

        private Vector2 ClampEditorSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(minEditorNodeSize, size.x), Mathf.Max(minEditorNodeSize, size.y));
        }

        private Vector2 GenerateOptionPosition(ConversationNode node, int optionIndex)
        {
            if (node == null) return new Vector2(optionDefaultWidth + optionDefaultSpacing, optionDefaultSpacing);
            float x = node.EditorSize.x + optionDefaultSpacing + Random.Range(10f, 45f);
            float y = (optionDefaultHeight + optionDefaultSpacing) * optionIndex + Random.Range(-20f, 20f);
            return new Vector2(x, y);
        }

        private void DrawConditionalBranchSection(ConversationNode node)
        {
            // Ensure singular conditionalBranch exists for conditional nodes
            if (node.conditionalBranch == null) node.conditionalBranch = new ConditionalBranch { Conditions = new List<ConditionRule>(), NextNodeIdTrue = 0, NextNodeIdFalse = 0 };
            var branch = node.conditionalBranch;
            if (branch.Conditions == null) branch.Conditions = new List<ConditionRule>();
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Conditional Branch", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            branch.NextNodeIdTrue = DrawNodeIdDropdownCompact("Next Node (True)", branch.NextNodeIdTrue, node, "Target node when branch evaluates true.");
            branch.NextNodeIdFalse = DrawNodeIdDropdownCompact("Next Node (False)", branch.NextNodeIdFalse, node, "Target node when branch evaluates false.");
            EditorGUILayout.EndHorizontal();
            DrawConditionAddSection(branch);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conditions", EditorStyles.boldLabel);
            DrawExistingConditionList(branch.Conditions);
            EditorGUILayout.EndVertical();
            node.DefaultBranchNodeId = DrawNodeIdDropdown("Default Branch Node", node.DefaultBranchNodeId, node, "Fallback target node when no branch matches.");
        }

        private void DrawConditionAddSection(ConditionalBranch branch)
        {
            if (!pendingConditionsByBranch.TryGetValue(branch, out var pendingCondition))
            {
                pendingCondition = new ConditionRule
                {
                    VariableName = "",
                    Operator = ComparisonOperator.Equal,
                    ValueDataType = ValueType.String,
                    Value = "",
                    IsValueVariable = false
                };
                pendingConditionsByBranch[branch] = pendingCondition;
            }
            EditorGUILayout.BeginVertical("box");
            pendingCondition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type expected for the condition value."), pendingCondition.ValueDataType, GUILayout.ExpandWidth(true));
            pendingCondition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name for this condition."), pendingCondition.VariableName ?? "", GUILayout.ExpandWidth(true));
            pendingCondition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), pendingCondition.Operator);
            DrawConditionValueRow(pendingCondition, false);
            if (GUILayout.Button(new GUIContent("Add", "Add this condition to the branch."), GUILayout.ExpandWidth(true)))
            {
                if (string.IsNullOrWhiteSpace(pendingCondition.VariableName)) EditorUtility.DisplayDialog("Invalid Condition", "Variable name cannot be empty.", "OK");
                else if (!IsConditionValueValid(pendingCondition)) EditorUtility.DisplayDialog("Invalid Condition", "Value does not match the selected value type.", "OK");
                else
                {
                    Undo.RecordObject(ownerWindow, "Add Condition");
                    branch.Conditions.Add(new ConditionRule
                    {
                        VariableName = pendingCondition.VariableName.Trim(),
                        Operator = pendingCondition.Operator,
                        ValueDataType = pendingCondition.ValueDataType,
                        Value = pendingCondition.ValueDataType == ValueType.Boolean ? ConversationEditorHelpers.NormalizeBooleanValue(pendingCondition.Value) : (pendingCondition.Value ?? ""),
                        IsValueVariable = pendingCondition.ValueDataType == ValueType.Boolean ? false : pendingCondition.IsValueVariable
                    });
                    pendingCondition.Operator = ComparisonOperator.Equal;
                    pendingCondition.Value = pendingCondition.ValueDataType == ValueType.Boolean ? "true" : "";
                    pendingCondition.IsValueVariable = false;
                    MarkDirty();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawExistingConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Count; i++)
            {
                var condition = conditions[i];
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(true);
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type used by this condition."), condition.ValueDataType, GUILayout.ExpandWidth(true));
                condition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name used by this condition."), condition.VariableName ?? "", GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
                condition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), condition.Operator);
                DrawConditionValueRow(condition, true);
                if (GUILayout.Button(new GUIContent("Remove", "Remove this condition."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConditionValueRow(ConditionRule condition, bool lockTypeSpecificFields)
        {
            if (condition.ValueDataType == ValueType.Boolean)
            {
                bool currentBool = ConversationEditorHelpers.ParseBooleanCondition(condition.Value);
                bool newBool = EditorGUILayout.ToggleLeft(new GUIContent("Is true", "Boolean value for this condition."), currentBool);
                condition.Value = newBool ? "true" : "false";
                condition.IsValueVariable = false;
                return;
            }
            EditorGUILayout.BeginHorizontal();
            condition.Value = EditorGUILayout.TextField(new GUIContent("Value", "Value to compare against."), condition.Value ?? "", GUILayout.ExpandWidth(true));
            bool previousGuiState = GUI.enabled;
            if (lockTypeSpecificFields) GUI.enabled = true;
            condition.IsValueVariable = EditorGUILayout.ToggleLeft(new GUIContent("variable", "Treat value as a variable name."), condition.IsValueVariable, GUILayout.Width(80));
            GUI.enabled = previousGuiState;
            EditorGUILayout.EndHorizontal();
        }

        private ComparisonOperator DrawComparisonOperatorDropdown(GUIContent label, ComparisonOperator value)
        {
            int index = System.Array.IndexOf(comparisonOperatorValues, value);
            if (index < 0) index = 0;
            int newIndex = EditorGUILayout.Popup(label, index, comparisonOperatorLabels, GUILayout.ExpandWidth(true));
            return comparisonOperatorValues[newIndex];
        }

        private bool IsConditionValueValid(ConditionRule condition)
        {
            if (condition == null) return false;
            if (condition.IsValueVariable) return !string.IsNullOrWhiteSpace(condition.Value);
            switch (condition.ValueDataType)
            {
                case ValueType.Integer:
                    return int.TryParse(condition.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
                case ValueType.Decimal:
                    return decimal.TryParse(condition.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
                case ValueType.Boolean:
                    return true;
                default:
                    return true;
            }
        }
        private void DrawConditionList(List<ConditionRule> conditions)
        {
            if (conditions == null) return;
            for (int i = 0; i < conditions.Count; i++)
            {
                EditorGUILayout.BeginVertical("box");
                var condition = conditions[i];
                EditorGUILayout.LabelField($"Condition {i + 1}", EditorStyles.boldLabel);
                condition.VariableName = EditorGUILayout.TextField(new GUIContent("Variable", "Variable name for this condition."), condition.VariableName ?? "", GUILayout.ExpandWidth(true));
                condition.Operator = DrawComparisonOperatorDropdown(new GUIContent("Operator", "Comparison operator."), condition.Operator);
                condition.ValueDataType = (ValueType)EditorGUILayout.EnumPopup(new GUIContent("Value Type", "Data type expected for this condition."), condition.ValueDataType, GUILayout.ExpandWidth(true));
                DrawConditionValueRow(condition, false);
                if (GUILayout.Button(new GUIContent("Remove", "Remove this condition."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Condition");
                    conditions.RemoveAt(i);
                    MarkDirty();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button(new GUIContent("Add", "Add a new condition."), GUILayout.ExpandWidth(true), GUILayout.Height(25)))
            {
                Undo.RecordObject(ownerWindow, "Add Condition");
                var newCondition = new ConditionRule
                {
                    VariableName = "newVariable",
                    Operator = ComparisonOperator.Equal,
                    ValueDataType = ValueType.String,
                    Value = "",
                    IsValueVariable = false
                };
                conditions.Add(newCondition);
                MarkDirty();
            }
        }

        private void DrawFunctionList(List<ConversationFunction> functions)
        {
            if (functions == null) return;
            DrawFunctionAddSection(functions);
            DrawSectionSeparator();
            for (int i = 0; i < functions.Count; i++)
            {
                var func = functions[i];
                if (func.Parameters == null) func.Parameters = new Dictionary<string, string>();
                EditorGUILayout.BeginVertical("box");
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(new GUIContent("Function", "Captured function name."), func.MethodName ?? "", GUILayout.ExpandWidth(true));
                EditorGUI.EndDisabledGroup();
                if (!functionParameterFoldouts.TryGetValue(func, out var isExpanded)) isExpanded = false;
                isExpanded = EditorGUILayout.Foldout(isExpanded, new GUIContent("Parameters", "Show or hide parameter values."), true);
                functionParameterFoldouts[func] = isExpanded;
                if (isExpanded)
                {
                    var parameterOrder = GetFunctionParameterOrder(func);
                    foreach (var parameterName in parameterOrder)
                    {
                        if (!func.Parameters.ContainsKey(parameterName)) func.Parameters[parameterName] = "";
                        EditorGUILayout.BeginHorizontal();
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUILayout.TextField(new GUIContent("", "Parameter name."), parameterName, GUILayout.Width(130));
                        EditorGUI.EndDisabledGroup();
                        func.Parameters[parameterName] = EditorGUILayout.TextField(new GUIContent("", "Parameter value."), func.Parameters[parameterName] ?? "", GUILayout.ExpandWidth(true));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                func.Timestamp = EditorGUILayout.IntField(new GUIContent("Timestamp", "Execution order for this function."), func.Timestamp, GUILayout.ExpandWidth(true));
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button(new GUIContent("Remove", "Remove this function."), GUILayout.ExpandWidth(true)))
                {
                    Undo.RecordObject(ownerWindow, "Remove Function");
                    functionParameterFoldouts.Remove(func);
                    functions.RemoveAt(i);
                    MarkDirty();
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndVertical();
                    break;
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawFunctionAddSection(List<ConversationFunction> functions)
        {
            var categoryNames = ConversationFunctionLibrary.GetFunctionCategoryNames();
            if (categoryNames.Length == 0) categoryNames = new[] { "Custom" };
            if (!categoryNames.Contains(selectedFunctionCategory)) selectedFunctionCategory = categoryNames[0];
            int categoryIndex = System.Array.IndexOf(categoryNames, selectedFunctionCategory);
            if (categoryIndex < 0) categoryIndex = 0;
            int newCategoryIndex = EditorGUILayout.Popup(new GUIContent("Category", "Function category filter."), categoryIndex, categoryNames, GUILayout.ExpandWidth(true));
            if (newCategoryIndex != categoryIndex)
            {
                selectedFunctionCategory = categoryNames[newCategoryIndex];
                selectedFunctionName = "";
                pendingFunctionParameters = new Dictionary<string, string>();
            }
            bool isCustomCategory = selectedFunctionCategory == "Custom";
            if (isCustomCategory)
            {
                customFunctionName = EditorGUILayout.TextField(new GUIContent("Function", "Custom function name."), customFunctionName ?? "", GUILayout.ExpandWidth(true));
                EditorGUILayout.BeginHorizontal();
                pendingCustomParameterName = EditorGUILayout.TextField(new GUIContent("", "Custom parameter name."), pendingCustomParameterName ?? "", GUILayout.ExpandWidth(true));
                pendingCustomParameterValue = EditorGUILayout.TextField(new GUIContent("", "Custom parameter value."), pendingCustomParameterValue ?? "", GUILayout.ExpandWidth(true));
                Color oldColor = GUI.backgroundColor;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button(new GUIContent("+", "Add custom parameter."), GUILayout.Width(28)))
                {
                    if (string.IsNullOrWhiteSpace(pendingCustomParameterName)) EditorUtility.DisplayDialog("Invalid Parameter", "Parameter name cannot be empty.", "OK");
                    else
                    {
                        string parameterName = pendingCustomParameterName.Trim();
                        pendingFunctionParameters[parameterName] = pendingCustomParameterValue ?? "";
                        pendingCustomParameterName = "";
                        pendingCustomParameterValue = "";
                    }
                }
                GUI.backgroundColor = oldColor;
                EditorGUILayout.EndHorizontal();
                foreach (var key in pendingFunctionParameters.Keys.ToList())
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.TextField(new GUIContent("", "Captured parameter name."), key, GUILayout.Width(130));
                    EditorGUI.EndDisabledGroup();
                    pendingFunctionParameters[key] = EditorGUILayout.TextField(new GUIContent("", "Captured parameter value."), pendingFunctionParameters[key] ?? "", GUILayout.ExpandWidth(true));
                    oldColor = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button(new GUIContent("X", "Remove custom parameter."), GUILayout.Width(28)))
                    {
                        pendingFunctionParameters.Remove(key);
                        GUI.backgroundColor = oldColor;
                        EditorGUILayout.EndHorizontal();
                        break;
                    }
                    GUI.backgroundColor = oldColor;
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                var functionsByCategory = ConversationFunctionLibrary.GetFunctionsForCategory(selectedFunctionCategory);
                if (functionsByCategory.Length == 0)
                {
                    selectedFunctionName = "";
                    pendingFunctionParameters.Clear();
                    EditorGUILayout.HelpBox("No functions available in this category.", MessageType.Info);
                }
                else
                {
                    if (!functionsByCategory.Contains(selectedFunctionName))
                    {
                        selectedFunctionName = functionsByCategory[0];
                        SetupPendingParametersForFunction(selectedFunctionName);
                    }
                    int selectedIndex = System.Array.IndexOf(functionsByCategory, selectedFunctionName);
                    if (selectedIndex < 0) selectedIndex = 0;
                    int newFunctionIndex = EditorGUILayout.Popup(new GUIContent("Function", "Function name filtered by category."), selectedIndex, functionsByCategory, GUILayout.ExpandWidth(true));
                    if (newFunctionIndex != selectedIndex)
                    {
                        selectedFunctionName = functionsByCategory[newFunctionIndex];
                        SetupPendingParametersForFunction(selectedFunctionName);
                    }
                    var parameterDefinitions = ConversationFunctionLibrary.GetFunctionParameters(selectedFunctionName);
                    if (parameterDefinitions != null)
                    {
                        foreach (var parameterDefinition in parameterDefinitions)
                        {
                            if (!pendingFunctionParameters.ContainsKey(parameterDefinition.Key)) pendingFunctionParameters[parameterDefinition.Key] = "";
                            pendingFunctionParameters[parameterDefinition.Key] = EditorGUILayout.TextField(new GUIContent(parameterDefinition.Key, parameterDefinition.Value), pendingFunctionParameters[parameterDefinition.Key] ?? "", GUILayout.ExpandWidth(true));
                        }
                    }
                }
            }
            pendingFunctionTimestamp = EditorGUILayout.IntField(new GUIContent("Timestamp", "Execution order for the new function."), pendingFunctionTimestamp, GUILayout.ExpandWidth(true));
            if (GUILayout.Button(new GUIContent("Add", "Add function to this node."), GUILayout.ExpandWidth(true), GUILayout.Height(25)))
            {
                string methodName = isCustomCategory ? (customFunctionName ?? "").Trim() : selectedFunctionName;
                if (string.IsNullOrWhiteSpace(methodName))
                {
                    EditorUtility.DisplayDialog("Invalid Function", "Function name cannot be empty.", "OK");
                    return;
                }
                if (pendingFunctionParameters.Keys.Any(string.IsNullOrWhiteSpace))
                {
                    EditorUtility.DisplayDialog("Invalid Parameters", "Parameter names cannot be empty.", "OK");
                    return;
                }
                Undo.RecordObject(ownerWindow, "Add Function");
                var newFunction = new ConversationFunction
                {
                    MethodName = methodName,
                    Parameters = new Dictionary<string, string>(pendingFunctionParameters),
                    Timestamp = pendingFunctionTimestamp
                };
                functions.Add(newFunction);
                functionParameterFoldouts[newFunction] = false;
                if (isCustomCategory)
                {
                    customFunctionName = "";
                    pendingFunctionParameters.Clear();
                    pendingCustomParameterName = "";
                    pendingCustomParameterValue = "";
                }
                else
                {
                    SetupPendingParametersForFunction(selectedFunctionName);
                }
                pendingFunctionTimestamp = 0;
                MarkDirty();
            }
        }

        private void SetupPendingParametersForFunction(string functionName)
        {
            pendingFunctionParameters = new Dictionary<string, string>();
            var parameterDefinitions = ConversationFunctionLibrary.GetFunctionParameters(functionName);
            if (parameterDefinitions == null) return;
            foreach (var parameter in parameterDefinitions) pendingFunctionParameters[parameter.Key] = "";
        }

        private IEnumerable<string> GetFunctionParameterOrder(ConversationFunction function)
        {
            var predefinedParameters = ConversationFunctionLibrary.GetFunctionParameters(function.MethodName);
            if (predefinedParameters != null)
            {
                foreach (var key in predefinedParameters.Keys)
                {
                    if (!function.Parameters.ContainsKey(key)) function.Parameters[key] = "";
                }
                foreach (var key in function.Parameters.Keys.Where(k => !predefinedParameters.ContainsKey(k)).ToList()) function.Parameters.Remove(key);
                return predefinedParameters.Keys;
            }
            return function.Parameters.Keys.OrderBy(key => key).ToList();
        }

        private void DrawSectionSeparator()
        {
            EditorGUILayout.Space(4);
            Rect separatorRect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(separatorRect, new Color(0.35f, 0.35f, 0.35f, 1f));
            EditorGUILayout.Space(6);
        }

        private void MarkDirty()
        {
            OnDirty?.Invoke();
        }
        #endregion
    }
}