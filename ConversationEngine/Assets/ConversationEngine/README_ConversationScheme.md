# ConversationEngine (Runtime/Data Schema)

ConversationEngine runtime schema provides the core models and utilities required to represent branching conversations, resources, and execution metadata in a structured and reusable format.

## Project Files

### `ConversationEngine.asmdef`
Defines the runtime assembly configuration.
- Assembly name: `ConversationEngine`.
- Root namespace: `ConversationScheme`.
- References `Unity.Newtonsoft.Json`.

### `ConversationScheme/ConversationSchemeModels.cs`
Defines core data structures used by conversations.
- Main containers for conversation metadata and graph content.
- Node models for Start, Dialogue, Function, Conditional, and End flows.
- Option and branching models for player choices and conditional routes.
- Resource models for backgrounds, audio, and actor definitions.
- Editor persistence fields for graph position, zoom, and panel state.

### `ConversationScheme/ConversationNodeUtility.cs`
Provides utility logic for node-related operations.
- Node ID support and validation helpers.
- Common graph-safe operations used by editor/runtime integrations.
- Shared helper methods for conversation node consistency.

### `Examples/*`
Sample assets for reference and onboarding.
- Example conversation files.
- Example actor/resource definitions.
- Demonstrates expected schema usage patterns.

## Conclusion

`ConversationEngine` is the data foundation of the whole toolkit. It enables predictable serialization, reusable narrative structures, and clean integration points for gameplay systems that consume conversation flow and branching logic.