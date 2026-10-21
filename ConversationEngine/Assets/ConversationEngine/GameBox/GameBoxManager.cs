using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GameBox
{
    public class GameBoxManager : MonoBehaviour
    {
        public DialogueBox dialogueBox;
        public OptionContainer optionContainer;
        private bool TypingOptions = false;
        private void Start()
        {
            dialogueBox.StartTyping("adasdada adadadsasda aqeqw131 131231313123 123123131231 dasdadsdad qeqeqweqeqe 12312312312 123123123123123 asdad");
        }
        void Update()
        {
            if (!string.IsNullOrEmpty(dialogueBox.fullText) && dialogueBox.IsTextFinished && !TypingOptions)
            {
                optionContainer.LoadOptions(new List<ConversationScheme.ConversationOption>
                {
                    new ConversationScheme.ConversationOption { Text = "Option 1" },
                    new ConversationScheme.ConversationOption { Text = "Option 2" },
                    new ConversationScheme.ConversationOption { Text = "Option 3" }
                });
                TypingOptions = true;
            }
        }
    }
}