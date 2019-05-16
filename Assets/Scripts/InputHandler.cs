using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private Transform ControlsParent;
    [SerializeField] private GameObject ControlsPrefab;
    [SerializeField] private Sprite[] KeyboardSpritesOuter;
    [SerializeField] private Sprite[] KeyboardSpritesInner;
    [SerializeField] private Sprite[] ControllerSpritesOuter;
    [SerializeField] private Sprite[] ControllerSpritesInner;

    private bool ActiveInputWindow;
    private bool WindowInit;
    private bool KeyboardLocked;
    private bool ControllerLocked;
    private bool SureMessageSent;
    private int JoystickCount;
    private int OldJoystickCount;
    private ControlsState[] ControlsStates;
    private Color[] SpriteColors = new Color[] { new Color(0.4f, 0.6f, 0.6f), new Color(0.6f, 0.4f, 0.4f), new Color(0.4f, 0.6f, 0.4f), new Color(0.6f, 0.6f, 0.4f) };

    private enum ControlsState
    {
        Full,
        Split,
        Empty,
    }

    private void Start()
    {
        JoystickCount = GetJoystickCount();
        OldJoystickCount = JoystickCount;
        ControlsStates = new ControlsState[JoystickCount + 1];
        ShowHideInputWindowAndPause(true, true);
        SetupInputWindow();
    }

    private void Update()
    {
        HandleQuitGame();
        HandleJoystickCount();
        HandleEscKey();
        if (ActiveInputWindow)
        {
            HandleInputSwitchKeys();
            HandleSpaceKey();
            HandleSpaceInit();
        }
        HandleResetGame();
    }

    private void SetupInputWindow()
    {
        float offsetX = 150;
        float offsetY = 75;
        Vector2[][] positions = new Vector2[][]
        {
            new Vector2[] { Vector2.zero },
            new Vector2[] { Vector2.left * offsetX, Vector2.right * offsetX },
            new Vector2[] { Vector2.up * offsetY, new Vector2(-offsetX, -offsetY), new Vector2(offsetX, -offsetY) },
            new Vector2[] { new Vector2(-offsetX, offsetY), new Vector2(offsetX, offsetY), new Vector2(-offsetX, -offsetY), new Vector2(offsetX, -offsetY) }
        };
        for (int i = 0; i < JoystickCount + 1; i++)
        {
            Sprite controlsSpriteOuter = i == 0 ? KeyboardSpritesOuter[0] : ControllerSpritesOuter[0];
            Sprite controlsSpriteInner = i == 0 ? KeyboardSpritesInner[0] : ControllerSpritesInner[0];
            Transform controls = Instantiate(ControlsPrefab, ControlsParent).transform;
            controls.name = i == 0 ? "Keyboard" : "Controller" + i;
            controls.GetComponent<RectTransform>().localPosition = positions[JoystickCount][i];
            controls.GetComponent<Image>().sprite = controlsSpriteOuter;
            controls.GetChild(0).GetComponent<Image>().sprite = controlsSpriteInner;
            controls.GetChild(0).GetComponent<Image>().color = SpriteColors[i];
            controls.GetChild(1).gameObject.SetActive(false);
        }
    }

    private void DestroyAllChildrenAndSetup()
    {
        List<GameObject> children = new List<GameObject>();
        for (int i = 1; i < ControlsParent.childCount; i++)
        {
            children.Add(ControlsParent.GetChild(i).gameObject);
        }
        foreach (GameObject child in children)
        {
            Destroy(child);
        }
        SetupInputWindow();
    }

    private void ShowHideInputWindowAndPause(bool value, bool hideEsc = false)
    {
        ActiveInputWindow = value;
        ControlsParent.gameObject.SetActive(ActiveInputWindow);
        if (hideEsc)
        {
            ControlsParent.GetChild(0).GetChild(1).gameObject.SetActive(false);
        }
        if (ActiveInputWindow)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    private void HandleSpaceKey()
    {
        if (WindowInit && Input.GetKeyDown(KeyCode.Space))
        {
            if (GetComponent<PlayerManager>().GetCompleteChildCount(ControlsParent) > 0)
            {
                if (!SureMessageSent)
                {
                    SureMessageSent = true;
                    GetComponent<GameHandler>().ShowAnnouncerText("Are you sure you want to restart with new settings?", new Color(0.6f, 0.2f, 0.2f), false, 2);
                }
                else
                {
                    SureMessageSent = false;
                    GetComponent<PlayerManager>().SetPlayerSettings(ControlsParent, () =>
                    {
                        ResetGame();
                    });
                }
            }
            else
            {
                GetComponent<GameHandler>().ShowAnnouncerText("You need at least a keyboard or controller to play", new Color(0.6f, 0.2f, 0.2f), false, 2);
            }
        }
    }

    private void HandleSpaceInit()
    {
        if (!WindowInit && Input.GetKeyDown(KeyCode.Space))
        {
            if (GetComponent<PlayerManager>().GetCompleteChildCount(ControlsParent) > 0)
            {
                GetComponent<PlayerManager>().SetPlayerSettings(ControlsParent, () =>
                {
                    WindowInit = true;
                    ControlsParent.GetChild(0).GetChild(1).gameObject.SetActive(true);
                    ResetGame();
                });
            }
            else
            {
                GetComponent<GameHandler>().ShowAnnouncerText("You need at least a keyboard or controller to play", new Color(0.6f, 0.2f, 0.2f), false, 2);
            }
        }
    }

    private void HandleResetGame()
    {
        if(WindowInit && Input.GetKeyDown(KeyCode.R))
        {
            if (!SureMessageSent)
            {
                SureMessageSent = true;
                GetComponent<GameHandler>().ShowAnnouncerText("Are you sure you want to restart?", new Color(0.6f, 0.2f, 0.2f), false, 2);
            }
            else
            {
                SureMessageSent = false;
                WindowInit = false;
                ShowHideInputWindowAndPause(true, true);
                DestroyAllChildrenAndSetup();
            }
        }
    }

    private void HandleQuitGame()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!SureMessageSent)
            {
                SureMessageSent = true;
                GetComponent<GameHandler>().ShowAnnouncerText("Are you sure you want to quit?", new Color(0.6f, 0.2f, 0.2f), false, 2);
            }
            else
            {
                SureMessageSent = false;
                Application.Quit();
            }
        }
    }

    private void ResetGame()
    {
        GetComponent<PlayerManager>().DestroyPlayers();
        GetComponent<GhostManager>().DestroyGhosts();
        ShowHideInputWindowAndPause(!ActiveInputWindow);
        GetComponent<PlayerManager>().StartGame();
        GetComponent<GhostManager>().StartGame();
    }


    private void HandleEscKey()
    {
        if (WindowInit && Input.GetKeyDown(KeyCode.Escape))
        {
            SureMessageSent = false;
            ShowHideInputWindowAndPause(!ActiveInputWindow);
        }
    }

    public void PauseGame()
    {
        FindObjectOfType<Canvas>().transform.GetChild(1).gameObject.SetActive(false);
        FindObjectOfType<Canvas>().transform.GetChild(3).gameObject.SetActive(false);
        FindObjectOfType<GameHandler>().InterruptLightnings();
        FindObjectOfType<GameHandler>().enabled = false;
        foreach (PlayerMovement player in FindObjectsOfType<PlayerMovement>())
        {
            player.enabled = false;
            player.GetComponent<PlayerTorch>().enabled = false;
        }
        foreach (GhostMovement ghost in FindObjectsOfType<GhostMovement>())
        {
            ghost.enabled = false;
        }
    }

    private void ResumeGame()
    {
        FindObjectOfType<Canvas>().transform.GetChild(1).gameObject.SetActive(true);
        FindObjectOfType<Canvas>().transform.GetChild(3).gameObject.SetActive(true);
        FindObjectOfType<GameHandler>().enabled = true;
        foreach (PlayerMovement player in FindObjectsOfType<PlayerMovement>())
        {
            player.enabled = true;
            player.GetComponent<PlayerTorch>().enabled = true;
        }
        foreach (GhostMovement ghost in FindObjectsOfType<GhostMovement>())
        {
            ghost.enabled = true;
        }
    }

    private void HandleJoystickCount()
    {
        JoystickCount = GetJoystickCount();
        if (OldJoystickCount != JoystickCount)
        {
            HandleJoystickConnections();
        }
        OldJoystickCount = JoystickCount;
    }

    private int GetJoystickCount()
    {
        int joystickCount = 0;
        foreach (string joystick in Input.GetJoystickNames())
        {
            if (joystick != "")
            {
                joystickCount++;
            }
        }
        return joystickCount;
    }

    [ContextMenu("Handle Joystick Connections")]
    private void HandleJoystickConnections()
    {
        WindowInit = false;
        ShowHideInputWindowAndPause(true, true);
        DestroyAllChildrenAndSetup();
    }

    private void HandleInputSwitchKeys()
    {
        float keyboard = Input.GetAxis(Player.GetFullAxisName("MoveHorizontal", Player.InputType.Keyboard));
        int keyboardAxis = Mathf.Abs(keyboard) > 0.1f ? (int)Mathf.Sign(keyboard) : 0;
        int controllerAxis = Mathf.RoundToInt(Input.GetAxis(Player.GetFullAxisName("MoveHorizontal", Player.InputType.Controller)));
        if (!KeyboardLocked && keyboardAxis != 0)
        {
            HandleInputSwitch(keyboardAxis, 1);
            KeyboardLocked = true;
        }
        if (!ControllerLocked && controllerAxis != 0)
        {
            HandleInputSwitch(controllerAxis, 2);
            ControllerLocked = true;
        }
        if (KeyboardLocked)
        {
            StartCoroutine(WaitToUnlockKeyboard());
        }
        if (ControllerLocked)
        {
            StartCoroutine(WaitToUnlockController());
        }
    }

    private IEnumerator WaitToUnlockKeyboard()
    {
        float keyboard = Input.GetAxis(Player.GetFullAxisName("MoveHorizontal", Player.InputType.Keyboard));
        int keyboardAxis = Mathf.Abs(keyboard) > 0.1f ? (int)Mathf.Sign(keyboard) : 0;
        while (keyboardAxis != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        KeyboardLocked = false;
    }

    private IEnumerator WaitToUnlockController()
    {
        int controllerAxis = Mathf.RoundToInt(Input.GetAxis(Player.GetFullAxisName("MoveHorizontal", Player.InputType.Controller)));
        while (controllerAxis != 0)
        {
            yield return new WaitForEndOfFrame();
        }
        ControllerLocked = false;
    }

    private void HandleInputSwitch(int controlsAxis, int i)
    {
        switch (ControlsStates[i - 1])
        {
            case ControlsState.Full:
                ControlsStates[i - 1] = ControlsState.Split;
                ControlsParent.GetChild(i).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesOuter[1] : ControllerSpritesOuter[1];
                ControlsParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesInner[1] : ControllerSpritesInner[1];
                ControlsParent.GetChild(i).GetChild(1).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesInner[2] : ControllerSpritesInner[2];
                ControlsParent.GetChild(i).GetChild(0).GetComponent<Image>().color = SpriteColors[GetColorIndex(i - 1)];
                ControlsParent.GetChild(i).GetChild(1).GetComponent<Image>().color = SpriteColors[GetColorIndex(i - 1) + 1];
                ControlsParent.GetChild(i).GetChild(1).gameObject.SetActive(true);
                SetNextSpriteColor(i);
                break;
            case ControlsState.Split:
                ControlsStates[i - 1] = ControlsState.Empty;
                ControlsParent.GetChild(i).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesOuter[0] : ControllerSpritesOuter[0];
                ControlsParent.GetChild(i).GetChild(0).gameObject.SetActive(false);
                ControlsParent.GetChild(i).GetChild(1).gameObject.SetActive(false);
                SetNextSpriteColor(i);
                break;
            case ControlsState.Empty:
                ControlsStates[i - 1] = ControlsState.Full;
                ControlsParent.GetChild(i).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesOuter[0] : ControllerSpritesOuter[0];
                ControlsParent.GetChild(i).GetChild(0).GetComponent<Image>().sprite = i == 1 ? KeyboardSpritesInner[0] : ControllerSpritesInner[0];
                ControlsParent.GetChild(i).GetChild(0).GetComponent<Image>().color = SpriteColors[GetColorIndex(i - 1)];
                ControlsParent.GetChild(i).GetChild(0).gameObject.SetActive(true);
                SetNextSpriteColor(i);
                break;
            default:
                break;
        }
    }

    private int GetColorIndex(int i)
    {
        int index = 0;
        for (int j = 0; j < i; j++)
        {
            if (ControlsStates[j] == ControlsState.Full)
            {
                index++;
            }
            else if (ControlsStates[j] == ControlsState.Split)
            {
                index += 2;
            }
        }
        return index;
    }

    private void SetNextSpriteColor(int i)
    {
        for (int j = i; j < ControlsStates.Length + 1; j++)
        {
            if (ControlsStates[j - 1] != ControlsState.Empty)
            {
                ControlsParent.GetChild(j).GetChild(0).GetComponent<Image>().color = SpriteColors[GetColorIndex(j - 1)];
            }
            if (ControlsStates[j - 1] == ControlsState.Split)
            {
                ControlsParent.GetChild(j).GetChild(1).GetComponent<Image>().color = SpriteColors[GetColorIndex(j - 1) + 1];
            }
        }
    }
}
