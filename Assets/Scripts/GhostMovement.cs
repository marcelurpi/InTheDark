using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    [HideInInspector] public bool ActiveGhost = true;

    [SerializeField] private float GhostSpeed, GhostHitboxSize;

    private PathPoint LastPathPoint, TargetPathPoint;

    public void SetTargetPathPoint(PathPoint targetPathPoint)
    {
        TargetPathPoint = targetPathPoint;
    }

    private void Update()
    {
        if (ActiveGhost)
        {
            HandleMovement();
            HandleAttack();
        }
    }

    private void HandleMovement()
    {
        if (Vector3.Distance(transform.position, TargetPathPoint.transform.position) > 0.05f)
        {
            Vector3 direction = (TargetPathPoint.transform.position - transform.position).normalized;
            transform.position += direction * GhostSpeed * Time.deltaTime;
        }
        else
        {
            PathPoint tempTargetPathPoint = TargetPathPoint;
            TargetPathPoint = TargetPathPoint.GetRandomAdjacentPathPoint(LastPathPoint, 0.1f);
            LastPathPoint = tempTargetPathPoint;
        }
    }

    private void HandleAttack()
    {
        Vector2[] directions = new Vector2[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        foreach (Vector2 direction in directions)
        {
            RaycastPlayer(direction);
        }
    }

    private void RaycastPlayer(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, GhostHitboxSize / 2, LayerMask.GetMask("Players"));
        Debug.DrawRay(transform.position, direction * GhostHitboxSize / 2, Color.green, 0.1f);
        if (hit.collider != null && ActiveGhost)
        {
            FindObjectOfType<GameHandler>().StrikeLightnings();
            hit.collider.GetComponent<PlayerMovement>().KillPlayer();
        }
    }

    public void KillGhost()
    {
        transform.position = new Vector3(999, 999);
        StartCoroutine(FindObjectOfType<GhostManager>().SpawnGhost(3));
        Destroy(gameObject);
        ActiveGhost = false;
    }

    public IEnumerator SlideToPosition(Vector3 position)
    {
        while (Vector3.Distance(transform.position, position) > 0.05f)
        {
            Vector3 direction = (position - transform.position).normalized;
            transform.position += direction * GhostSpeed * Time.deltaTime;
            yield return null;
        }
        transform.position = position;
    }
}
