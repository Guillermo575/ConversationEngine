# ConversationEngine

ConversationEngine is a Unity-based toolkit for building, editing, and maintaining branching conversations using a visual node editor and a reusable runtime data schema.

## Project Structure

### `ConversationEngine` (Runtime/Data Schema)
This project defines the conversation data model used at runtime and by editor tools.
- Stores resources, actors, nodes, options, conditional branches, and function calls.
- Supports serialization workflows for external conversation files.
- Keeps editor-related layout settings persisted in conversation assets.

### `ConversationEngine.Editor` (Editor Tooling)
This project provides Unity Editor interfaces for authoring conversations visually.
- Includes graph-based node editing.
- Includes resource and inspector panels.
- Handles conversation asset creation, opening, editing, and saving workflows.

## User Guide

### 1) Create or open a conversation file
1. In Unity, create a new conversation asset from the ConversationEngine menu.
2. Open it to launch the conversation editor window.
3. If opening an existing file, verify resource references first.

### 2) Build conversation flow
1. Add nodes in the graph panel (Start, Dialogue, Function, Conditional, End).
2. Connect nodes to define progression.
3. Add options in dialogue nodes for branching.
4. Add conditional rules where dynamic flow is needed.

### 3) Configure resources and metadata
1. Add actors, backgrounds, and audio resources.
2. Set conversation title and description.
3. Validate references before saving.

### 4) Save and integrate
1. Save the conversation asset.
2. Consume the generated data from your gameplay/runtime systems.
3. Use function nodes and timed function calls to trigger game logic.

## Use Cases

- Visual novels and dialogue-heavy games.
- RPG NPC interactions with branching choices.
- Quest and mission dialogue trees.
- Tutorial flows with conditional progression.
- Event-driven narrative sequences linked to gameplay systems.

## Compatibility

- Unity project with `.NET Framework 4.7.1` target profile.
- Uses `Unity.Newtonsoft.Json` in both runtime and editor assemblies.
- Reusable structure for visual novels, RPG scenes, and other narrative systems.
