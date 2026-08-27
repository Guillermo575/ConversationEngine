# Conversation Editor - Visual Node-Based Editor

## Overview
The Conversation Editor is a powerful visual tool for creating and managing conversation files in Unity. It provides an intuitive node-based interface with a three-panel layout, making it easy to create complex branching dialogues, conditional logic, and interactive narratives.

## Interface Layout

The editor features a three-panel layout:
- **Left Panel (Resources)**: Manage scene backgrounds, audio, and actors
- **Center Panel (Graph)**: Visual node-based conversation flow editor
- **Right Panel (Inspector)**: Edit properties of selected nodes, shown only when a node is selected

All panels are resizable by dragging the splitter bars between them.

## File Management

### Supported File Types
- **.conversation**: New standardfile extension for conversation files
- **.json**: Legacy format, still fully supported

### File Operations
- **New** (Ctrl+N): Create a new conversation with Start and End nodes (not auto-linked)
- **Open** (Ctrl+O): Load existing .conversation or .json files
- **Save** (Ctrl+S): Save changes to current file
- **Save As**: Save to a new .conversation file

## Features

### Visual Node-Based Editing
- **Three-Panel Interface**: Resources (left) | Graph (center) | Inspector (right)
- **Resizable Panels**: Drag splitter bars to adjust panel widths
- **Multiple Node Types**: Support for Start, Dialogue, Function, Conditional, and End nodes
- **Smart Connections**: Create connections via right-click menu, not Ctrl+click
- **Color-Coded Nodes**: Each node type has a distinct color for easy identification
- **Visual Feedback**: Nodes have thick black borders that turn golden when selected, white while dragging
- **Resizable Nodes**: Adjust node size to fit content
- **Grid Background**: Aligned grid for precise node placement
- **Infinite Canvas**: Auto-expanding workspace as nodes are added

### Node Types

#### Start Node (Blue)
- Entry point of every conversation
- Automatically created if missing
- Cannot be deleted or duplicated
- Only one allowed per conversation
- **NextNodeId is editable** - can be cleared to 0
- Displays "START" text

#### End Node (Red)
- Terminal node marking conversation end
- Automatically created if missing
- Cannot be deleted or duplicated
- **NOT auto-linked** to other nodes
- Displays "END" text

#### Dialogue Node (Green)
- Standard dialogue with speaker and text
- Supports player options/choices
- Can have multiple branching paths
- Displays: ID, Actor, and text preview

#### Function Node (White)
- Executes functions without displaying text
- Invisible to players
- Used for background operations (audio, scene changes, etc.)
- Supports multiple function calls with timestamps

#### Conditional Node (Yellow Diamond)
- Branching logic based on conditions
- Supports TRUE/FALSE paths
- Multiple condition rules with AND/OR logic
- Fallback default branch

### Node Visual States

All nodes provide clear visual feedback through border colors:

- **Default State (Black Border)**: Normal node state when not selected
- **Selected State (Golden Border)**: Node is selected and inspector panel is showing its properties
- **Dragging State (White Border)**: Node is being moved with mouse drag
- **Border Thickness**: All borders are prominently visible (3x thicker than standard) for better visibility

The thick borders ensure clear visual distinction between states without obscuring node content.

### Connection System
- **Main Connection**: White line from node to next node
- **Option Connections**: Cyan lines from player options
- **Conditional Branches**: Green (TRUE) and Red (FALSE) lines
- **Thicker Lines**: Connection lines are now thicker (5px) for easier interaction
- **Interactive Creation**: Right-click on node > "Connect to Node" > click target node
- **Clear Connections**: Right-click on a connection line to clear it
- **Auto-routing**: Automatic connection cleanup when nodes are deleted

### Mouse Controls

#### Left Click
- **On Node**: Select node and show inspector panel (border turns golden)
- **On Empty Space**: Pan the canvas view (does not deselect node)
- **Drag on Node**: Move the node (border turns white while dragging)
- **Drag on Empty Space**: Pan the camera/canvas view
- **On Connection Line + Drag**: Move the nearest connected node

#### Right Click
- **On Node**: Show node context menu
- **On Empty Space**: Show creation and auto-layout menu
- **On Connection Line**: Show menu to clear connection

#### Middle Mouse / Alt+Left Click
- **Drag**: Pan the camera view

#### Mouse Wheel
- **Scroll**: Zoom in/out (10% - 500%) centered on the current pointer position

### Zoom Controls
- **Vertical Zoom Slider**: Available in the top-right corner of the graph panel
- **Range Validation**: Minimum `0.1x` and maximum `5.0x`
- **Persistent Zoom**: Current zoom value is saved in the conversation file and restored when reopened

### Keyboard Shortcuts
- **Ctrl+S**: Save conversation
- **Ctrl+N**: New conversation
- **Delete**: Delete selected node
- **F**: Frame/focus selected node
- **Escape**: Deselect node or cancel connection mode

### Node Inspector Panel

The inspector panel appears on the right when a node is selected and shows:

#### All Node Properties
- **ID**: Auto-generated, read-only unique identifier
- **Node Type**: Node type selector (read-only for Start/End)

#### Start Node Properties
- **Next Node**: Dropdown selector for connected node

#### Dialogue/Function Node Properties
- **Speaker Actor**: Dropdown of available actors
- **Text**: Multi-line text area for dialogue content
- **Next Node**: Dropdown selector (not manual ID entry)
- **Options** (Dialogue only): Player choice management
  - Add/remove options
  - Per-option text and Next Node dropdown
  - Condition rules for option visibility
- **Functions**: Timed function execution
  - Predefined function library
  - Custom function support
  - Parameter configuration
  - Timestamp for text synchronization
- **Editor Properties**: Position and size

#### Conditional Node Properties
- **Conditional Branches**: Multiple branch support
  - TRUE path - dropdown selector
  - FALSE path - dropdown selector
  - Condition rule list per branch
- **Default Branch Node**: Fallback dropdown selector

### Next Node Dropdown

All Next Node ID fields now use dropdowns instead of manual text entry:
- **First Option**: "NINGUNO" (represents ID 0, no connection)
- **Format**: `ID - ActorId - TextPreview [NodeType]`
- **Example**: `5 - actor_knight - Greetings, traveler... [END]`
- **Valid Targets**: Excludes Start nodes and the current node itself

### Resource Management

#### Scene Backgrounds
- Manage background images and scenes
- ID and Path-based reference system

#### Audio Backgrounds
- Support for multiple audio types:
  - Background Music
  - Sound Effects
  - Voice
- Per-resource audio type configuration

#### Actors
- Actor definition via JSON files
- Icon paths for visual representation
- Sound effect associations
- Body part composition system (for complex character assembly)

### Auto-Layout

The editor includes intelligent auto-layout functionality accessible from:
- **Toolbar Button**: "Auto-Layout" button shows menu with Horizontal/Vertical options
- **Right-Click Menu**: Available when right-clicking on empty canvas space

#### Horizontal Layout
- Start node positioned at center-left
- Nodes flow from left to right following connections
- Branch nodes (options/conditionals) arrange vertically
- Maintains consistent horizontal alignment for clarity
- Preserves vertical spacing for branching paths

#### Vertical Layout
- Start node positioned at top-center
- Nodes flow from top to bottom following connections
- Branch nodes (options/conditionals) arrange horizontally
- Maintains consistent vertical alignment for clarity
- Preserves horizontal spacing for branching paths

Both layouts intelligently handle:
- Multiple dialogue options creating branches
- Conditional true/false paths
- Default branch fallbacks
- Complex conversation trees with multiple levels

## Workflow Best Practices

1. **Start with Resources**: Define your actors and assets first in the left panel
2. **Build the Flow**: Create nodes in the graph panel (center)
3. **Connect Nodes**: Right-click on source node > "Connect to Node" > click target
4. **Configure Details**: Select nodes to edit properties in the inspector (right)
5. **Test Flow**: Follow connections visually to verify conversation logic
6. **Save Often**: Use Ctrl+S frequently to save your work

## Tips and Tricks

- Use the **F key** to quickly center view on selected node
- **Frame important nodes** by selecting them and pressing F
- **Persistent Selection**: Nodes stay selected when panning the view, allowing easier multi-step editing
- **Press Escape** to deselect the current node
- **Visual Feedback**: Golden border = selected, White border = dragging, Black border = default
- **Auto-Layout Options**: Use horizontal layout for left-to-right flow, vertical for top-to-bottom
- **Pan with empty space drag** to navigate large conversations easily
- **Use the grid** as a visual guide for alignment
- **Color coordination**: Match your node colors to your mental model of the conversation flow

## Troubleshooting

### Nodes appear outside visible area
- Press F with node selected to frame it
- Pan the view by dragging on empty space
- Check node's Editor Position in inspector

### Can't connect nodes
- Ensure you're using right-click > "Connect to Node", not Ctrl+click
- Press Escape if stuck in connection mode
- Verify target node is valid (not a Start node)

### Inspector panel won't show
- Click on a node to select it
- Check that you're not clicking on empty space immediately after

### Changes not saving
- File name shows asterisk (*) when dirty
- Use Ctrl+S or click Save button
- Check file permissions if save fails

## Technical Notes

### Node ID System
- IDs are auto-generated starting from 1
- Start node typically has ID 1
- End node typically has ID 2
- IDs are permanent once assigned
- Deleting a node removes all references to that ID

### Connection Logic
- Nodes can have multiple outgoing connections (via Options or Conditional Branches)
- A node with no explicit next connection and no options/branches will not auto-link
- Orphaned nodes (no incoming connections) are valid but may be unreachable in gameplay

### File Format
- JSON-based structure
- Human-readable and VCS-friendly
- Contains `ResourceManager`, `ConversationManager`, and `EditorSettings`
- Editor properties (Position, Size) are stored in the file
- `EditorSettings.Zoom` stores the graph zoom used in the editor

## Version History

### Latest Version
- Three-panel resizable layout
- Support for .conversation file extension
- Next Node dropdown system with formatted options
- Improved mouse controls (camera panning, node selection)
- Thicker connection lines for easier interaction
- Right-click connection creation and clearing
- Start node NextNodeId is now editable
- No auto-linking between Start and End nodes
- Inspector panel hides when clicking empty space
- Escape key cancels connection mode
- Better canvas handling without artificial bounds
- Zoom interaction reworked to scale graph elements with consistent hit detection
- Vertical zoom slider at top-right (`0.1x` to `5.0x`) with persisted zoom per file

### Previous Features
- Visual node-based editing
- Multiple node types
- Resource management
- Undo/Redo support
- Predefined function library
- **Default Branch**: Fallback node when no conditions match
- **Condition Rules**:
  - Variable name
  - Comparison operator (==, !=, >, >=, <, <=)
  - Value type (String, Integer, Decimal, Boolean)
  - Value (literal or variable reference)

#### Function Node Properties
- Same as Dialogue node but primarily focused on function list
- No text display to players

### Predefined Function Library

The editor includes a comprehensive library of common functions organized by category:

#### Background & Scene
- `SetBackground`: Change scene background
- `FadeBackground`: Smooth background transition
- `ClearBackground`: Remove current background

#### Audio
- `PlayAudio`: Play any audio resource
- `StopAudio`: Stop audio playback
- `FadeAudio`: Fade audio volume
- `PlayBackgroundMusic`: Play looping music
- `PlaySoundEffect`: Play one-shot sound

#### Actor Management
- `ShowActor`: Display actor on screen
- `HideActor`: Remove actor from screen
- `MoveActor`: Animate actor movement
- `SetActorScale`: Resize actor
- `SetActorExpression`: Change facial expression
- `PlayActorAnimation`: Trigger actor animation

#### Body Parts (Modular Characters)
- `SetBodyPart`: Change body part resource
- `ShowBodyPart`: Make body part visible
- `HideBodyPart`: Hide specific body part

#### Camera Control
- `CameraShake`: Screen shake effect
- `CameraZoom`: Camera zoom in/out
- `CameraFocus`: Focus on specific target

#### Variables
- `SetVariable`: Assign variable value
- `IncrementVariable`: Increase numeric variable
- `DecrementVariable`: Decrease numeric variable

#### UI & Effects
- `ShowTextBox`: Display text container
- `HideTextBox`: Hide text container
- `ShowChoiceBox`: Display choice menu
- `PlayScreenEffect`: Trigger screen effects (flash, blur, etc.)
- `FadeToBlack`: Fade screen to black
- `FadeFromBlack`: Fade in from black
- `SetTextSpeed`: Adjust text display speed
- `SetTextColor`: Change text color
- `ShowNameTag`: Display character name
- `HideNameTag`: Hide character name

#### Game Flow
- `Wait`: Pause for duration
- `LoadScene`: Load new Unity scene
- `SaveCheckpoint`: Create save point
- `TriggerEvent`: Fire custom game event
- `EndConversation`: Force conversation end

#### Effects
- `PlayParticleEffect`: Spawn particle system
- `StopParticleEffect`: Stop particle system

#### Progression
- `StartQuest`: Begin new quest
- `CompleteQuest`: Mark quest complete
- `UnlockAchievement`: Award achievement
- `AddItem`: Add item to inventory
- `RemoveItem`: Remove item from inventory

**Custom Functions**: Select "Custom" from dropdown to define your own function with free-form parameters.

## User Interface

### Top Toolbar
- **New**: Create new conversation file
- **Open**: Open existing conversation file
- **Save**: Save current conversation (Ctrl+S)
- **Save As**: Save with new filename
- **Current File**: Displays active filename

### Tab System
- **Resources Tab**: Manage backgrounds, audio, and actors
- **Conversation Graph Tab**: Visual node editor

### Navigation Controls
- **Pan View**: Middle mouse button or Alt+Left mouse drag
- **Zoom**: Mouse wheel or vertical zoom slider (top-right)
- **Frame Selection**: Press F to center on selected node
- **Grid**: Aligned background grid for precision

### Context Menus

#### Graph Context Menu (Right-click empty space)
- Create Node ? Dialogue
- Create Node ? Function
- Create Node ? Dialogue with Options (creates node with 2 default options)
- Create Node ? Conditional

#### Node Context Menu (Right-click on node)
- Add Option (Dialogue nodes only)
- Duplicate Node
- Delete Node

#### Option Context Menu (Right-click on option)
- Duplicate Option
- Delete Option

## Keyboard Shortcuts
- **Ctrl+S**: Save conversation
- **Ctrl+N**: New conversation
- **Ctrl+Z**: Undo
- **Ctrl+Y**: Redo
- **Delete**: Delete selected node
- **F**: Frame selected node

## Workflow

### Creating a New Conversation

1. **Create File**:
   - Right-click in Project window
   - Navigate to: Create ? ConversationEngine ? Conversation File
   - File is created with Start and End nodes already set up

2. **Open Editor**:
   - Double-click the conversation JSON file
   - The Conversation Editor window opens automatically

3. **Add Resources**:
   - Switch to Resources tab
   - Add backgrounds, audio, and actors
   - Provide IDs and file paths

4. **Build Conversation**:
   - Switch to Conversation Graph tab
   - Right-click to create nodes
   - Connect nodes from context menu: right-click source node > "Connect to Node" > click target
   - Configure node properties in Inspector

5. **Save**:
   - Press Ctrl+S or click Save button
   - Connections are saved exactly as configured (no automatic End linking)

### Creating Branching Dialogue

1. Create a Dialogue node
2. In Inspector, click "Add Option" multiple times
3. Configure option text and target nodes
4. Connect option nodes visually or set Next Node ID manually
5. Optionally add conditions to options for dynamic availability

### Using Conditional Logic

1. Create a Conditional node
2. In Inspector, click "Add Branch"
3. Add condition rules to the branch
4. Set TRUE and FALSE path node IDs
5. Set Default Branch Node ID as fallback
6. Connect visually using green (TRUE) and red (FALSE) connectors

### Adding Timed Functions

1. Select any node (Dialogue or Function)
2. In Inspector, click "Add Function"
3. Choose function from dropdown
4. Configure parameters based on function requirements
5. Set Timestamp (character index) for execution timing
   - 0 = Execute immediately when node starts
   - > 0 = Execute when text reaches that character

## File Format

Conversations are saved as JSON files with the following structure:
- **ResourceManager**: Lists of backgrounds, audio, and actors
- **ConversationManager**: List of conversation nodes
- Each node contains:
  - Unique ID (integer)
  - Node type (Start, Dialogue, Conditional, Function, End)
  - Properties (speaker, text, connections, etc.)
  - Editor metadata (position, size)

## Best Practices

### Node Organization
- **Horizontal Flow**: Arrange nodes left-to-right for story progression
- **Vertical Spacing**: Use vertical space for branching options
- **Grouping**: Keep related nodes close together
- **Naming**: Use clear, descriptive text for easy identification

### Performance
- **Reasonable Graph Size**: Keep conversations under 100 nodes when possible
- **Split Long Conversations**: Break very long stories into multiple files
- **Lazy Loading**: Load actors/resources only when needed

### Reusability
- **Shared Resources**: Create a common resource pool for multiple conversations
- **Modular Actors**: Define actors in separate JSON files
- **Function Abstraction**: Use functions for reusable logic

### Testing
- **End Node Connections**: Ensure all branches eventually reach End node
- **Condition Testing**: Test all conditional paths
- **Option Availability**: Verify options appear with correct conditions
- **Function Timing**: Check that timed functions execute at correct points

## Integration with ConversationEngine

The Conversation Editor generates files that will be consumed by the ConversationEngine runtime (to be developed separately). The engine will:
- Load conversation JSON files
- Instantiate resources (backgrounds, audio, actors)
- Execute conversation flow based on node connections
- Evaluate conditions and branching logic
- Call functions at specified timestamps
- Handle player input for options

## Troubleshooting

### Nodes Not Connecting
- Ensure you are using right-click on source node > "Connect to Node"
- Check that source and target are valid node types
- Verify target node is not a `Start` node

### Functions Not Appearing
- Check spelling of function names
- For custom functions, ensure "Custom" is selected first
- Verify parameter dictionary is properly formatted

### Save Failed
- Check file permissions
- Ensure valid file path
- Look for JSON serialization errors in Console

### Asset Not Recognized
- Ensure file has .json extension
- Verify JSON structure matches ConversationData format
- Check for syntax errors in JSON

## Future Enhancements

Planned features for future versions:
- **Actor Editor**: Visual editor for actor body parts and resources
- **Search/Filter**: Find nodes by text or ID
- **Minimap**: Overview navigation for large graphs
- **Comments**: Add annotation nodes
- **Validation**: Real-time error checking
- **Templates**: Pre-built conversation patterns
- **Localization**: Multi-language support
- **Version Control**: Better diff/merge support

## Technical Details

### Dependencies
- **Newtonsoft.Json**: For JSON serialization
- **UnityEditor**: Editor-only functionality
- **ConversationScheme**: Core data models

### File Structure
```
Assets/ConversationEngine/
??? ConversationScheme/
?   ??? ConversationSchemeModels.cs
?   ??? ConversationNodeUtility.cs
??? Editor/
?   ??? ConversationEditorWindow.cs
?   ??? ConversationFunctionLibrary.cs
?   ??? ConversationAssetImporter.cs
?   ??? ConversationMenuItems.cs
??? Examples/
?   ??? conversation_intro.json
?   ??? Actors/
?       ??? actor_knight.json
?       ??? royal_messenger.json
??? README.md
```

## Support

For issues, questions, or contributions:
- Check existing conversation files in Examples folder
- Review ConversationScheme documentation (README.md)
- Test with provided example conversations

---

**Version**: 1.0  
**Compatibility**: Unity 2019.4+  
**C# Target**: .NET Framework 4.7.1  
**License**: [Your License Here]
