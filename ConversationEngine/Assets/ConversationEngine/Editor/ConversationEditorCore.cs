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
        #endregion
    }
}