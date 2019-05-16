using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathPoint : MonoBehaviour
{
    [SerializeField] private PathPoint[] AdjacentPathPoints;

    public PathPoint GetRandomAdjacentPathPoint(PathPoint lastPoint, float percentageGoingLast)
    {
        if (lastPoint == null)
        {
            return AdjacentPathPoints[Random.Range(0, AdjacentPathPoints.Length)];
        }
        bool goingLast = Random.Range(0f, 1f) <= percentageGoingLast;
        if (goingLast || AdjacentPathPoints.Length == 1)
        {
            return lastPoint;
        }
        return GetRandomAdjacentPathPointNoLast(lastPoint);
    }

    private PathPoint GetRandomAdjacentPathPointNoLast(PathPoint lastPoint)
    {
        PathPoint[] adjacentPointsNoLast = new PathPoint[AdjacentPathPoints.Length - 1];
        int i = 0;
        foreach (PathPoint point in AdjacentPathPoints)
        {
            if (point.transform != lastPoint.transform)
            {
                adjacentPointsNoLast[i] = point;
                i++;
            }
        }
        if (adjacentPointsNoLast.Length == 0) return lastPoint;
        else return adjacentPointsNoLast[Random.Range(0, adjacentPointsNoLast.Length)];
    }
}
