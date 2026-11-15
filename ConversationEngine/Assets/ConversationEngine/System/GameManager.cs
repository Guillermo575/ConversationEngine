using UnityEngine;
using System.Linq;
using System.Collections.Generic;
public class GameManager : MonoBehaviour
{
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

    #region Start & Update
    void Start()
    {
    }
    void Update()
    {
    }
    #endregion
}