using UnityEngine;
using UnityEngine.InputSystem;

public class PauseController : MonoBehaviour
{
    public static PauseController Instance { get; private set; }

    public bool IsPaused { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private InputActionReference pauseAction; // assegna l'azione dal tuo Input Actions asset

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        pauseAction.action.Enable();
        pauseAction.action.performed += OnPausePressed;
    }

    void OnDisable()
    {
        pauseAction.action.performed -= OnPausePressed;
        pauseAction.action.Disable();
    }

    void OnPausePressed(InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        pauseMenuUI.SetActive(IsPaused);
    }
}
