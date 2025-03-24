using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugController : MonoBehaviour
{
    public static DebugController Instance { get; private set; }
    
    [SerializeField] private CinemachineInputProvider playerCameraInputProvider;
    [SerializeField] private CinemachineFreeLook playerFreeLookCamera;
    
    enum DebugCommandTag
    {
        NONE,
        GOOD,
        ERROR,
        WARNING
    }


    public bool ShowConsole;

    string _input;
    GUIStyle _textStyle;
    GUIStyle _inputStyle;
    GUIStyle _errorStyle;
    GUIStyle _goodStyle;

    public static DebugCommand HELP;
    public static DebugCommand CLEAR;
    public static DebugCommand<int> SET_BASE_SPEED;
    public static DebugCommand<bool> SET_GOD_MODE;
    public static DebugCommand<bool> SET_DEBUG_CAMERA;
    public static DebugCommand INVERT_DEBUG_CAMERA_MODE;
    public List<object> commandList;
    private Dictionary<string, DebugCommandTag> outputLog;

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
        
        HELP = new DebugCommand("help", "Show list of commands", "help", () =>
        {
            outputLog.Clear();
            outputLog.Add("SHOWING ALL AVAILABLE COMMANDS", DebugCommandTag.GOOD);

            foreach (var command in commandList)
            {
                DebugCommandBase cmd = command as DebugCommandBase;
                // outputLog.Add($"{cmd.commandFormat} - {cmd.commandDescription}", DebugCommandTag.NONE);
                LogCommandOutput($"{cmd.commandFormat} - {cmd.commandDescription}", DebugCommandTag.NONE);
            }
        });

        CLEAR = new DebugCommand("clear", "Clear the console", "clear", () =>
        {
            outputLog.Clear();
        });

        SET_BASE_SPEED = new DebugCommand<int>("set_base_speed", "Set the base speed of the player", "set_base_speed <speed>", (x) =>
        {
            Debug.Log("Setting base speed");
            // PlayerStateMachine.Instance.BaseSpeed = x;
            // outputLog.Add("Base speed set to " + x, DebugCommandTag.GOOD);
            LogCommandOutput("Base speed set to " + x, DebugCommandTag.GOOD);
        });

        SET_GOD_MODE = new DebugCommand<bool>("set_god_mode", "Set god mode for the player", "set_god_mode <true/false>", (x) =>
        {
            Debug.Log("Setting god mode");
            // PlayerStateMachine.Instance.GodMode = x;
            // outputLog.Add("God mode set to " + x, DebugCommandTag.GOOD);
            LogCommandOutput("God mode set to " + x, DebugCommandTag.GOOD);
        });

        INVERT_DEBUG_CAMERA_MODE = new DebugCommand("invert_camera", "Inverts Y axis of the camera", "invert_camera",() =>
        {
            Debug.Log("Inverting camera y");
            playerFreeLookCamera.m_YAxis.m_InvertInput = !playerFreeLookCamera.m_YAxis.m_InvertInput;
            LogCommandOutput("Inverting camera y", DebugCommandTag.GOOD);
        });

        commandList = new List<object> {
            HELP,
            CLEAR,
            SET_BASE_SPEED,
            SET_GOD_MODE,
            SET_DEBUG_CAMERA,
            INVERT_DEBUG_CAMERA_MODE,
        };

        outputLog = new Dictionary<string, DebugCommandTag>();
    }

    public void OnSubmit(InputValue value)
    {
        if (ShowConsole)
        {
            HandleInput();
            _input = "";
        }
    }

    public void OnToggleDebug(InputValue value)
    {
        ShowConsole = !ShowConsole;
        Cursor.lockState = ShowConsole ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = ShowConsole;
        playerCameraInputProvider.enabled = !ShowConsole;
        _input = "";
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

            // Have the output box grow in height based on the number of lines
            // viewport.height = 20 * (index + 1);

            // Use different styles based on the command tag (GOOD, ERROR, WARNING)
            switch (tag)
            {
                case DebugCommandTag.ERROR:
                    GUI.Label(outputRect, output, _errorStyle); // Render error in red
                    break;
                case DebugCommandTag.GOOD:
                    GUI.Label(outputRect, output, _goodStyle); // Render good output in green
                    break;
                default:
                    GUI.Label(outputRect, output, _textStyle); // Render normal output
                    break;
            }

            index++;
        }

        GUI.EndScrollView();


        GUI.Box(new Rect(0, y, Screen.width / 2, 40), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        _input = GUI.TextField(new Rect(10f, y, (Screen.width / 2) - 10f, 40f), _input, _inputStyle);
    }

    private void SetupTextStyles()
    {
        _textStyle = new GUIStyle(GUI.skin.label);
        _textStyle.fontSize = 16; // Set font size for text labels

        _inputStyle = new GUIStyle(GUI.skin.textField);
        _inputStyle.fontSize = 16; // Set font size for the input text field
        _inputStyle.alignment = TextAnchor.MiddleLeft; // Align text to the left

        _errorStyle = new GUIStyle(GUI.skin.label);
        _errorStyle.fontSize = 16; // Set font size for error text
        _errorStyle.fontStyle = FontStyle.Bold;
        _errorStyle.normal.textColor = Color.red; // Set red text color for errors
        _errorStyle.hover.textColor = Color.red; // Set red text color for errors

        _goodStyle = new GUIStyle(GUI.skin.label);
        _goodStyle.fontSize = 16; // Set font size for error text
        _goodStyle.fontStyle = FontStyle.Italic;
        _goodStyle.normal.textColor = Color.green; // Set red text color for errors
        _goodStyle.hover.textColor = Color.green; // Set red text color for errors
    }

    private void HandleInput()
    {
        string[] properties = _input.Split(' ');
        bool commandFound = false;

        for (int i = 0; i < commandList.Count; i++)
        {
            DebugCommandBase command = commandList[i] as DebugCommandBase;

            if (_input.Contains(command.commandId))
            {
                commandFound = true;

                if (commandList[i] as DebugCommand != null)
                {
                    (commandList[i] as DebugCommand).Invoke();
                }
                else if (commandList[i] as DebugCommand<int> != null)
                {
                    if (int.TryParse(properties[1], out int parsedValue))
                    {
                        (commandList[i] as DebugCommand<int>).Invoke(parsedValue);
                    }
                    else
                    {
                        // outputLog.Add("Invalid argument for command", DebugCommandTag.ERROR);
                        LogCommandOutput("Invalid argument for command", DebugCommandTag.ERROR);
                    }
                }
                else if (commandList[i] as DebugCommand<bool> != null)
                {
                    if (bool.TryParse(properties[1], out bool parsedValue))
                    {
                        (commandList[i] as DebugCommand<bool>).Invoke(parsedValue);
                    }
                    else
                    {
                        // outputLog.Add("Invalid argument for command", DebugCommandTag.ERROR);
                        LogCommandOutput("Invalid argument for command", DebugCommandTag.ERROR);
                    }
                }

                break;
            }
        }

        // If the command wasn't found, add it as an error
        if (!commandFound)
        {
            outputLog.Add("Command not found", DebugCommandTag.ERROR);
        }
    }

    private void LogCommandOutput(string output, DebugCommandTag tag)
    {
        // Ensure the key is unique before adding it to the dictionary
        string outputKey = output;
        int duplicateCount = 1;

        // If the key already exists, append a unique count to avoid duplication
        while (outputLog.ContainsKey(outputKey))
        {
            outputKey = $"{output} ({duplicateCount})";
            duplicateCount++;
        }

        // Add the unique key to the outputLog
        outputLog.Add(outputKey, tag);
    }
}
