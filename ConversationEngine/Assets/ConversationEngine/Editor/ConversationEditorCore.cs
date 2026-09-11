using ConversationScheme;
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
    }
}