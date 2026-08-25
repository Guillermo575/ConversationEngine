# Conversation Engine - Changes Summary

## Overview
This document summarizes all changes made to the ConversationScheme system to prepare it for visual node-based editing.

## Changes to ConversationSchemeModels.cs

### 1. New Node Types
Added three new `ConversationNodeType` enum values:
- **Start**: Entry point node (only one allowed per conversation)
- **Function**: Invisible node that executes functions without displaying text
- **End**: Terminal node marking the end of a conversation branch

The complete enum now includes:
```csharp
public enum ConversationNodeType
{
    Start,
    Dialogue,
    Conditional,
    Function,
    End
}
```

### 2. Node ID System Changes
- Changed `ConversationNode.Id` from `string` to `int`
- IDs start from 1 and auto-increment
- Changed `ConversationOption.NextNodeId` from `string` to `int`
- **Removed fields**: `ReferenceFlag`, `NextReferenceFlag`, `TargetReferenceFlag`

### 3. Actor Class Updates
- Added `IconPath` property for node editor visualization
- Used to display actor icons in the visual editor

### 4. BodyPartResource Enhancement
- Added `List<PivotPoint> PivotPoints` property
- Allows resource-specific pivot point overrides
- Enables precise positioning for different poses/animations
- If a pivot ID matches the parent BodyPart's pivot, the resource-specific one takes precedence

### 5. ConversationNode Enhancements
- Added `Vector2 EditorPosition` for visual editor node placement
- Added `Vector2 EditorSize` for visual editor node dimensions (default: 200x100)
- Changed `NextNodeId` to `int`
- Changed `DefaultBranchNodeId` to `int`

### 6. ConversationOption Changes
- Renamed `TargetNodeId` to `NextNodeId` (consistency with ConversationNode)
- Changed type from `string` to `int`
- Removed `TargetReferenceFlag`

### 7. ConditionalBranch Updates
- **Removed**: `TargetNodeId` and `TargetReferenceFlag`
- **Added**: `NextNodeIdTrue` (int) - node to navigate when conditions are true
- **Added**: `NextNodeIdFalse` (int) - node to navigate when conditions are false

### 8. ConditionRule Enhancements
- Added `ValueDataType` property of type `ValueType` enum
- Added `IsValueVariable` boolean property:
  - `false` (default): `Value` is treated as a literal value
  - `true`: `Value` is treated as a variable name to lookup

New `ValueType` enum:
```csharp
public enum ValueType
{
    String,
    Integer,
    Decimal,
    Boolean
}
```

## New Utility Class: ConversationNodeUtility

Created `ConversationNodeUtility.cs` with helper methods for node management:

### Methods:
1. **GetNextAvailableId(nodes)**: Finds the next available node ID
   - Starts from highest ID + 1
   - Handles integer overflow by restarting from 1
   - Ensures uniqueness

2. **IsIdUnique(nodeId, nodes, excludeNodeId)**: Validates ID uniqueness
   - Useful when editing existing nodes
   - Can exclude a specific node from the check

3. **RemoveNodeReferences(deletedNodeId, nodes)**: Cleans up references
   - Removes references from NextNodeId
   - Removes references from DefaultBranchNodeId
   - Removes references from Options
   - Removes references from ConditionalBranches

4. **EnsureStartNodeExists(conversationData)**: Validates Start node
   - Creates a Start node if missing
   - Auto-connects to first available node
   - Places at position (0, 0)

5. **ValidateStartNode(nodes)**: Ensures only one Start node exists

6. **GetNodeReferences(targetNodeId, nodes)**: Finds all nodes referencing a target
   - Returns list of node IDs that reference the target
   - Useful for dependency tracking

## Updated Example Files

### conversation_intro.json & conversation_intro.xml
- Updated with new node structure
- Includes Start node (ID: 1) at position (0, 0)
- Function node (ID: 2) for setup functions
- Multiple Dialogue nodes with proper integer IDs
- Two End nodes (IDs: 12, 13) for different endings
- All nodes have EditorPosition and EditorSize
- Removed all ReferenceFlag fields
- All NextNodeId values are integers

### actor_knight.json & royal_messenger.json
- Added `IconPath` field pointing to actor icon images
- BodyPartResource entries now include `PivotPoints` arrays (empty where not needed)

## Node Type Behavior

### Start Node
- Must be unique (only one per conversation)
- Auto-created at (0, 0) if missing when opening editor
- NextNodeId points to first conversation node
- Cannot have SpeakerActorId or Text (unused)

### Dialogue Node
- Standard conversation node
- Can have SpeakerActorId, Text, Options
- Supports Functions for timed events
- NextNodeId for linear progression

### Conditional Node
- Evaluates ConditionalBranches
- Uses NextNodeIdTrue and NextNodeIdFalse for branching
- DefaultBranchNodeId as fallback
- No direct NextNodeId (handled by branches)

### Function Node
- "Invisible" to the player
- Text and SpeakerActorId are ignored by engine
- Only executes Functions
- Useful for setup, transitions, effects
- Cannot be edited for text/speaker in visual editor

### End Node
- Terminal node
- Marks conversation conclusion
- Can have Text for final messages ("THE END", "TO BE CONTINUED", etc.)
- NextNodeId typically 0 (no next node)

## Benefits of These Changes

1. **Simplified Node Management**: Integer IDs are easier to work with than strings
2. **Automatic ID Generation**: No manual ID creation needed
3. **Reference Tracking**: Easy to find and remove orphaned references
4. **Visual Editor Ready**: EditorPosition and EditorSize support node-based editing
5. **Type Safety**: ValueType enum ensures proper condition evaluation
6. **Flexible Branching**: ConditionalBranch with separate True/False paths
7. **Actor Visualization**: IconPath enables actor icons in editor
8. **Precise Animation**: BodyPartResource PivotPoints allow pose-specific positioning
9. **Start Node Validation**: Automatic creation ensures valid conversation flow

## Migration Notes

When migrating old conversation files:
1. Convert string IDs to integers (starting from 1)
2. Remove ReferenceFlag, NextReferenceFlag, TargetReferenceFlag fields
3. Rename TargetNodeId to NextNodeId in Options
4. Split ConditionalBranch TargetNodeId into NextNodeIdTrue/NextNodeIdFalse
5. Add IconPath to Actor definitions
6. Add PivotPoints arrays to BodyPartResource entries
7. Add EditorPosition and EditorSize to all nodes
8. Add a Start node (NodeType = Start) at the beginning
9. Convert final nodes to End type
10. Update function-only nodes to Function type

## Next Steps

The visual node editor will use:
- EditorPosition and EditorSize for node placement
- ConversationNodeUtility for ID management
- Start node as the entry point
- End nodes as terminal points
- Function nodes for invisible setup operations
- IconPath for displaying actor images in nodes
