using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTorch : MonoBehaviour
{
    [HideInInspector] public float TorchBatteryCharge;
    [HideInInspector] public int GhostsKilledCount = 0;

    [SerializeField] private float TorchBatteryChargeStart, TorchBatteryLosePerSec, TorchHitboxSize;
    [SerializeField] private Sprite TorchEnabledSprite, TorchDisabledSprite;
    [SerializeField] private AudioClip TorchSoundClip, BatteryPickupClip, GhostDeathClip;

    private bool TorchEnabled, TorchEmptyAnnounced, BatteryPickupSpawned;
    private bool LockedInput = false;
    private Vector2 TorchDirectionAxis, TorchDirection;
    private List<GameObject> GhostsKilled;
    private GameObject TorchGameObject;

    private void Start()
    {
        TorchDirection = transform.position;
        GhostsKilled = new List<GameObject>();
        TorchBatteryCharge = TorchBatteryChargeStart;
        TorchGameObject = transform.GetChild(2).gameObject;
        transform.GetChild(3).GetChild(0).GetComponent<SpriteRenderer>().color = GetComponent<PlayerMovement>().PlayerColor;
        transform.GetChild(3).GetChild(1).GetComponent<SpriteRenderer>().color = GetComponent<PlayerMovement>().PlayerColor;
    }

    private void Update()
    {
        HandleInput();
        HandleTorch();
        HandlePickups();
        HandleTorchKill();
        HandleDirection();
    }

    private void HandleInput()
    {
        Player.InputType playerInputType = GetComponent<PlayerMovement>().PlayerInputType;
        Player.InputSide playerInputSide = GetComponent<PlayerMovement>().PlayerInputSide;
        bool lastTorchEnabled = TorchEnabled;
        if (!LockedInput)
        {
            if(playerInputType == Player.InputType.Keyboard)
            {
                string buttonName = Player.GetFullAxisName("TorchSwitch", playerInputType, playerInputSide);
                TorchEnabled = Input.GetButton(buttonName);
            }
            else
            {
                string axisName = Player.GetFullAxisName("TorchSwitch", playerInputType, playerInputSide);
                TorchEnabled = Input.GetAxis(axisName) > 0f;
            }
        }
        if(lastTorchEnabled != TorchEnabled && !GetComponent<AudioSource>().isPlaying)
        {
            GetComponent<AudioSource>().clip = TorchSoundClip;
            GetComponent<AudioSource>().Play();
        }
    }

    private void HandleDirection()
    {
        if (!GetComponent<PlayerMovement>().MovementControlsTorch)
        {
            float rotationZ = Mathf.Atan2(TorchDirection.y, TorchDirection.x) * Mathf.Rad2Deg;
            TorchGameObject.transform.localRotation = Quaternion.Euler(new Vector3(0, 0, rotationZ));
            transform.GetChild(0).localRotation = Quaternion.Euler(new Vector3(0, 0, rotationZ));
        }
    }

    private void HandleTorch()
    {
        bool torchActive = TorchEnabled && TorchBatteryCharge > 0;
        TorchGameObject.SetActive(torchActive);
        Sprite torchSprite = torchActive ? TorchEnabledSprite : TorchDisabledSprite;
        transform.GetChild(0).GetComponent<SpriteRenderer>().sprite = torchSprite;
        if (torchActive)
        {
            TorchBatteryCharge -= TorchBatteryLosePerSec * Time.deltaTime;
        }
        if (TorchBatteryCharge <= 0 && TorchEnabled)
        {
            TorchEnabled = false;
            if (!TorchEmptyAnnounced)
            {
                TorchEmptyAnnounced = true;
                Color playerColor = GetComponent<PlayerMovement>().PlayerColor;
                FindObjectOfType<GameHandler>().ShowAnnouncerText("Your torch is out of battery. Pick up another one", playerColor, false, 2, () =>
                {
                    TorchEmptyAnnounced = false;
                });
            }
        }
        TorchBatteryCharge = Mathf.Max(TorchBatteryCharge, 0);
    }

    private void HandlePickups()
    {
        if (TorchBatteryCharge <= 20 && !BatteryPickupSpawned)
        {
            SpawnBatteryItem();
        }
        if (BatteryPickupSpawned)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            Vector3 direction = player.MovementControlsTorch ? player.MoveDirection : TorchDirection;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 0.1f, LayerMask.GetMask("Pickups"));
            Debug.DrawRay(transform.position, direction * 0.1f, Color.green, 0.1f);
            if (hit.collider != null)
            {
                if (!GetComponent<AudioSource>().isPlaying || GetComponent<AudioSource>().clip == TorchSoundClip)
                {
                    GetComponent<AudioSource>().clip = BatteryPickupClip;
                    GetComponent<AudioSource>().Play();
                }
                hit.collider.gameObject.SetActive(false);
                hit.collider.transform.SetParent(transform);
                TorchBatteryCharge = TorchBatteryChargeStart;
                BatteryPickupSpawned = false;
            }
        }
    }

    private void HandleTorchKill()
    {
        if (TorchEnabled && TorchBatteryCharge > 0)
        {
            PlayerMovement player = GetComponent<PlayerMovement>();
            Vector3 direction = player.MovementControlsTorch ? player.MoveDirection : TorchDirection; 
            Vector3 origin = transform.position + direction * 0.5f;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, TorchHitboxSize, LayerMask.GetMask("Ghosts"));
            Debug.DrawRay(origin, direction * TorchHitboxSize, Color.green, 0.1f);
            bool collidedGhostHit = hit.collider != null && !GhostsKilled.Contains(hit.collider.gameObject);
            if (collidedGhostHit)
            {
                if (!GetComponent<AudioSource>().isPlaying || GetComponent<AudioSource>().clip == TorchSoundClip)
                {
                    GetComponent<AudioSource>().clip = GhostDeathClip;
                    GetComponent<AudioSource>().Play();
                }
                GhostsKilledCount++;
                FindObjectOfType<GameHandler>().StrikeLightnings();
                StartCoroutine(StopAndKillGhost(hit.collider.gameObject, 1, direction));
            }
        }
    }

    private IEnumerator StopAndKillGhost(GameObject ghost, float timeToKillGhost, Vector3 direction)
    {
        GetComponent<PlayerMovement>().SetLockInput(true);
        LockedInput = true;
        GetComponent<PlayerMovement>().HandleTrackSnapping(() => {
            Vector3 newGhostPosition = transform.position + direction;
            StartCoroutine(ghost.GetComponent<GhostMovement>().SlideToPosition(newGhostPosition));
        });
        GhostsKilled.Add(ghost.gameObject);
        ghost.GetComponent<GhostMovement>().ActiveGhost = false;
        Color playerColor = GetComponent<PlayerMovement>().PlayerColor;
        FindObjectOfType<GameHandler>().ShowAnnouncerText("Nice! You killed a Ghost", playerColor, false, 2);
        yield return new WaitForSeconds(timeToKillGhost);
        ghost.GetComponent<GhostMovement>().KillGhost();
        GhostsKilled.Remove(ghost);
        GetComponent<PlayerMovement>().SetLockInput(false);
        LockedInput = false;
    }

    private void SpawnBatteryItem()
    {
        GameObject batteryItem = transform.GetChild(3).gameObject;
        BatteryPickupSpawned = true;
        PathPoint[] availableSpawnPositions = FindObjectsOfType<PathPoint>();
        Vector3 randomSpawnPosition = availableSpawnPositions[Random.Range(0, availableSpawnPositions.Length)].transform.position;
        batteryItem.transform.SetParent(null);
        batteryItem.transform.position = randomSpawnPosition;
        batteryItem.transform.rotation = Quaternion.Euler(Vector3.zero);
        batteryItem.SetActive(true);
    }
}
