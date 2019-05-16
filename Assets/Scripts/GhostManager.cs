using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostManager : MonoBehaviour
{
    [SerializeField] private int StartGhostCount;
    [SerializeField] private GameObject GhostPrefab;
    [SerializeField] private PathPoint[] SpawnLocations;
    [SerializeField] private Transform GhostsParent;

    public void StartGame()
    {
        for (int i = 0; i < StartGhostCount; i++)
        {
            StartCoroutine(SpawnGhost(1));
        }
    }

    public IEnumerator SpawnGhost(float timeToSpawn)
    {
        yield return new WaitForSeconds(timeToSpawn);
        PathPoint targetPathPoint = FindGhostSpawnPoint();
        GameObject ghost = Instantiate(GhostPrefab, targetPathPoint.transform.position, Quaternion.identity, GhostsParent);
        ghost.GetComponent<GhostMovement>().SetTargetPathPoint(targetPathPoint);
        FindObjectOfType<GameHandler>().StrikeLightnings();
    }

    private PathPoint FindGhostSpawnPoint()
    {
        float minimumDistanceFromPlayersAndGhosts = 3f;
        List<PathPoint> validSpawnPoints = new List<PathPoint>();
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();
        GhostMovement[] ghosts = FindObjectsOfType<GhostMovement>();
        foreach (PathPoint point in SpawnLocations)
        {
            bool valid = true;
            foreach (PlayerMovement player in players)
            {
                if (Vector3.Distance(point.transform.position, player.transform.position) <= minimumDistanceFromPlayersAndGhosts)
                {
                    valid = false;
                }
            }
            foreach (GhostMovement ghost in ghosts)
            {
                if (Vector3.Distance(point.transform.position, ghost.transform.position) <= minimumDistanceFromPlayersAndGhosts)
                {
                    valid = false;
                }
            }
            if (valid)
            {
                validSpawnPoints.Add(point);
            }
        }
        return validSpawnPoints[UnityEngine.Random.Range(0, validSpawnPoints.Count)];
    }

    public void DestroyGhosts()
    {
        foreach (GhostMovement ghost in FindObjectsOfType<GhostMovement>())
        {
            Destroy(ghost.gameObject);
        }
    }
}
