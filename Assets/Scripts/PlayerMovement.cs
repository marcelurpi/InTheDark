using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [HideInInspector] public Vector2 MoveDirection;

    public int PlayerLives;
    public Player.InputType PlayerInputType;
    public Player.InputSide PlayerInputSide;
    public Color PlayerColor;
    public bool MovementControlsTorch = true;

    [SerializeField] private float PlayerSpeed, PlayerHitboxSize;
    [SerializeField] private AudioClip PlayerDeathClip;

    private bool LockedInput = false;
    private Vector2 MoveAxis;

    public void SetColorAndLayer(int i)
    {
        transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().color = PlayerColor;
        transform.GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = i * 3;
        transform.GetChild(0).GetChild(0).GetComponent<SpriteRenderer>().sortingOrder = i * 3 + 1;
        transform.GetChild(2).GetChild(1).GetComponent<SpriteRenderer>().sortingOrder = i * 3 + 2;
    }

    private void Update()
    {
        HandleInput();
        HandleDirection();
        HandleMovement();
        HandleTrackSnapping();
    }

    private void HandleInput()
    {
        if (!LockedInput)
        {
            string horizontalAxisName = Player.GetFullAxisName("MoveHorizontal", PlayerInputType, PlayerInputSide);
            string verticalAxisName = Player.GetFullAxisName("MoveVertical", PlayerInputType, PlayerInputSide);
            float horizontal = Input.GetAxis(horizontalAxisName);
            float vertical = Input.GetAxis(verticalAxisName);
            Vector2 vectHorizontal = new Vector2(Mathf.RoundToInt(horizontal), 0);
            Vector2 vectVertical = new Vector2(0, Mathf.RoundToInt(vertical));
            MoveAxis = Mathf.Abs(horizontal) > Mathf.Abs(vertical) ? vectHorizontal : vectVertical;
            MoveDirection = MoveAxis != Vector2.zero ? MoveAxis : MoveDirection;
        }
        else
        {
            MoveAxis = Vector2.zero;
        }
    }

    public void SetLockInput(bool value)
    {
        LockedInput = value;
        MoveAxis = Vector2.zero;
    }

    private void HandleDirection()
    {
        if (MovementControlsTorch)
        {
            float rotationZ = Mathf.Atan2(MoveDirection.y, MoveDirection.x) * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(new Vector3(0, 0, rotationZ));
        }
    }

    private void HandleMovement()
    {
        if (MoveAxis != Vector2.zero)
        {
            Vector3 origin1 = transform.position + new Vector3(MoveAxis.y, MoveAxis.x) * PlayerHitboxSize * 0.5f;
            Vector3 origin2 = transform.position - new Vector3(MoveAxis.y, MoveAxis.x) * PlayerHitboxSize * 0.5f;
            RaycastHit2D hit1 = Physics2D.Raycast(origin1, MoveAxis, 0.5f, LayerMask.GetMask("Obstacles"));
            RaycastHit2D hit2 = Physics2D.Raycast(origin2, MoveAxis, 0.5f, LayerMask.GetMask("Obstacles"));
            Debug.DrawRay(transform.position + new Vector3(MoveAxis.y, MoveAxis.x) * PlayerHitboxSize * 0.5f, MoveAxis, Color.green, 0.1f);
            Debug.DrawRay(transform.position - new Vector3(MoveAxis.y, MoveAxis.x) * PlayerHitboxSize * 0.5f, MoveAxis, Color.green, 0.1f);
            if (hit1.collider == null && hit2.collider == null)
            {
                transform.position += (Vector3)MoveAxis * PlayerSpeed * Time.deltaTime;
            }
        }
    }

    public void HandleTrackSnapping(Action actionAfterSnap = null)
    {
        float positionX = MoveAxis.x == 0 ? Mathf.Round(transform.position.x) : transform.position.x;
        float positionY = MoveAxis.y == 0 ? Mathf.Round(transform.position.y) : transform.position.y;
        StartCoroutine(SlideToPosition(new Vector3(positionX, positionY), actionAfterSnap));
    }

    private IEnumerator SlideToPosition(Vector3 position, Action actionAfterSlide = null)
    {
        while (Vector3.Distance(transform.position, position) > 0.05f)
        {
            Vector3 direction = (position - transform.position).normalized;
            transform.position += direction * PlayerSpeed * 0.1f * Time.deltaTime;
            yield return null;
        }
        transform.position = position;
        actionAfterSlide?.Invoke();
    }

    public void KillPlayer()
    {
        if (!GetComponent<AudioSource>().isPlaying)
        {
            GetComponent<AudioSource>().clip = PlayerDeathClip;
            GetComponent<AudioSource>().Play();
        }
        transform.position = Vector2.zero;
        if (PlayerLives > 1)
        {
            PlayerLives--;
            FindObjectOfType<GameHandler>().ShowAnnouncerText("Oops! A Ghost killed you", PlayerColor, false, 2);
        }
        else
        {
            FindObjectOfType<GameHandler>().GameOver();
        }
    }
}
