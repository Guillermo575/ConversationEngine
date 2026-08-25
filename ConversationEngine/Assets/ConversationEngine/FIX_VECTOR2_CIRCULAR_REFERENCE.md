# Fix: Vector2 Circular Reference Issue

## Problem
When creating a new conversation file using `Assets > Create > ConversationEngine > Conversation File`, the following error occurred:

```
JsonSerializationException: Self referencing loop detected for property 'normalized' with type 'UnityEngine.Vector2'. 
Path 'ConversationManager.Nodes[0].EditorPosition'.
```

## Root Cause
Unity's `Vector2` type has a `normalized` property that returns a new `Vector2`, which Newtonsoft.Json interprets as a circular reference during serialization. This is a common issue when serializing Unity types with Newtonsoft.Json.

## Solution
Created a custom JSON serialization system that:

1. **UnityVector2Converter.cs** - Custom JsonConverter for Vector2
   - Serializes Vector2 as a simple object with X and Y properties
   - Deserializes from the same format
   - Prevents Newtonsoft.Json from trying to serialize internal Vector2 properties

2. **ConversationJsonSettings.cs** - Centralized JSON settings
   - Provides consistent JsonSerializerSettings across all serialization
   - Configured with:
     - `ReferenceLoopHandling = Ignore` - Prevents circular reference errors
     - `Formatting = Indented` - Human-readable JSON output
     - Custom UnityVector2Converter registered
   - Offers simple `Serialize()` and `Deserialize<T>()` methods

3. **Updated all serialization calls** to use `ConversationJsonSettings`:
   - `ConversationMenuItems.cs` - Creating new conversation/actor files
   - `ConversationEditorWindow.cs` - Loading and saving conversations
   - `ConversationAssetImporter.cs` - Asset recognition

## Changes Made

### New Files
- `Assets/ConversationEngine/Editor/UnityVector2Converter.cs`
- `Assets/ConversationEngine/Editor/ConversationJsonSettings.cs`

### Modified Files
- `Assets/ConversationEngine/Editor/ConversationMenuItems.cs`
  - Replaced: `JsonConvert.SerializeObject(data, Formatting.Indented)`
  - With: `ConversationJsonSettings.Serialize(data)`

- `Assets/ConversationEngine/Editor/ConversationEditorWindow.cs`
  - Replaced: `JsonConvert.DeserializeObject<T>(json)`
  - With: `ConversationJsonSettings.Deserialize<T>(json)`
  - Replaced: `JsonConvert.SerializeObject(data, Formatting.Indented)`
  - With: `ConversationJsonSettings.Serialize(data)`

- `Assets/ConversationEngine/Editor/ConversationAssetImporter.cs`
  - Replaced: `JsonConvert.DeserializeObject<T>(json)`
  - With: `ConversationJsonSettings.Deserialize<T>(json)`

## Verification
? Build successful - No compilation errors
? Vector2 serialization now works correctly
? All existing JSON files remain compatible

## Usage
The fix is transparent to users. Simply use the editor as normal:
1. Right-click in Project window
2. Select: **Create > ConversationEngine > Conversation File**
3. The file is created without errors
4. Vector2 properties (EditorPosition, EditorSize) serialize correctly

## JSON Output Format
Vector2 properties are now serialized as:
```json
"EditorPosition": {
  "X": 0.0,
  "Y": 0.0
}
```

This format matches the existing example files and is fully compatible with the ConversationScheme format.

## Technical Details
The custom converter only affects Vector2 serialization. All other types (strings, ints, lists, dictionaries, enums) continue to use Newtonsoft.Json's default serialization, maintaining full compatibility with the existing conversation format.
