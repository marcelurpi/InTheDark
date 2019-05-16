using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerManager : MonoBehaviour
{
    [HideInInspector] public PlayerMovement[] PlayerMovements;

    [SerializeField] private Player[] Players;
    [SerializeField] private GameObject PlayerPrefab;
    [SerializeField] private Transform PlayersParent;

    public void StartGame()
    {
        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        int maxPlayers = 4;
        PlayerMovements = new PlayerMovement[Mathf.Min(Players.Length, maxPlayers)];
        for (int i = 0; i < Mathf.Min(Players.Length, maxPlayers); i++)
        {
            GameObject playerGameObject = Instantiate(PlayerPrefab, PlayersParent);
            playerGameObject.name = "Player" + (i + 1);
            playerGameObject.transform.position = Players[i].SpawnPosition;
            PlayerMovement playerMovement = playerGameObject.GetComponent<PlayerMovement>();
            playerMovement.PlayerColor = Players[i].PlayerColor;
            playerMovement.PlayerInputType = Players[i].PlayerInputType;
            playerMovement.PlayerInputSide = Players[i].PlayerInputSide;
            playerMovement.SetColorAndLayer(i);
            PlayerMovements[i] = playerMovement;
        }
        GetComponent<GameHandler>().CreatePlayerTexts();
    }

    public void SetPlayerSettings(Transform controlsParent, Action actionAfterSetting)
    {
        Vector2[][] spawnPositions = new Vector2[][]
        {
            new Vector2[] { Vector2.zero },
            new Vector2[] { Vector2.left, Vector2.right },
            new Vector2[] { Vector2.left, Vector2.zero, Vector2.right },
            new Vector2[] { Vector2.left * 2, Vector2.left, Vector2.right, Vector2.right * 2 },
        };
        int completeChildCount = GetCompleteChildCount(controlsParent);
        Players = new Player[completeChildCount];
        for (int i = 0; i < completeChildCount; i++)
        {
            bool isRight = false;
            Transform childTransform = GetChildTransform(controlsParent, ref isRight, i);
            if (childTransform != null)
            {
                bool isFull = !childTransform.parent.GetChild(1).gameObject.activeSelf;
                Players[i] = new Player
                {
                    PlayerInputType = childTransform.parent.name == "Keyboard" ? Player.InputType.Keyboard : Player.InputType.Controller,
                    PlayerInputSide = isFull ? Player.InputSide.Full : isRight ? Player.InputSide.Right : Player.InputSide.Left,
                    PlayerColor = childTransform.GetComponent<Image>().color,
                    SpawnPosition = spawnPositions[completeChildCount - 1][i]
                };
            }
        }
        actionAfterSetting?.Invoke();
    }

    public int GetCompleteChildCount(Transform controlsParent)
    {
        int count = 0;
        for (int i = 1; i < controlsParent.childCount; i++)
        {
            count = controlsParent.GetChild(i).GetChild(0).gameObject.activeSelf ? count + 1 : count;
            count = controlsParent.GetChild(i).GetChild(1).gameObject.activeSelf ? count + 1 : count;
        }
        return count;
    }

    private Transform GetChildTransform(Transform controlsParent, ref bool isRight, int i)
    {
        int count = 0;
        for (int j = 1; j < controlsParent.childCount; j++)
        {
            if (controlsParent.GetChild(j).GetChild(0).gameObject.activeSelf)
            {
                if (i == count)
                {
                    return controlsParent.GetChild(j).GetChild(0);
                }
                count++;
            }
            if (controlsParent.GetChild(j).GetChild(1).gameObject.activeSelf)
            {
                if (i == count)
                {
                    isRight = true;
                    return controlsParent.GetChild(j).GetChild(1);
                }
                count++;
            }
        }
        return null;
    }

    public void DestroyPlayers()
    {
        foreach (PlayerMovement player in PlayerMovements)
        {
            Destroy(player.gameObject);
        }
        GetComponent<GameHandler>().DestroyPlayerTexts();
    }
}

[Serializable]
public class Player
{
    public enum InputType
    {
        Keyboard,
        Controller,
    };

    public enum InputSide
    {
        Full,
        Left,
        Right,
    };

    public Color PlayerColor;
    public InputType PlayerInputType;
    public InputSide PlayerInputSide = InputSide.Full;
    public Vector2 SpawnPosition;

    public static string GetFullAxisName(string baseAxisName, InputType inputType, InputSide inputSide = InputSide.Full)
    {
        switch (inputType)
        {
            case InputType.Keyboard:
                baseAxisName = "KB" + baseAxisName;
                break;
            case InputType.Controller:
                baseAxisName = "CR" + baseAxisName;
                break;
            default:
                break;
        }
        switch (inputSide)
        {
            case InputSide.Left:
                baseAxisName += "L";
                break;
            case InputSide.Right:
                baseAxisName += "R";
                break;
            default:
                break;
        }
        return baseAxisName;
    }
}
