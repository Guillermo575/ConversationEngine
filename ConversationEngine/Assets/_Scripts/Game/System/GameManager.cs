using Codice.Client.BaseCommands;
using UnityEngine;
using UnityEngine.InputSystem;
public class GameManager : MonoBehaviour
{
    #region Variables
    [SerializeField] private InputActionAsset MainInput;
    #endregion

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
    void Awake()
    {
        CreateSingleton();
        EnableMainInputMap("UI", "Submit");
        EnableMainInputMap("UI", "Pause");
    }
    void Start()
    {
    }
    void Update()
    {
    }
    #endregion

    #region Get
    public InputActionAsset GetMainInput() { return MainInput; }
    public InputActionMap GetMainInputMap(string name = "UI")
    {
        return MainInput.FindActionMap(name);
    }
    public bool EnableMainInputMap(string map, string actionName)
    {
        InputActionMap inputMap = GetMainInputMap();
        if (inputMap == null) return false;
        InputAction submitAction = inputMap.FindAction(actionName);
        if (submitAction == null) return false;
        submitAction.Enable();
        return true;
    }
    public bool IsTriggered(string actionName)
    {
        InputActionMap inputMap = GetMainInputMap();
        if (inputMap == null) return false;
        InputAction submitAction = inputMap.FindAction(actionName);
        if (submitAction == null) return false;
        return submitAction.WasPerformedThisFrame();
    }
    #endregion
}