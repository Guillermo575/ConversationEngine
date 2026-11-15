# ConversationEngine.Editor (Visual Authoring Tools)

ConversationEngine.Editor is the Unity Editor module that provides a visual workflow for creating, editing, and validating conversation graphs built on top of the runtime schema.

## Project Files

### `Editor/ConversationEngine.Editor.asmdef`
Defines the editor assembly configuration.
- Assembly name: `ConversationEngine.Editor`.
- Root namespace: `ConversationEditor`.
- References `ConversationEngine` and `Unity.Newtonsoft.Json`.
- Limited to Unity `Editor` platform.

### `Editor/ConversationEditorWindow.cs`
Main editor window entry point.
- Hosts the conversation editing workspace.
- Handles layout composition and main interaction loop.
- Connects graph, inspector, and resource panels.

### `Editor/ConversationEditorCore.cs`
Coordinates editor state and shared workflows.
- Manages current conversation context.
- Handles orchestration between UI components and data changes.
- Centralizes reusable editor logic.

### `Editor/Panel/EditorInspector.cs`
Inspector-side editing logic for selected elements.
- Shows and edits node properties.
- Manages option, branch, and function parameter editing.
- Applies validation constraints for safe graph data editing.

### `Editor/Setup/ConversationEditorSetup.cs`
Editor initialization and setup utilities.
- Registers setup routines needed by editor tooling.
- Ensures editor-side systems are prepared correctly.

### `Editor/Setup/ConversationMenuItems.cs`
Unity menu integration for ConversationEngine.
- Adds menu commands to create/open conversation-related assets.
- Provides editor actions accessible from Unity menus.

### `Editor/ConversationFunctionLibrary.cs`
Catalog of available conversation function definitions.
- Supplies function names/categories for editor selection.
- Supports parameter-driven function authoring in nodes.

### `Editor/ConversationAssetImporter.cs`
Asset import and file recognition support.
- Handles conversation-related file integration in the Unity project.
- Improves editor-side asset workflow and discovery.

## User Guide: What You Can Do in the Editor Window

This guide summarizes the most important actions available in the Conversation Editor window.

### 1) Manage the workspace panels
- **Resources panel (left)**: define backgrounds, audio, actors, title, and description.
- **Graph panel (center)**: create and connect nodes to build conversation flow.
- **Inspector panel (right)**: edit the selected node, option, or branch properties.
- Drag splitters to resize panels.
- Use **Hide** / **Show properties** to collapse or restore the Resources panel.

### 2) Build conversation flow visually
- Right click on empty graph space to create nodes:
  - Dialogue
  - Function
  - Dialogue with Options
  - Conditional
- Right click a node to open node actions.
- Connect nodes from source to target using the context menu flow.
- Use **Horizontal** or **Vertical** auto-layout to reorganize large graphs.

### 3) Edit node behavior
- **Dialogue nodes**: set speaker, text, options, and timed functions.
- **Function nodes**: execute background logic without showing dialogue text.
- **Conditional nodes**: configure TRUE/FALSE branches and default fallback.
- **Start/End nodes**: control entry and termination of conversation flow.

### 4) Navigate efficiently
- **Left click** node: select and open inspector.
- **Left click + drag** node: move node.
- **Drag empty space** or **Middle mouse drag**: pan the canvas.
- **Mouse wheel**: zoom in/out around pointer.
- Use the zoom slider (top-right) for precise zoom control.

### 5) Use keyboard shortcuts
- **Ctrl+S**: Save current conversation
- **Ctrl+N**: Create new conversation
- **Ctrl+Z**: Undo
- **Ctrl+Y**: Redo
- **Delete**: Delete selected node
- **F**: Frame/focus selected node
- **Escape**: Deselect node or cancel connection mode

### 6) Configure branching and options
- Add multiple options in Dialogue nodes.
- Set per-option target node from dropdown.
- Add condition rules to control option visibility.
- For Conditional nodes, define TRUE/FALSE targets and a default path.

### 7) Trigger gameplay logic with functions
- Add one or more functions in Dialogue or Function nodes.
- Select predefined function entries or use custom functions.
- Define parameters in key-value format.
- Set timestamp to control when each function executes in text flow.

### 8) Save-safe workflow
1. Define resources first.
2. Build graph structure.
3. Configure node details in inspector.
4. Validate branches and targets.
5. Save frequently with **Ctrl+S**.

### 9) Common quick checks
- If inspector is not visible, select a node.
- If connection fails, retry from node context menu.
- If a node is lost, select it and press **F**.
- If graph feels crowded, apply auto-layout and then fine-tune manually.

## Conclusion

`ConversationEngine.Editor` delivers the practical authoring experience of the toolkit. It turns complex branching dialogue structures into an efficient visual workflow, reducing implementation friction and improving consistency between narrative design and runtime data.
