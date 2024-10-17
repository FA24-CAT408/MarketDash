using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugController : MonoBehaviour
{
    public bool ShowConsole;

    bool _showHelp;
    string _input;

    public static DebugCommand HELP;
    public static DebugCommand<int> SET_BASE_SPEED;
    public List<object> commandList;

    private void Awake()
    {
        SET_BASE_SPEED = new DebugCommand<int>("set_base_speed", "Set the base speed of the player", "set_base_speed <speed>", (x) =>
        {
            Debug.Log("Setting base speed");
            PlayerStateMachine.Instance.BaseSpeed = x;
        });

        HELP = new DebugCommand("help", "Show list of commands", "help", () =>
        {
            _showHelp = true;
        });

        commandList = new List<object> {
            HELP,
            SET_BASE_SPEED,
        };
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
    }

    Vector2 scroll;

    private void OnGUI()
    {
        if (!ShowConsole)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float y = 0.0f;

        if (_showHelp)
        {
            GUI.Box(new Rect(0, 0, Screen.width, 100), "");

            Rect viewport = new Rect(0, 0, Screen.width - 30, 20 * commandList.Count);

            scroll = GUI.BeginScrollView(new Rect(0, y + 5f, Screen.width, 90), scroll, viewport);

            for (int i = 0; i < commandList.Count; i++)
            {
                DebugCommandBase command = commandList[i] as DebugCommandBase;

                string label = $"{command.commandFormat} - {command.commandDescription}";

                Rect labelRect = new Rect(5, 20 * i, viewport.width - 100, 20);

                GUI.Label(labelRect, label);
            }

            GUI.EndScrollView();

            y += 100f;
        }

        GUI.Box(new Rect(0, y, Screen.width, 30), "");
        GUI.backgroundColor = new Color(0, 0, 0, 0);
        _input = GUI.TextField(new Rect(10f, y + 5f, Screen.width - 20f, 20f), _input);
    }

    private void HandleInput()
    {
        string[] properties = _input.Split(' ');

        for (int i = 0; i < commandList.Count; i++)
        {
            DebugCommandBase command = commandList[i] as DebugCommandBase;

            if (_input.Contains(command.commandId))
            {
                if (commandList[i] as DebugCommand != null)
                {
                    (commandList[i] as DebugCommand).Invoke();
                }
                else if (commandList[i] as DebugCommand<int> != null)
                {
                    (commandList[i] as DebugCommand<int>).Invoke(int.Parse(properties[1]));
                }
            }
        }
    }
}
