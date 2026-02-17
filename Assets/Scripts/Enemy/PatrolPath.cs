using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a patrol path for AI.
/// Attach this to an empty GameObject and add child objects as waypoints.
/// </summary>
public class PatrolPath : MonoBehaviour
{
    [SerializeField] private Color debugColor = Color.cyan;
    [SerializeField] private float waypointRadius = 0.3f;
    
    private List<Transform> waypoints = new List<Transform>();

    private void Awake()
    {
        // Auto-find children as waypoints
        RefreshWaypoints();
    }

    public void RefreshWaypoints()
    {
        waypoints.Clear();
        foreach (Transform child in transform)
        {
            if (child != transform)
                waypoints.Add(child);
        }
    }

    public Transform GetWaypoint(int index)
    {
        if (waypoints.Count == 0) RefreshWaypoints();
        if (waypoints.Count == 0) return null;
        
        return waypoints[index % waypoints.Count];
    }

    public int GetNextIndex(int currentIndex)
    {
        if (waypoints.Count == 0) return 0;
        return (currentIndex + 1) % waypoints.Count;
    }
    
    public int GetClosestWaypointIndex(Vector3 position)
    {
        if (waypoints.Count == 0) RefreshWaypoints();
        if (waypoints.Count == 0) return -1;

        int closestIndex = 0;
        float minDst = float.MaxValue;

        for (int i = 0; i < waypoints.Count; i++)
        {
            float dst = Vector3.Distance(position, waypoints[i].position);
            if (dst < minDst)
            {
                minDst = dst;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = debugColor;
        RefreshWaypoints();

        for (int i = 0; i < waypoints.Count; i++)
        {
            Gizmos.DrawSphere(waypoints[i].position, waypointRadius);
            
            if (i > 0)
            {
                Gizmos.DrawLine(waypoints[i-1].position, waypoints[i].position);
            }
        }
        
        // Loop line
        if (waypoints.Count > 1)
        {
            Gizmos.DrawLine(waypoints[waypoints.Count-1].position, waypoints[0].position);
        }
    }
}
