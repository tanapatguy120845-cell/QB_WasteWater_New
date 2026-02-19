using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterWalker : MonoBehaviour
{
    public float speed = 2.0f;
    // Offset Removed per user request
    
    private List<Vector2> waypoints;
    private int targetIndex = 0;
    
    // Lane Keeping Data
    private float lateralOffset = 0;
    private float forwardOffset = 0;

    // Dynamic Branching
    private PipeGroupPath pipeGroupPath;
    private GameObject waterPrefab;
    private HashSet<Vector2> spawnedJunctions = new HashSet<Vector2>();


    public void Initialize(PipeGroupPath pathData, GameObject prefab)
    {
        pipeGroupPath = pathData;
        waterPrefab = prefab;
    }

    public void BeginJourney(List<Vector2> path)
    {
        waypoints = path;
        targetIndex = 0;
        
        if (waypoints != null && waypoints.Count > 1)
        {
            // 1. Calculate the initial offset relative to the first segment
            // We TRUST the current position (SpawnPoint) completely. No snapping.
            Vector3 startNode = waypoints[0];
            Vector3 currentPos = transform.position; 
            Vector3 diff = currentPos - startNode;

            Vector3 firstSegDir = (waypoints[1] - waypoints[0]).normalized;
            // Left Normal (90 deg Rot) relative to flow
            Vector3 firstSegLeft = new Vector3(-firstSegDir.y, firstSegDir.x, 0); 

            // Project diff onto Normal (Lateral) and Dir (Forward)
            // Lateral = How far left/right of the center line?
            lateralOffset = Vector3.Dot(diff, firstSegLeft);
            
            // Forward = How far ahead/behind the logical start point?
            // e.g. If marker is at Pipe Head (Edge) and StartNode is Center, this captures that difference.
            forwardOffset = Vector3.Dot(diff, firstSegDir); 
            
            targetIndex = 1; 
        }
        else if (waypoints != null && waypoints.Count == 1)
        {
             // Single point case fallback
             transform.position = waypoints[0];
        }
    }

    void Update()
    {
        if (waypoints == null || targetIndex >= waypoints.Count)
        {
            Destroy(gameObject); 
            return;
        }

        Vector2 pA = waypoints[targetIndex - 1]; // Start of current segment
        Vector2 pB = waypoints[targetIndex];     // End of current segment
        
        Vector3 segDir = (pB - pA).normalized;
        Vector3 segNormal = new Vector3(-segDir.y, segDir.x, 0);

        // Calculate Target Position with Lane Offset
        // We apply the SAME relative offset we found at the start
        Vector3 worldOffset = (segNormal * lateralOffset); 
        // Note: forwardOffset is usually not applied to the target itself constantly, 
        // but if we want to shift the whole path longitudinally, we can add it.
        // However, standard "Lane" logic implies lateral shift only. 
        // Let's add it to respect the spawn point exactly.
        worldOffset += (segDir * forwardOffset); 

        // Target Position
        Vector3 target = (Vector3)pB + worldOffset;
        
        // Calculate Direction to target
        Vector3 moveDir = target - transform.position;
        
        // Rotation (Face the movement direction)
        if (moveDir != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // Move
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

        // Check distance
        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            targetIndex++;
            
            // Check for branching paths at this waypoint
            if (pipeGroupPath != null && waterPrefab != null && targetIndex < waypoints.Count)
            {
                Vector2 currentWaypoint = waypoints[targetIndex - 1];
                
                // Only check if we haven't already spawned at this junction
                if (!spawnedJunctions.Contains(currentWaypoint))
                {
                    spawnedJunctions.Add(currentWaypoint);
                    
                    // Find connected paths at this position
                    List<List<Vector2>> connectedPaths = pipeGroupPath.FindConnectedPathsAt(currentWaypoint);
                    
                    // Spawn water on each connected path (except the one we're already on)
                    foreach (var connectedPath in connectedPaths)
                    {
                        // Check if this is a different path (not the one we're currently on)
                        if (connectedPath.Count > 0 && !IsCurrentPath(connectedPath))
                        {
                            pipeGroupPath.SpawnWaterOnPath(waterPrefab, connectedPath, currentWaypoint);
                        }
                    }
                }
            }
        }
    }
    
    // Helper method to check if a path is the one we're currently on
    private bool IsCurrentPath(List<Vector2> path)
    {
        if (waypoints == null || path == null || waypoints.Count < 2 || path.Count < 2)
            return false;
        
        // Simple check: if the next waypoint matches the second point of the path
        if (targetIndex < waypoints.Count)
        {
            return Vector2.Distance(waypoints[targetIndex], path[1]) < 0.1f;
        }
        
        return false;
    }
}