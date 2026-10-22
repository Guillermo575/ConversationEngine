using UnityEngine;
using System.Linq;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{
    [SerializeField] private Dictionary<string, string> myDictionary;
    void Start()
    {
    }

    void Update()
    {
    }

    public string getValueDict(string name)
    {
        if (myDictionary != null && myDictionary.ContainsKey(name))
        {
            return myDictionary[name];
        }
        return null;
    }

    #region Singleton
    private static GameManager SingletonGameManager;
    private GameManager() { }
    private void CreateSingleton()
    {
        if (SingletonGameManager == null)
        {
            SingletonGameManager = this;
        }
        else
        {
            Debug.LogError("Ya existe una instancia de esta clase");
        }
    }
    public static GameManager GetSingleton()
    {
        return SingletonGameManager;
    }
    #endregion
}