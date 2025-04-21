using System;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class DebugController : MonoBehaviour
{
    public static DebugController Instance { get; private set; }

    [SerializeField] private CinemachineInputProvider playerCameraInputProvider;
    [SerializeField] private CinemachineFreeLook playerFreeLookCamera;
    [SerializeField] private InputReader _input;

    enum DebugCommandTag
    {
        NONE,
        GOOD,
        ERROR,
        WARNING
    }

    public bool ShowConsole;

    string _inputText;
    GUIStyle _textStyle;
    GUIStyle _inputStyle;
    GUIStyle _errorStyle;
    GUIStyle _goodStyle;

    public static DebugCommand HELP;
    public static DebugCommand CLEAR;

    // Player Commands
    public static DebugCommand<int> SET_BASE_SPEED;

    // Camera Commands
    public static DebugCommand INVERT_DEBUG_CAMERA_MODE;
    public static DebugCommand<float> SET_X_SENSITIVITY;
    public static DebugCommand<float> SET_Y_SENSITIVITY;
    public static DebugCommand<float> ADD_X_SENSITIVITY;
    public static DebugCommand<float> ADD_Y_SENSITIVITY;
    public static DebugCommand RESET_SENSITIVITY;

    // Scene Commands
    public static DebugCommand GO_TO_MAIN_MENU;

    public List<object> commandList;
    private Dictionary<string, DebugCommandTag> outputLog;

    private const string DebugInputControlName = "DebugConsoleInput";
    private bool shouldFocusInput = false;
    
    private UIManager uiManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
        uiManager = FindObjectOfType<UIManager>();

        _input.ToggleDebugModeEvent += HandleToggleDebug;
        _input.SubmitEvent += HandleSubmit;

        HELP = new DebugCommand("help", "Show list of commands", "help", () =>
        {
            outputLog.Clear();
            outputLog.Add("SHOWING ALL AVAILABLE COMMANDS", DebugCommandTag.GOOD);

            foreach (var command in commandList)
            {
                if (command is DebugCommandBase cmd)
                    LogCommandOutput($"{cmd.commandFormat} - {cmd.commandDescription}", DebugCommandTag.NONE);
            }
        });

        CLEAR = new DebugCommand("clear", "Clear the console", "clear", () =>
        {
            outputLog.Clear();
        });

        SET_BASE_SPEED = new DebugCommand<int>("set_base_speed", "Set the base speed of the player", "set_base_speed <speed>", (x) =>
        {
            var player = FindObjectOfType<KCCPlayerController>();
            if (player != null)
            {
                player.MaxStableMoveSpeed = x;
                LogCommandOutput("Base speed set to " + x, DebugCommandTag.GOOD);
            }
            else
            {
                LogCommandOutput("Player not found", DebugCommandTag.ERROR);
            }
        });

        INVERT_DEBUG_CAMERA_MODE = new DebugCommand("invert_camera", "Inverts Y axis of the camera", "invert_camera", () =>
        {
            if (playerFreeLookCamera != null)
            {
                playerFreeLookCamera.m_YAxis.m_InvertInput = !playerFreeLookCamera.m_YAxis.m_InvertInput;
                LogCommandOutput("Inverting camera y", DebugCommandTag.GOOD);
            }
            else
            {
                LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
            }
        });

        SET_X_SENSITIVITY = new DebugCommand<float>(
            "set_x_sensitivity",
            "Set X axis camera sensitivity",
            "set_x_sensitivity <value>",
            (x) =>
            {
                if (playerFreeLookCamera != null)
                {
                    playerFreeLookCamera.m_XAxis.m_MaxSpeed = x;
                    LogCommandOutput("X sensitivity set to " + x, DebugCommandTag.GOOD);
                }
                else
                {
                    LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
                }
            }
        );

        SET_Y_SENSITIVITY = new DebugCommand<float>(
            "set_y_sensitivity",
            "Set Y axis camera sensitivity",
            "set_y_sensitivity <value>",
            (y) =>
            {
                if (playerFreeLookCamera != null)
                {
                    playerFreeLookCamera.m_YAxis.m_MaxSpeed = y;
                    LogCommandOutput("Y sensitivity set to " + y, DebugCommandTag.GOOD);
                }
                else
                {
                    LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
                }
            }
        );

        ADD_X_SENSITIVITY = new DebugCommand<float>(
            "add_x_sensitivity",
            "Add to X axis camera sensitivity",
            "add_x_sensitivity <value>",
            (x) =>
            {
                if (playerFreeLookCamera != null)
                {
                    playerFreeLookCamera.m_XAxis.m_MaxSpeed += x;
                    LogCommandOutput("X sensitivity increased by " + x, DebugCommandTag.GOOD);
                }
                else
                {
                    LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
                }
            }
        );

        ADD_Y_SENSITIVITY = new DebugCommand<float>(
            "add_y_sensitivity",
            "Add to Y axis camera sensitivity",
            "add_y_sensitivity <value>",
            (y) =>
            {
                if (playerFreeLookCamera != null)
                {
                    playerFreeLookCamera.m_YAxis.m_MaxSpeed += y;
                    LogCommandOutput("Y sensitivity increased by " + y, DebugCommandTag.GOOD);
                }
                else
                {
                    LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
                }
            }
        );

        RESET_SENSITIVITY = new DebugCommand(
            "reset_sensitivity",
            "Reset camera sensitivity to default",
            "reset_sensitivity",
            () =>
            {
                if (playerFreeLookCamera != null)
                {
                    playerFreeLookCamera.m_XAxis.m_MaxSpeed = 120f; // Set your default value
                    playerFreeLookCamera.m_YAxis.m_MaxSpeed = 1f;   // Set your default value
                    LogCommandOutput("Camera sensitivity reset to default", DebugCommandTag.GOOD);
                }
                else
                {
                    LogCommandOutput("Camera not found", DebugCommandTag.ERROR);
                }
            }
        );

        GO_TO_MAIN_MENU = new DebugCommand(
            "main_menu",
            "Go to Main Menu",
            "main_menu",
            () =>
            {
                var uiManager = FindObjectOfType<UIManager>();
                if (uiManager != null)
                {
                    LogCommandOutput("Going To Main Menu Scene", DebugCommandTag.GOOD);
                    uiManager.PerformFadeOut(() =>
                    {
                        var gm = FindObjectOfType<GameManager>();
                        if (gm != null)
                            gm.currentLevel = 0;
                        SceneManager.LoadScene("Main Menu");
                    });
                }
                else
                {
                    LogCommandOutput("UIManager not found", DebugCommandTag.ERROR);
                }
            });

        commandList = new List<object> {
            HELP,
            CLEAR,
            SET_BASE_SPEED,
            INVERT_DEBUG_CAMERA_MODE,
            SET_X_SENSITIVITY,
            SET_Y_SENSITIVITY,
            ADD_X_SENSITIVITY,
            ADD_Y_SENSITIVITY,
            RESET_SENSITIVITY,
            GO_TO_MAIN_MENU
        };

        outputLog = new Dictionary<string, DebugCommandTag>();
    }

    public void HandleSubmit()
    {
        if (ShowConsole)
        {
            HandleInput();
            _inputText = "/";
        }
    }

    public void HandleToggleDebug()
    {
        if ((GameManager.Instance.CurrentState is GameManager.GameState.GameOver or GameManager.GameState.LoadingIn) || uiManager.uiIsPaused)
            return;

        ShowConsole = !ShowConsole;

        if (ShowConsole)
        {
            GameManager.Instance.PauseGame();
            shouldFocusInput = true;
            _inputText = "/";
        }
        else
        {
            GameManager.Instance.UnpauseGame();
        }

        playerCameraInputProvider.enabled = !ShowConsole;
    }

    Vector2 scroll;

    private void OnGUI()
    {
        if (!ShowConsole)
        {
            return;
        }

        if (_textStyle == null)
        {
            SetupTextStyles();
        }

        float y = (Screen.height / 2) + 100f;

        GUI.backgroundColor = new Color(0, 0, 0, 1f);

        // Box for showing command outputs and help
        GUI.Box(new Rect(0, y - 150f, Screen.width / 2, 150), "");
        Rect viewport = new Rect(0, 0, (Screen.width / 2) - 30, 40 * (outputLog.Count + commandList.Count));
        scroll = GUI.BeginScrollView(new Rect(0, y - 150f, Screen.width / 2 + 10.0f, 150), scroll, viewport);

        // Show the command help
        int index = 0;
        foreach (var entry in outputLog)
        {
            string output = entry.Key;
            DebugCommandTag tag = entry.Value;

            Rect outputRect = new Rect(5, 20 * index, viewport.width, 50 * (index + 1));

            switch (tag)
            {
                case DebugCommandTag.ERROR:
                    GUI.Label(outputRect, output, _errorStyle);
                    break;
                case DebugCommandTag.GOOD:
                    GUI.Label(outputRect, output, _goodStyle);
                    break;
                default:
                    GUI.Label(outputRect, output, _textStyle);
                    break;
            }

            index++;
        }

        GUI.EndScrollView();

        GUI.Box(new Rect(0, y, Screen.width / 2, 40), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        GUI.SetNextControlName(DebugInputControlName);
        _inputText = GUI.TextField(new Rect(10f, y, (Screen.width / 2) - 10f, 40f), _inputText, _inputStyle);

        if (shouldFocusInput)
        {
            GUI.FocusControl(DebugInputControlName);
            shouldFocusInput = false;
        }
    }

    private void SetupTextStyles()
    {
        _textStyle = new GUIStyle(GUI.skin.label);
        _textStyle.fontSize = 16;

        _inputStyle = new GUIStyle(GUI.skin.textField);
        _inputStyle.fontSize = 16;
        _inputStyle.alignment = TextAnchor.MiddleLeft;

        _errorStyle = new GUIStyle(GUI.skin.label);
        _errorStyle.fontSize = 16;
        _errorStyle.fontStyle = FontStyle.Bold;
        _errorStyle.normal.textColor = Color.red;
        _errorStyle.hover.textColor = Color.red;

        _goodStyle = new GUIStyle(GUI.skin.label);
        _goodStyle.fontSize = 16;
        _goodStyle.fontStyle = FontStyle.Italic;
        _goodStyle.normal.textColor = Color.green;
        _goodStyle.hover.textColor = Color.green;
    }

    private void HandleInput()
    {
        // Require commands to start with '/' and not be just '/'
        if (string.IsNullOrWhiteSpace(_inputText) || !_inputText.StartsWith("/") || _inputText.Length == 1)
        {
            LogCommandOutput("Commands must start with '/' and include a command", DebugCommandTag.ERROR);
            return;
        }
        
        outputLog.Clear();

        string commandText = _inputText.Substring(1);
        string[] properties = commandText.Split(' ');
        string inputCommand = properties[0].ToLower();
        bool commandFound = false;

        for (int i = 0; i < commandList.Count; i++)
        {
            if (commandList[i] is DebugCommandBase command && inputCommand == command.commandId.ToLower())
            {
                commandFound = true;
                try
                {
                    if (commandList[i] as DebugCommand != null)
                    {
                        (commandList[i] as DebugCommand)?.Invoke();
                    }
                    else if (commandList[i] as DebugCommand<int> != null)
                    {
                        if (properties.Length > 1 && int.TryParse(properties[1], out int parsedValue))
                        {
                            (commandList[i] as DebugCommand<int>)?.Invoke(parsedValue);
                        }
                        else
                        {
                            LogCommandOutput("Invalid or missing argument for command", DebugCommandTag.ERROR);
                        }
                    }
                    else if (commandList[i] as DebugCommand<float> != null)
                    {
                        if (properties.Length > 1 && float.TryParse(properties[1], out float parsedValue))
                        {
                            (commandList[i] as DebugCommand<float>)?.Invoke(parsedValue);
                        }
                        else
                        {
                            LogCommandOutput("Invalid or missing argument for command", DebugCommandTag.ERROR);
                        }
                    }
                    else if (commandList[i] as DebugCommand<bool> != null)
                    {
                        if (properties.Length > 1 && bool.TryParse(properties[1], out bool parsedValue))
                        {
                            (commandList[i] as DebugCommand<bool>)?.Invoke(parsedValue);
                        }
                        else
                        {
                            LogCommandOutput("Invalid or missing argument for command", DebugCommandTag.ERROR);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogCommandOutput("Command error: " + ex.Message, DebugCommandTag.ERROR);
                }
                break;
            }
        }

        if (!commandFound)
        {
            LogCommandOutput("Command not found", DebugCommandTag.ERROR);
        }
    }

    private void LogCommandOutput(string output, DebugCommandTag tag)
    {
        string outputKey = output;
        int duplicateCount = 1;

        while (outputLog.ContainsKey(outputKey))
        {
            outputKey = $"{output} ({duplicateCount})";
            duplicateCount++;
        }

        outputLog.Add(outputKey, tag);
    }

    private void OnDestroy()
    {
        _input.ToggleDebugModeEvent -= HandleToggleDebug;
        _input.SubmitEvent -= HandleSubmit;
    }
}
