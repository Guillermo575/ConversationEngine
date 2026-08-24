# ConversationEngine - ConversationScheme

## Purpose
`ConversationScheme` defines the external data format used by the future `ConversationEngine`.

This stage only creates the schema and example files. Runtime logic (load, save, rendering, flow execution, function execution) will be implemented later.

## Main Structure
- `ConversationData`
  - `ResourceManager`: references to scenery, audio and actors.
  - `ConversationManager`: conversation nodes and branching flow.

## Resource System
- `Resource` (base class)
  - `Id`: unique identifier.
  - `Path`: asset path or external file path.
  - `gameObject`: runtime instance reference. Marked to be ignored by XML serialization.
- Child classes:
  - `SceneBackground`
  - `AudioBackground` (`BackgroundMusic`, `SoundEffect`, `Voice`)
  - `Actor`

## Actor Composition
`Actor` supports simple and complex character composition:
- `SoundEffectPaths`: actor-specific sound references.
- `BodyParts`: modular visual composition.

`BodyPart` includes:
- `Id`: part identifier (`body`, `head`, `eyes`, `mouth`, `main`, etc.).
- `AttachToPivotId`: pivot of parent part where this part is attached.
- `NestedResources`: list of possible resources/assets for that part.
- `CurrentResourceId`: active resource for this part.
- `PivotPoints`: local anchors where other parts can be attached.

`PivotPoint` stores local coordinates (`X`, `Y`) where `(0,0)` is parent center.

## Conversation Flow
- `ConversationManager` contains ordered `Nodes`.
- `ConversationNode` supports:
  - `SpeakerActorId` (empty for narrator)
  - dialogue `Text`
  - `ReferenceFlag`
  - `NextNodeId` / `NextReferenceFlag`
  - player `Options`
  - timed `Functions`
  - `ConditionalBranches` for non-linear routes

## Function Calls
`ConversationFunction` is metadata for runtime execution:
- `MethodName`
- `Parameters` (`Dictionary<string, string>`)
- `Timestamp` (character index of dialogue text, default `0`)

## Conditional Nodes
Use `NodeType = Conditional` with:
- `ConditionalBranches`: list of condition routes.
- `DefaultBranchNodeId` / `DefaultBranchReferenceFlag`: fallback route.

## Example Files Included
- `Examples/conversation_intro.json`
- `Examples/conversation_intro.xml`
- `Examples/Actors/royal_messenger.json`

These examples include:
- Resources (background, audio, actors)
- One actor defined inline
- One actor loaded from external JSON path
- Branched story nodes with options and reference flags

## Expected Benefits
- Flexible actor representation from static portraits to modular animated assets.
- Editable external files (`JSON` / `XML`) with standard text tools.
- Separation between data schema (this stage) and runtime behavior (`ConversationEngine`, next stage).
- Reusable structure for visual novels, RPG scenes, and other narrative systems.
