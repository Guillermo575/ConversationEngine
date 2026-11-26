using ConversationEngine.ConversationScheme;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace ConversationEngine.Box
{
    public class ContainerManager : MonoBehaviour
    {
        #region Public Variables
        public TextMeshProUGUI txtTile;
        public ContainerMain dialogueBox;
        public Transform optionContainer;
        public GameObject btnOption;
        public GameObject pnlNext;
        //[ConversationFileFilter]
        public TextAsset conversationFile;
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
            gameManager = GameManager.GetSingleton();
            LoadConversationFromAsset();
        }
        void Update()
        {
            var IsPressed = gameManager.IsTriggered("Submit");
            Debug.Log($"IsPressed: {IsPressed}");
            pnlNext.SetActive(dialogueBox.IsTextFinished);
            if (!string.IsNullOrEmpty(dialogueBox.fullText))
            {
                if (dialogueBox.IsTextFinished)
                {
                    if (CursorNode.Options != null && CursorNode.Options.Count > 0)
                    {
                        if (!TypingOptions)
                        {
                            LoadOptions(CursorNode.Options);
                            TypingOptions = true;
                        }
                        if (IsPressed)
                        {
                            var objSelectedOption = GetCurrentSelectedOption();
                            if (objSelectedOption != null)
                            {
                                objSelectedOption.onClick.Invoke();
                            }
                        }
                    }
                    else
                    {
                        if (IsPressed)
                        {
                            ProcessNode(CursorNode.NextNode);
                        }
                    }
                }
                else
                {
                    if (IsPressed)
                    {
                        dialogueBox.StopTyping();
                    }
                }
            }
        }
        private void LoadConversationFromAsset()
        {
            if (conversationFile == null) return;
            ConversationData data = JsonUtility.FromJson<ConversationData>(conversationFile.text);
            if (data == null || data.ConversationManager == null) return;
            LoadConversation(data.ConversationManager);
        }
        #endregion

        #region General Methods
        public void LoadConversation(ConversationManager conversationManager)
        {
            if (conversationManager == null || conversationManager.Nodes == null) return;
            lstNodes = conversationManager.Nodes;
            ConversationNode CurrentNode = null;
            var StartNode = (from x in lstNodes where ConversationNodeUtility.ParseConversationNodeType(x.NodeType) == ConversationNodeType.Start select x).ToList();
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
            ProcessNode(CurrentNode);
            //bool Finalizado = false;
            //while (!Finalizado)
            //{
            //}
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
            TypingOptions = false;
            pnlNext.SetActive(false);
            if (CursorNode == null) return;
            this.CursorNode = CursorNode;
            switch (ConversationNodeUtility.ParseConversationNodeType(CursorNode.NodeType))
            {
                case ConversationNodeType.Start: ProcessNode(CursorNode.NextNode); break;
                case ConversationNodeType.Dialogue: ProcessNodeText(CursorNode); break;
                case ConversationNodeType.Conditional: ProcessConditionalBranch(CursorNode.conditionalBranch); break;
                case ConversationNodeType.Function: ProcessNode(CursorNode.NextNode); break;
                case ConversationNodeType.End: Finalizado = true; break;
                case ConversationNodeType.Option: break;
                case ConversationNodeType.Observation: break;
            }
        }
        public void ProcessConditionalBranch(ConditionalBranch conditionalBranch)
        {
            if (conditionalBranch == null) return;
            if (ConversationEngine.GameHelper.ComparativeHelper.ProcessConditions(conditionalBranch))
            {
                ProcessNode(conditionalBranch.NextNodeTrue);
            }
            else
            {
                ProcessNode(conditionalBranch.NextNodeFalse);
            }
        }
        public void ProcessNodeText(ConversationNode conversationNode)
        {
            txtTile.text = conversationNode.Title;
            dialogueBox.StartTyping(CursorNode.Text);
        }
        #endregion

        #region Options Processing Methods
        public void LoadOptions(List<ConversationOption> lstOptions)
        {
            ClearOptions();
            if (lstOptions == null) return;
            foreach (var obj in lstOptions)
            {
                GameObject optionObject = Instantiate(btnOption, optionContainer);
                optionObject.SetActive(true);
                TMP_Text textComponent = optionObject.GetComponentInChildren<TMP_Text>(true);
                if (textComponent == null) continue;
                textComponent.text = obj.Text;
                Button buttonComponent = optionObject.GetComponent<Button>();
                if (buttonComponent != null)
                {
                    buttonComponent.onClick.AddListener(() => ClearAndProcessOption(obj.NextNode));
                }
            }
        }
        public void ClearOptions()
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
        }
        public void ClearAndProcessOption(ConversationNode conversationNode)
        {
            ClearOptions();
            ProcessNode(conversationNode);
        }
        public Button GetCurrentSelectedOption()
        {
            if (EventSystem.current == null) return null;
            if (optionContainer == null) return null;
            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            if (selectedObject == null) return null;
            Button selectedButton = selectedObject.GetComponent<Button>();
            if (selectedButton == null) selectedButton = selectedObject.GetComponentInParent<Button>();
            if (selectedButton == null) return null;
            if (!selectedButton.transform.IsChildOf(optionContainer)) return null;
            return selectedButton;
        }
        #endregion
    }
}