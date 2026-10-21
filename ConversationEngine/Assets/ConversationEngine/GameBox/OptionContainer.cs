using ConversationScheme;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
namespace GameBox
{
    public class OptionContainer : MonoBehaviour
    {
        [SerializeField] private GameObject btnOption;
        public void LoadOptions(List<ConversationOption> lstOptions)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject == btnOption)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                Destroy(child.gameObject);
            }
            if (lstOptions == null) return;
            foreach(var obj in lstOptions)
            {
                GameObject optionObject = Instantiate(btnOption, transform);
                optionObject.SetActive(true);
                TMP_Text textComponent = optionObject.GetComponentInChildren<TMP_Text>(true);
                if (textComponent == null) continue;
                textComponent.text = obj.Text;
            }
        }
    }
}