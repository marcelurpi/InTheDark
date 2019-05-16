using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public class GameHandler : MonoBehaviour
{
    [HideInInspector] public PlayerMovement[] Players;

    [SerializeField] private bool AllLightningsDisabled, AutoLightningsDisabled, BackgroundLightningDisabled;
    [SerializeField] private float LightningTickerMin, LightningTickerMax;
    [SerializeField] private GameObject PlayerTextsPrefab, KillsTextGameObject, AnnouncerTextGameObject;
    [SerializeField] private GameObject EnvironmentLightGameObject;
    [SerializeField] private Tilemap FloorTilemap;

    private float LightningTicker;
    private bool PlayerTextsActive;
    private bool strikingLightning;
    private Color FloorDarkColor;
    private List<Coroutine> LightningCoroutines;
    private GameObject[] BatteryTextGameObjects, LivesTextGameObjects;

    private void Start()
    {
        BatteryTextGameObjects = new GameObject[0];
        LivesTextGameObjects = new GameObject[0];
        LightningCoroutines = new List<Coroutine>();
        FloorDarkColor = FloorTilemap.color;
        AnnouncerTextGameObject.SetActive(false);
        LightningTicker = UnityEngine.Random.Range(LightningTickerMin, LightningTickerMax);
    }

    private void Update()
    {
        if (PlayerTextsActive)
        {
            UpdateBatteryText();
            UpdateLivesText();
        }
        UpdateKillsText();
        HandleLightnings();
    }

    public void CreatePlayerTexts()
    {
        Players = GetComponent<PlayerManager>().PlayerMovements;
        BatteryTextGameObjects = new GameObject[Players.Length];
        LivesTextGameObjects = new GameObject[Players.Length];
        Vector2[] anchoredPositions = new Vector2[] { new Vector2(100, -40), new Vector2(-100, -40), new Vector2(100, 50), new Vector2(-100, 50) };
        float textsOffset = 230;
        Transform playerTextsParent = FindObjectOfType<Canvas>().transform.GetChild(3);
        for (int i = 0; i < Players.Length; i++)
        {
            GameObject playerTexts = Instantiate(PlayerTextsPrefab, playerTextsParent);
            playerTexts.name = "Player " + (i + 1) + " texts";

            GameObject batteryText = playerTexts.transform.GetChild(0).gameObject;
            TextChildSetup(ref batteryText, i);
            batteryText.GetComponent<RectTransform>().anchoredPosition = anchoredPositions[i];
            BatteryTextGameObjects[i] = batteryText;

            GameObject livesText = playerTexts.transform.GetChild(1).gameObject;
            TextChildSetup(ref livesText, i);
            Vector2 direction = i % 2 == 0 ? Vector2.right : Vector2.left;
            Vector2 anchoredPositionOffsetted = anchoredPositions[i] + direction * textsOffset;
            livesText.GetComponent<RectTransform>().anchoredPosition = anchoredPositionOffsetted;
            LivesTextGameObjects[i] = livesText;
        }
        PlayerTextsActive = true;
    }

    private void TextChildSetup(ref GameObject textChild, int i)
    {
        Vector2[] anchors = new Vector2[] { Vector2.up, Vector2.one, Vector2.zero, Vector2.right };
        TextAlignmentOptions[] alignments = new TextAlignmentOptions[] { TextAlignmentOptions.TopLeft,
            TextAlignmentOptions.TopRight, TextAlignmentOptions.BottomLeft, TextAlignmentOptions.BottomRight };
        textChild.GetComponent<RectTransform>().pivot = anchors[i];
        textChild.GetComponent<RectTransform>().anchorMin = anchors[i];
        textChild.GetComponent<RectTransform>().anchorMax = anchors[i];
        Color playerColor = Players[i].GetComponent<PlayerMovement>().PlayerColor;
        textChild.GetComponent<TextMeshProUGUI>().color = playerColor;
        textChild.GetComponent<TextMeshProUGUI>().alignment = alignments[i];
    }

    public void DestroyPlayerTexts()
    {
        PlayerTextsActive = false;
        foreach(GameObject text in BatteryTextGameObjects)
        {
            Destroy(text);
        }
        foreach (GameObject text in LivesTextGameObjects)
        {
            Destroy(text);
        }
    }

    private void UpdateBatteryText()
    {
        for (int i = 0; i < Players.Length; i++)
        {
            int batteryCharge = Mathf.RoundToInt(Players[i].GetComponent<PlayerTorch>().TorchBatteryCharge);
            BatteryTextGameObjects[i].GetComponent<TextMeshProUGUI>().text = "Battery: " + batteryCharge;
        }
    }

    private void UpdateLivesText()
    {
        for (int i = 0; i < Players.Length; i++)
        {
            int remainingLives = Players[i].GetComponent<PlayerMovement>().PlayerLives;
            LivesTextGameObjects[i].GetComponent<TextMeshProUGUI>().text = "Lives: " + remainingLives;
        }
    }

    private void UpdateKillsText()
    {
        int ghostsKilled = 0;
        for (int i = 0; i < Players.Length; i++)
        {
            ghostsKilled += Players[i].GetComponent<PlayerTorch>().GhostsKilledCount;
        }
        KillsTextGameObject.GetComponent<TextMeshProUGUI>().text = ghostsKilled.ToString();
    }

    public void ShowAnnouncerText(string text, Color color, bool permanent, float secondsToHide = 0, Action actionAfterAnnouncer = null)
    {
        AnnouncerTextGameObject.GetComponent<TextMeshProUGUI>().text = text;
        AnnouncerTextGameObject.GetComponent<TextMeshProUGUI>().color = color;
        AnnouncerTextGameObject.SetActive(true);
        if (!permanent)
        {
            StartCoroutine(HideAnnouncerText(secondsToHide, actionAfterAnnouncer));
        }
    }

    private IEnumerator HideAnnouncerText(float secondsToHide, Action actionAfterAnnouncer)
    {
        yield return new WaitForSeconds(secondsToHide);
        AnnouncerTextGameObject.SetActive(false);
        actionAfterAnnouncer?.Invoke();
    }

    private void HandleLightnings()
    {
        if(!strikingLightning && FloorTilemap.color != FloorDarkColor)
        {
            FloorTilemap.color = FloorDarkColor;
        }
        if (!AutoLightningsDisabled)
        {
            if (LightningTicker > 0)
            {
                LightningTicker -= Time.deltaTime;
            }
            else
            {
                StrikeLightnings();
            }
        }
    }

    [ContextMenu("Strike Lightnings")]
    public void StrikeLightnings()
    {
        if (!AllLightningsDisabled && !strikingLightning)
        {
            strikingLightning = true;
            LightningTicker = UnityEngine.Random.Range(LightningTickerMin, LightningTickerMax);
            Coroutine lightningCoroutine = StartCoroutine(StrikeTwoLightnings(() => {
                strikingLightning = false;
            }));
            LightningCoroutines.Add(lightningCoroutine);
        }
    }

    public void InterruptLightnings()
    {
        while (LightningCoroutines.Count > 0)
        {
            StopCoroutine(LightningCoroutines[0]);
            LightningCoroutines.RemoveAt(0);
        }
        strikingLightning = false;
        FloorTilemap.color = FloorDarkColor;
        if (!BackgroundLightningDisabled) FindObjectOfType<Camera>().backgroundColor = FloorDarkColor;
        EnvironmentLightGameObject.SetActive(false);
    }

    private IEnumerator StrikeTwoLightnings(Action actionAfterStrikes)
    {
        float enterSpeed = 20;
        float duration = 0.1f;
        Coroutine lightningCoroutine1 = StartCoroutine(StrikeOneLightning(duration, enterSpeed, 5));
        LightningCoroutines.Add(lightningCoroutine1);
        yield return lightningCoroutine1;
        Coroutine lightningCoroutine2 = StartCoroutine(StrikeOneLightning(duration, enterSpeed, 1.5f));
        LightningCoroutines.Add(lightningCoroutine2);
        yield return lightningCoroutine2;
        actionAfterStrikes?.Invoke();
    }

    private IEnumerator StrikeOneLightning(float duration, float enterSpeed, float exitSpeed)
    {
        Color lightColor = Color.white;
        EnvironmentLightGameObject.SetActive(true);
        yield return StartCoroutine(LerpTilemapColorWithSpeed(FloorDarkColor, lightColor, enterSpeed));
        yield return new WaitForSeconds(duration);
        yield return StartCoroutine(LerpTilemapColorWithSpeed(lightColor, FloorDarkColor, exitSpeed));
        EnvironmentLightGameObject.SetActive(false);
    }

    private IEnumerator LerpTilemapColorWithSpeed(Color floorStart, Color floorEnd, float speed)
    {
        float lerpPercent = 0;
        while (lerpPercent < 1)
        {
            Color lerpColor = Color.Lerp(floorStart, floorEnd, lerpPercent);
            FloorTilemap.color = lerpColor;
            if(!BackgroundLightningDisabled) FindObjectOfType<Camera>().backgroundColor = lerpColor;
            lerpPercent += speed * Time.deltaTime;
            yield return null;
        }
        FloorTilemap.color = floorEnd;
        if(!BackgroundLightningDisabled) FindObjectOfType<Camera>().backgroundColor = floorEnd;
    }

    public void GameOver()
    {
        ShowAnnouncerText("GAME OVER, Press R key to restart", new Color(0.6f, 0.2f, 0.2f), true);
        GetComponent<InputHandler>().PauseGame();
        InterruptLightnings();
    }
}
