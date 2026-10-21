using System.Collections;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
namespace GameBox
{
    public class DialogueBox : MonoBehaviour
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
            isTyping = !isTyping;
        }
        public void PauseTyping()
        {
            isTyping = false;
        }
        public void ResumeTyping()
        {
            isTyping = true;
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
            int l = 0;
            while (!IsTextFinished)
            {
                if (isTyping)
                {
                    char letter = fullText[l];
                    textMeshPro.text += letter;
                    l++;
                    yield return new WaitForSeconds(typingSpeed);
                }
                else
                {
                    yield return new WaitForSeconds(0.01f);
                }
            }
            isTyping = false;
        }
        #endregion
    }
}
