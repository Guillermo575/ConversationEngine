using System;
using System.Collections.Generic;
using ConversationEngine.ConversationScheme;
using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    public class ActorData : MonoBehaviour
    {
        #region Actor Fields
        public string Id;
        public string Name;
        public List<AlternateText> lstAlternateName = new List<AlternateText>();
        public Sprite IconObject;
        #endregion
    }
}
