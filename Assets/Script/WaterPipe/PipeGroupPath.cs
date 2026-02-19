using PGroup;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class PipeGroupPath : MonoBehaviour
{
    // Stores the list of paths (each path is a list of points)
    public List<List<Vector2>> savedPaths = new List<List<Vector2>>();
    public Transform spawnPoint; // Re-added to fix CS0103
    
    [Header("Settings")]
    public float spawnOffset = 0.15f; // User adjustable offset (0.0 - 0.5)

    [Header("PGroup")]
    private int currentWaterFlow;

    public void StartWaterFlow()
    {
        currentWaterFlow = 0;
        StopAllCoroutines();
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).GetComponent<PipeController>())
            {
                transform.GetChild(i).GetComponent<PipeController>().ResetWater();
            }
        }
        StartCoroutine(DelayWaterFlowStep());
    }
    private IEnumerator DelayWaterFlowStep()
    {
        while (currentWaterFlow < transform.childCount)
        {
            if (transform.GetChild(currentWaterFlow).GetComponent<PipeController>())
            {
                transform.GetChild(currentWaterFlow).GetComponent<PipeController>().StartWaterFlow(true);
                yield return new WaitForSeconds(2);
                currentWaterFlow++;
            }
            else currentWaterFlow++;
        }
    }
    public void FlipDirectionPipe()
    {
        for (int i = 0; i < transform.childCount; i++) 
        {
            if (transform.GetChild(i).GetComponent<PipeController>())
            {
                if (transform.GetChild(i).localEulerAngles.z == 0)
                {
                    transform.GetChild(i).localEulerAngles = new Vector3(0, 0, 180);
                }
                else if (transform.GetChild(i).localEulerAngles.z == 90)
                {
                    transform.GetChild(i).localEulerAngles = new Vector3(0, 0, -90);
                }
                else if (transform.GetChild(i).localEulerAngles.z == 270)
                {
                    transform.GetChild(i).localEulerAngles = new Vector3(0, 0, 90);
                }
                else
                {
                    transform.GetChild(i).localEulerAngles = Vector3.zero;
                }
            }
        }
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).SetAsFirstSibling();
        }
        StartWaterFlow();
    }


    public void SetupWaterFlow(bool value)
    {
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<PipeController>())
                {
                    transform.GetChild(i).GetComponent<PipeController>().StartWaterFlow(value);
                    /*if(i == transform.childCount - 1)
                    {
                        transform.GetChild(i).transform.localEulerAngles = transform.GetChild(i - 1).transform.localEulerAngles;
                    }
                    if (i > 0)
                    {
                        if (transform.GetChild(i - 1).GetComponent<PipeController>())
                        {
                            if (transform.GetChild(i).transform.position.x == transform.GetChild(i - 1).transform.position.x ||
                                transform.GetChild(i).transform.position.y == transform.GetChild(i - 1).transform.position.y)
                            {
                                transform.GetChild(i).transform.localEulerAngles = transform.GetChild(i - 1).transform.localEulerAngles;
                            }
                        }
                    }*/
                }
            }
        }
    }
    public void ChangeAllWaterColor(int num)
    {
        if (transform.childCount > 0)
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).GetComponent<PipeController>())
                {
                    transform.GetChild(i).GetComponent<PipeController>().SetColorWater(num);
                }
            }
        }
    }
    public void Initialize(List<List<Vector2>> paths)
    {
        savedPaths = new List<List<Vector2>>();
        if (paths == null) return;

        foreach (var segment in paths)
        {
            if (segment == null || segment.Count < 2) continue;

            // Attempt to merge with an existing path
            bool merged = false;
            foreach (var existingPath in savedPaths)
            {
                // Check if existingPath ends exactly where the new segment starts
                if (existingPath.Count > 0 && Vector2.Distance(existingPath[existingPath.Count - 1], segment[0]) < 0.01f)
                {
                    // Append points from segment (skipping the first one as it overlaps)
                    for (int i = 1; i < segment.Count; i++)
                    {
                        existingPath.Add(segment[i]);
                    }
                    merged = true;
                    break;
                }
                // Check if existingPath starts exactly where the new segment ends (Prepend case)
                else if (existingPath.Count > 0 && Vector2.Distance(existingPath[0], segment[segment.Count - 1]) < 0.01f)
                {
                    // Prepend points from segment (skipping the last one as it overlaps)
                    for (int i = segment.Count - 2; i >= 0; i--)
                    {
                        existingPath.Insert(0, segment[i]);
                    }
                    merged = true;
                    break;
                }
            }

            if (!merged)
            {
                savedPaths.Add(new List<Vector2>(segment));
            }
        }
    }

    public bool isFlowing = false;
    private Coroutine flowCoroutine;

    public void ToggleFlow(GameObject waterPrefab, float interval = 0.5f)
    {
        isFlowing = !isFlowing;
        
        if (isFlowing)
        {
            if (flowCoroutine == null)
                flowCoroutine = StartCoroutine(SpawnRoutine(waterPrefab, interval));
        }
        else
        {
            if (flowCoroutine != null)
            {
                StopCoroutine(flowCoroutine);
                flowCoroutine = null;
            }
        }
    }

    IEnumerator SpawnRoutine(GameObject waterPrefab, float interval)
    {
        while (isFlowing)
        {
            SpawnWater(waterPrefab);
            yield return new WaitForSeconds(interval);
        }
    }

    void SpawnWater(GameObject waterPrefab)
    {
        // Spawn only on the FIRST valid path
        foreach (var path in savedPaths)
        {
            if (path != null && path.Count > 0)
            {
                SpawnWaterOnPath(waterPrefab, path, path[0]);
                return; // Spawn only once
            }
        }
    }

    // Public method to spawn water on a specific path from a specific position
    public void SpawnWaterOnPath(GameObject waterPrefab, List<Vector2> path, Vector2 startPosition)
    {
        if (path == null || path.Count == 0) return;

        // Create a copy of the path
        List<Vector2> runPath = new List<Vector2>(path);

        // Extend the start to the pipe opening (Spawn from tip)
        if (runPath.Count >= 2)
        {
            Vector2 dir = (runPath[1] - runPath[0]).normalized;
            float offsetDist = Vector2.Distance(runPath[0], runPath[1]) * spawnOffset;
            Vector2 newStart = runPath[0] - dir * offsetDist;
            runPath.Insert(0, newStart);
        }

        GameObject water = Instantiate(waterPrefab, runPath[0], Quaternion.identity);
        WaterWalker walker = water.GetComponent<WaterWalker>();
        if (walker != null)
        {
            walker.Initialize(this, waterPrefab);
            walker.BeginJourney(runPath);
        }
    }

    // Find all paths that start near a given position (for branching)
    public List<List<Vector2>> FindConnectedPathsAt(Vector2 position, float tolerance = 0.2f)
    {
        List<List<Vector2>> connectedPaths = new List<List<Vector2>>();

        foreach (var path in savedPaths)
        {
            if (path != null && path.Count > 0)
            {
                // Check if this path starts near the given position
                if (Vector2.Distance(path[0], position) < tolerance)
                {
                    connectedPaths.Add(path);
                }
            }
        }

        return connectedPaths;
    }
}