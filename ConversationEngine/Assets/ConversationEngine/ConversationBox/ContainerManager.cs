using ConversationScheme;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
namespace ConversationBox
{
    public class ContainerManager : MonoBehaviour
    {
        #region Public Variables
        public ContainerMain dialogueBox;
        public Transform optionContainer;
        public GameObject btnOption;
        #endregion

        #region Private Variables
        private bool TypingOptions = false;
        private List<ConversationNode> lstNodes = new List<ConversationNode>();
        private ConversationNode CursorNode;
        private bool Finalizado = false;
        private GameManager gameManager;
        #endregion

        #region Unity Methods
        private void Start()
        {
            dialogueBox.StartTyping("adasdada adadadsasda aqeqw131 131231313123 123123131231 dasdadsdad qeqeqweqeqe 12312312312 123123123123123 asdad");
        }
        void Update()
        {
            if (!string.IsNullOrEmpty(dialogueBox.fullText) && dialogueBox.IsTextFinished && !TypingOptions)
            {
                LoadOptions(new List<ConversationScheme.ConversationOption>
                {
                    new ConversationScheme.ConversationOption { Text = "Option 1" },
                    new ConversationScheme.ConversationOption { Text = "Option 2" },
                    new ConversationScheme.ConversationOption { Text = "Option 3" }
                });
                TypingOptions = true;
            }
        }
        #endregion

        #region General Methods
        public void LoadConversation(ConversationManager conversationManager)
        {
            if (conversationManager == null || conversationManager.Nodes == null)
                lstNodes = conversationManager.Nodes;
            Node CurrentNode = null;
            var StartNode = (from x in lstNodes where x.NodeType == ConversationNodeType.Start select x).ToList();
            if (StartNode.Count == 0) return;
            CurrentNode = StartNode.First();
            foreach (var obj in lstNodes)
            {
                if (obj.NextNodeId != 0)
                {
                    obj.NextNode = SearchNode(obj.NextNodeId);
                }
                if (obj.conditionalBranch != null)
                {
                    obj.conditionalBranch.NextNodeTrue = SearchNode(obj.conditionalBranch.NextNodeIdTrue);
                    obj.conditionalBranch.NextNodeFalse = SearchNode(obj.conditionalBranch.NextNodeIdFalse);
                }
                if (obj.Options != null)
                {
                    foreach (var objOptions in obj.Options)
                    {
                        objOptions.NextNode = SearchNode(objOptions.NextNodeId);
                    }
                }
            }
            bool Finalizado = false;
            while (!Finalizado)
            {

            }
        }
        public ConversationNode SearchNode(int Id)
        {
            var lst = (from x in lstNodes where x.Id == Id select x).ToList();
            if (lst.Count > 0)
            {
                return lst.First();
            }
            return null;
        }
        #endregion

        #region Conversation Processing Methods
        public void ProcessNode(ConversationNode CursorNode)
        {
            if (CursorNode == null) return;
            this.CursorNode = CursorNode;
            switch (CursorNode.NodeType)
            {
                case ConversationNodeType.Start: break;
                case ConversationNodeType.Dialogue: break;
                case ConversationNodeType.Conditional: ProcessConditionalBranch(CursorNode.conditionalBranch); break;
                case ConversationNodeType.Function: break;
                case ConversationNodeType.End: Finalizado = true; break;
                case ConversationNodeType.Option: break;
                case ConversationNodeType.Observation: break;
            }
        }
        public void ProcessConditionalBranch(ConditionalBranch conditionalBranch)
        {
            if (conditionalBranch == null) return;
            if (Helper.ProcessConditions(conditionalBranch))
            {
                ProcessNode(conditionalBranch.NextNodeTrue);
            }
            else
            {
                ProcessNode(conditionalBranch.NextNodeFalse);
            }
        }
        #endregion

        #region Options Processing Methods
        public void LoadOptions(List<ConversationOption> lstOptions)
        {
            for (int i = optionContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = optionContainer.GetChild(i);
                if (child.gameObject == btnOption)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                Destroy(child.gameObject);
            }
            if (lstOptions == null) return;
            foreach (var obj in lstOptions)
            {
                GameObject optionObject = Instantiate(btnOption, optionContainer);
                optionObject.SetActive(true);
                TMP_Text textComponent = optionObject.GetComponentInChildren<TMP_Text>(true);
                if (textComponent == null) continue;
                textComponent.text = obj.Text;
            }
        }
        #endregion
    }
}