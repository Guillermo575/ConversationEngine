using System;
using System.Collections.Generic;
using UnityEngine;
namespace ConversationEngine.ActorBuilder
{
    [System.Serializable]
    public class SoundLibraryEntry
    {
        public string Id;
        public UnityEngine.Object SoundObject;
    }
    public class SoundLibrary : MonoBehaviour
    {
        #region Sound Data
        [SerializeField] private List<SoundLibraryEntry> entries = new List<SoundLibraryEntry>();
        #endregion

        #region Setup
        public void SetEntries(List<SoundLibraryEntry> values)
        {
            entries = values ?? new List<SoundLibraryEntry>();
        }
        #endregion

        #region Access
        public List<SoundLibraryEntry> GetEntries()
        {
            return entries;
        }
        #endregion
    }
}
