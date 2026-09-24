using ConversationScheme;
using UnityEngine;
namespace ConversationEditor
{
    public class ConversationEditorCore
    {
        #region Singleton
        private static ConversationEditorCore SingletonObject;
        private ConversationEditorCore() { }
        private ConversationEditorCore CreateSingleton()
        {
            if (SingletonObject == null)
            {
                SingletonObject = this;
            }
            return SingletonObject;
        }
        public static ConversationEditorCore GetSingleton()
        {
            if (SingletonObject == null)
            {
                SingletonObject = new ConversationEditorCore().CreateSingleton();
            }
            return SingletonObject;
        }
        #endregion

        #region Core Data
        public ConversationData conversationData;
        public string currentFilePath;
        public bool isDirty = false;
        #endregion

        #region Constants
        public const float gridSpacing = 20f;
        public static readonly Color gridColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        public const float zoomControlScale = 1.5f;
        public const int minNodeFontSize = 8;
        public const int nodeHeaderBaseFontSize = 11;
        public const int nodeBodyBaseFontSize = 12;
        public const float optionDefaultWidth = 150f;
        public const float optionDefaultHeight = 60f;
        public const float optionDefaultSpacing = 10f;
        public const float minEditorNodeSize = 20f;
        public const float nodeHorizontalPadding = 16f;
        public const float nodeVerticalPadding = 12f;
        public const float estimatedLineSpacing = 3f;
        public const float minZoom = 0.1f;
        public const float maxZoom = 5.0f;
        #endregion

        #region Helper Methods
        public int GetScaledNodeFontSize(int baseFontSize, float zoom)
        {
            return Mathf.Max(minNodeFontSize, Mathf.RoundToInt(baseFontSize * zoom));
        }
        public Vector2 ClampEditorSize(Vector2 size)
        {
            return new Vector2(Mathf.Max(minEditorNodeSize, size.x), Mathf.Max(minEditorNodeSize, size.y));
        }
        public int GetNodePreviewTextLength(ConversationNode node, bool hasActorLine, float zoom)
        {
            int bodyFontSize = GetScaledNodeFontSize(nodeHeaderBaseFontSize, zoom);
            float scaledZoom = Mathf.Max(minZoom, zoom);
            float usableWidth = Mathf.Max(minEditorNodeSize, (node.EditorSize.x - nodeHorizontalPadding) * scaledZoom);
            float headerHeight = GetScaledNodeFontSize(nodeHeaderBaseFontSize, zoom) + estimatedLineSpacing;
            float actorHeight = hasActorLine ? bodyFontSize + estimatedLineSpacing : 0f;
            float usableHeight = Mathf.Max(minEditorNodeSize, (node.EditorSize.y - nodeVerticalPadding) * scaledZoom - headerHeight - actorHeight);
            return EstimatePreviewLength(usableWidth, usableHeight, bodyFontSize);
        }
        public int GetOptionPreviewTextLength(ConversationOption option, float zoom)
        {
            int bodyFontSize = GetScaledNodeFontSize(nodeBodyBaseFontSize, zoom);
            float scaledZoom = Mathf.Max(minZoom, zoom);
            float usableWidth = Mathf.Max(minEditorNodeSize, (option.EditorSize.x - nodeHorizontalPadding) * scaledZoom);
            float headerHeight = bodyFontSize + estimatedLineSpacing;
            float usableHeight = Mathf.Max(minEditorNodeSize, (option.EditorSize.y - nodeVerticalPadding) * scaledZoom - headerHeight);
            return EstimatePreviewLength(usableWidth, usableHeight, bodyFontSize);
        }
        public int EstimatePreviewLength(float width, float height, int fontSize)
        {
            float estimatedCharacterWidth = Mathf.Max(1f, fontSize * 0.55f);
            float lineHeight = Mathf.Max(1f, fontSize + estimatedLineSpacing);
            int charsPerLine = Mathf.Max(1, Mathf.FloorToInt(width / estimatedCharacterWidth));
            int maxLines = Mathf.Max(1, Mathf.FloorToInt(height / lineHeight));
            return charsPerLine * maxLines;
        }
        #endregion
    }
}