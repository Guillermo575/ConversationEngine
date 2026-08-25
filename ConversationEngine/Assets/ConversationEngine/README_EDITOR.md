# Conversation Editor - Visual Node-Based Editor

## Overview
The Conversation Editor is a powerful visual tool for creating and managing conversation files in Unity. It provides an intuitive node-based interface similar to Unity's Animator window, making it easy to create complex branching dialogues, conditional logic, and interactive narratives.

## Features

### Visual Node-Based Editing
- **Animator-like Interface**: Familiar visual graph editor with drag-and-drop functionality
- **Multiple Node Types**: Support for Start, Dialogue, Function, Conditional, and End nodes
- **Visual Connections**: Draw connections between nodes by Ctrl+click dragging
- **Color-Coded Nodes**: Each node type has a distinct color for easy identification
- **Resizable Nodes**: Adjust node size to fit content
- **Grid Background**: Aligned grid for precise node placement

### Node Types

#### Start Node (Blue)
- Entry point of every conversation
- Automatically created if missing
- Cannot be deleted or duplicated
- Only one allowed per conversation
- Displays "START" text

#### End Node (Red)
- Terminal node marking conversation end
- Automatically created if missing
- Cannot be deleted or duplicated
- Auto-linked when nodes have no next connection
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

### Connection System
- **Main Connection**: White line from node to next node
- **Option Connections**: Cyan lines from player options
- **Conditional Branches**: Green (TRUE) and Red (FALSE) lines
- **Interactive Creation**: Ctrl+click drag from source to target
- **Auto-routing**: Automatic connection cleanup when nodes are deleted

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

### Node Inspector Panel
Located on the right side, the inspector shows detailed properties for the selected node:

#### Dialogue Node Properties
- **ID**: Auto-generated, read-only unique identifier
- **Node Type**: Node type selector (read-only for Start/End)
- **Speaker Actor**: Dropdown of available actors
- **Text**: Multi-line text area for dialogue content
- **Next Node ID**: Manual node connection option
- **Options**: Player choice management
  - Add/remove options
  - Per-option text and target node
  - Condition rules for option visibility
- **Functions**: Timed function execution
  - Predefined function library
  - Custom function support
  - Parameter configuration
  - Timestamp for text synchronization
- **Editor Properties**: Position and size

#### Conditional Node Properties
- **Conditional Branches**: Multiple branch support
  - TRUE path node ID
  - FALSE path node ID
  - Condition rule list per branch
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
- **Zoom**: Mouse wheel or zoom slider (bottom-left)
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
   - Connect nodes by Ctrl+click dragging
   - Configure node properties in Inspector

5. **Save**:
   - Press Ctrl+S or click Save button
   - Empty Next Node IDs automatically connect to End node

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
- Ensure you're Ctrl+clicking when dragging
- Check that source and target are valid node types
- Verify you're not connecting incompatible elements

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
