# Installation Instructions

## Required Package: Newtonsoft.Json

The Conversation Editor requires the Newtonsoft.Json package to serialize and deserialize conversation files.

### Installation Steps:

#### Option 1: Via Package Manager (Recommended)
1. Open Unity
2. Go to **Window > Package Manager**
3. Click the **+** button in the top-left corner
4. Select **Add package by name...**
5. Enter: `com.unity.nuget.newtonsoft-json`
6. Click **Add**

#### Option 2: Manual manifest.json Edit
The package has already been added to your `Packages/manifest.json` file with this line:
```json
"com.unity.nuget.newtonsoft-json": "3.2.1"
```

If Unity doesn't automatically download it:
1. Close Unity
2. Delete the `Library` folder (this will force Unity to reimport everything)
3. Reopen Unity
4. Wait for Unity to download and import the package

#### Option 3: Alternative JSON Serialization
If you cannot install Newtonsoft.Json, you can use the provided `SimpleJsonSerializer.cs` as a fallback:
1. Replace all `using Newtonsoft.Json;` with `using ConversationEditor;`
2. Replace `JsonConvert.SerializeObject(data, Formatting.Indented)` with `SimpleJsonSerializer.Serialize(data)`
3. Replace `JsonConvert.DeserializeObject<ConversationData>(json)` with `SimpleJsonSerializer.Deserialize(json)`

**Note:** The SimpleJsonSerializer is a basic implementation and may not support all advanced features like Dictionary serialization in Functions.

### Verification

Once the package is installed, you should be able to:
1. Create conversation files via: **Assets > Create > ConversationEngine > Conversation File**
2. Double-click any conversation JSON file to open the editor
3. Save and load conversations without errors

### Troubleshooting

**Problem:** "The type or namespace name 'Newtonsoft' could not be found"

**Solutions:**
1. Verify the package is installed in **Window > Package Manager**
2. Check that `Packages/manifest.json` contains the Newtonsoft.Json entry
3. Try reimporting all assets: **Assets > Reimport All**
4. Check the assembly definition files reference `Unity.Newtonsoft.Json`
5. Restart Unity Editor

**Problem:** Package won't install

**Solutions:**
1. Check your internet connection (Unity needs to download the package)
2. Check Unity's package manager cache: Delete `Library/PackageCache`
3. Use Unity 2020.1 or later (earlier versions may not support this package)
4. Manually download the .unitypackage from Unity Asset Store

### Assembly Definition Files

The following assembly definition files have been created:
- `Assets/ConversationEngine/ConversationScheme/ConversationEngine.asmdef`
- `Assets/ConversationEngine/Editor/ConversationEngine.Editor.asmdef`

Both reference `Unity.Newtonsoft.Json`. If you modify these files, ensure the reference remains intact.

### Next Steps

Once Newtonsoft.Json is properly installed:
1. Open the example conversation: `Assets/ConversationEngine/Examples/conversation_intro.json`
2. The Conversation Editor window should open automatically
3. Explore the visual node editor
4. Create your own conversation files!

For more information, see `README_EDITOR.md`.
