using UnityEngine;

namespace ConversationEditor
{
    /// <summary>
    /// Helper class for Unity's Vector2 serialization
    /// JsonUtility handles Vector2 natively, so this is kept for compatibility
    /// </summary>
    public static class UnityVector2Converter
    {
        // JsonUtility natively supports Vector2, this class is no longer needed
        // but kept for potential future custom handling
    }
}
