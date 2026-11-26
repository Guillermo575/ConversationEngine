using System.Collections;
using TMPro;
using UnityEngine;
namespace Game.ConversationBox
{
    public class ContainerMain : MonoBehaviour
    {
        #region Variables
        [SerializeField] private TextMeshProUGUI textMeshPro;
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private float typingSpeed = 0.1f;
        [SerializeField] public string fullText;
        #endregion

        #region Private
        private Coroutine typingCoroutine;
        private bool isTyping = false;
        public bool IsTextFinished { get { return textMeshPro.text.Length >= fullText.Length; } }
        #endregion

        #region General
        private void Update()
        {
            if (isTyping)
            {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 1000000);
            }
        }
        public void StartTyping(string fullText)
        {
            this.fullText = fullText;
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText());
        }
        #endregion

        #region Manage Type
        public void PauseOrResumeTyping()
        {
            isTyping = IsTextFinished ? false : !isTyping;
        }
        public void PauseTyping()
        {
            isTyping = false;
        }
        public void ResumeTyping()
        {
            isTyping = IsTextFinished ? false : true;
        }
        public void StopTyping()
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            textMeshPro.text = fullText;
            isTyping = false;
        }
        #endregion

        #region Coroutine
        private IEnumerator TypeText()
        {
            textMeshPro.text = "";
            isTyping = true;
            string[] words = fullText.Split(' ');
            foreach (var word in words)
            {
                string currentText = textMeshPro.text;
                if (!string.IsNullOrEmpty(currentText))
                {
                    string tempText = currentText + " " + word;
                    float textWidth = textMeshPro.GetPreferredValues(tempText).x;
                    float containerWidth = rectTransform.rect.width;
                    if (textWidth > containerWidth)
                    {
                        textMeshPro.text += "\n";
                    }
                }
                for (int l = 0; l < word.Length;)
                {
                    if (isTyping)
                    {
                        textMeshPro.text += word[l++];
                        yield return new WaitForSeconds(typingSpeed);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.01f);
                    }
                }
                textMeshPro.text += " ";
                yield return new WaitForSeconds(typingSpeed);
            }
            isTyping = false;
        }
        #endregion
    }
}