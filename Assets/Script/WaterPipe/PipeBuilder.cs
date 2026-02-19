using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipeBuilder : MonoBehaviour
{
    [Header("Layer Settings")]
    public string pipeLayerName = "Default"; // User can change this to "Pipe"

    [Header("Prefabs")]
    public GameObject straightPipePrefab;   // 2-way straight
    public GameObject cornerPipePrefab;     // 2-way turn
    public GameObject threeWayPipePrefab;   // 3-way T
    public GameObject fourWayPipePrefab;    // 4-way Cross

    [Header("Settings")]
    public float gridSize = 1f;
    public float defaultSpawnOffset = 0.15f; // Global default for new pipes

    [Header("Rotation Offsets")]
    public float straightRotationOffset = 0f;
    public float cornerRotationOffset = 0f;
    public float threeWayRotationOffset = 0f;
    public float fourWayRotationOffset = 0f;

    [Header("Connector Scale Settings")]
    [Tooltip("Scale for corner connectors (affects only connectors, not straight pipes)")]
    public Vector3 cornerPipeScale = Vector3.one;
    [Tooltip("Scale for 3-way T-junction connectors")]
    public Vector3 threeWayPipeScale = Vector3.one;
    [Tooltip("Scale for 4-way cross connectors")]
    public Vector3 fourWayPipeScale = Vector3.one;

    [Header("Connector Position Offset")]
    [Tooltip("Position offset for corner connectors (use to fix alignment issues)")]
    public Vector2 cornerPipeOffset = Vector2.zero;
    [Tooltip("Position offset for 3-way T-junction connectors")]
    public Vector2 threeWayPipeOffset = Vector2.zero;
    [Tooltip("Position offset for 4-way cross connectors")]
    public Vector2 fourWayPipeOffset = Vector2.zero;

    // Grid System: Key = Position, Value = Connection Mask (Bitmask)
    // 1: Right, 2: Up, 4: Left, 8: Down
    private Dictionary<Vector2, int> gridConnections = new Dictionary<Vector2, int>();
    private Dictionary<Vector2, GameObject> gridObjects = new Dictionary<Vector2, GameObject>();
    
    private Transform currentBatchContainer;

    public void BuildPipeBatch(List<List<Vector2>> paths)
    {
        if (paths == null || paths.Count == 0) return;

        // Create a new container for this entire batch
        GameObject groupObj = new GameObject("PipeGroup_" + _groupIndex++);
        groupObj.transform.parent = this.transform;
        currentBatchContainer = groupObj.transform;
        
        // Add Path Data Component and Initialize
        PipeGroupPath pathData = groupObj.AddComponent<PipeGroupPath>();
        pathData.spawnOffset = defaultSpawnOffset; // Apply global default
        pathData.Initialize(paths);

        HashSet<Vector2> pointsToUpdate = new HashSet<Vector2>();

        // 1. Update Connection Data for ALL paths in the batch
        foreach (var points in paths)
        {
            Debug.Log(points);
            if (points == null || points.Count < 2) continue;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Vector2 p1 = points[i];
                Vector2 p2 = points[i+1];
                List<Vector2> intermediatePoints = AddConnectionBetween(p1, p2);
                pointsToUpdate.UnionWith(intermediatePoints);
            }
            
             // Also add original points and their neighbors
            foreach(var p in points)
            {
                Debug.Log(p);
                pointsToUpdate.Add(p);
                pointsToUpdate.Add(Snap(p)); 
                pointsToUpdate.Add(Snap(p + Vector2.right * gridSize));
                pointsToUpdate.Add(Snap(p + Vector2.left * gridSize));
                pointsToUpdate.Add(Snap(p + Vector2.up * gridSize));
                pointsToUpdate.Add(Snap(p + Vector2.down * gridSize));
            }
        }

        // 2. Refresh Visuals
        foreach(var p in pointsToUpdate)
        {
            UpdatePipeVisualAt(p);
        }
        
        currentBatchContainer = null; // Reset
    }
    
    private int _groupIndex = 1;

    // Kept for backward compatibility if needed, calling Batch with single item
    public void BuildPipePath(List<Vector2> points)
    {
        BuildPipeBatch(new List<List<Vector2>> { points });
    }

    private List<Vector2> AddConnectionBetween(Vector2 start, Vector2 end)
    {
        List<Vector2> affectedPoints = new List<Vector2>();
        
        // Simple DDA or Step walker to handle straight lines larger than 1 unit
        Vector2 dir = (end - start).normalized;
        float dist = Vector2.Distance(start, end);
        int steps = Mathf.RoundToInt(dist / gridSize);
        
        for (int i = 0; i < steps; i++)
        {
            Vector2 current = start + dir * (i * gridSize);
            Vector2 next = start + dir * ((i + 1) * gridSize);
            
            // Quantize to grid to be safe
            current = Snap(current);
            next = Snap(next);
            
            Connect(current, next);
            
            affectedPoints.Add(current);
            affectedPoints.Add(next);
        }
        
        return affectedPoints;
    }

    private void Connect(Vector2 a, Vector2 b)
    {
        if (!gridConnections.ContainsKey(a)) gridConnections[a] = 0;
        if (!gridConnections.ContainsKey(b)) gridConnections[b] = 0;

        Vector2 dir = (b - a).normalized;
        
        // Add connection A->B
        int dirMaskA = GetDirectionMask(dir);
        gridConnections[a] |= dirMaskA;
        
        // Add connection B->A (Opposite)
        int dirMaskB = GetDirectionMask(-dir);
        gridConnections[b] |= dirMaskB;
    }
    
    private int GetDirectionMask(Vector2 dir)
    {
        // 1: Right, 2: Up, 4: Left, 8: Down
        if (dir.x > 0.5f) return 1;  // Right
        if (dir.y > 0.5f) return 2;  // Up
        if (dir.x < -0.5f) return 4; // Left
        if (dir.y < -0.5f) return 8; // Down
        return 0;
    }

    private void UpdatePipeVisualAt(Vector2 pos)
    {
        pos = Snap(pos);
        if (!gridConnections.ContainsKey(pos)) return;

        int mask = gridConnections[pos];
        if (mask == 0) return; // No connections?

        // Determine Parent Logic:
        // If replacing an existing pipe, keep its parent (so it stays in its original Group).
        // If it's a new pipe, use currentBatchContainer.
        Transform targetParent = currentBatchContainer;
        
        if (gridObjects.ContainsKey(pos))
        {
            if (gridObjects[pos] != null) 
            {
                targetParent = gridObjects[pos].transform.parent;
                Destroy(gridObjects[pos]);
            }
            gridObjects.Remove(pos);
        }
        
        // Fallback if something went wrong (e.g. modifying old pipe outside of batch context?)
        // If currentBatchContainer is null (maybe just verifying?), use generated pipes holder?
        // But for this specific logic, targetParent should be valid if exists, or batch if new.
        if (targetParent == null) return; // Should not happen for new pipes

        GameObject prefab = null;
        float rotation = 0;
        
        // Count neighbors
        int count = 0;
        if ((mask & 1) != 0) count++;
        if ((mask & 2) != 0) count++;
        if ((mask & 4) != 0) count++;
        if ((mask & 8) != 0) count++;
        
        if (count == 1)
        {
            // End Cap / Dead End -> Treat as Straight (or specific End Cap)
            prefab = straightPipePrefab;
            // Align with the single connection
            if ((mask & 1) != 0) rotation = 0;   // Right
            if ((mask & 2) != 0) rotation = 90;  // Up
            if ((mask & 4) != 0) rotation = 180; // Left
            if ((mask & 8) != 0) rotation = 270; // Down
        }
        else if (count == 2)
        {
            // Straight or Corner
            if ((mask & 1) != 0 && (mask & 4) != 0) // Right & Left (Horizontal)
            {
                prefab = straightPipePrefab;
                rotation = 0;
            }
            else if ((mask & 2) != 0 && (mask & 8) != 0) // Up & Down (Vertical)
            {
                prefab = straightPipePrefab;
                rotation = 90;
            }
            else
            {
                // Corner
                prefab = cornerPipePrefab;
                // Corner Prefab Default: Down & Right (8 & 1) at Rot 0
                
                if ((mask & 8) != 0 && (mask & 1) != 0) rotation = 0;   // Down-Right
                if ((mask & 1) != 0 && (mask & 2) != 0) rotation = 90;  // Right-Up
                if ((mask & 2) != 0 && (mask & 4) != 0) rotation = 180; // Up-Left
                if ((mask & 4) != 0 && (mask & 8) != 0) rotation = 270; // Left-Down
            }
        }
        else if (count == 3)
        {
            // 3-Way (T-Shape)
            // Default 3-Way Prefab: Connects Down, Right, Left? (T pointing Down?)
            // Or Left, Up, Right (T pointing Up)?
            // STANDARD T-SHAPE often points DOWN (Left-Right-Down) at 0 rot.
            // Let's assume: Down (8) + Right (1) + Left (4) = Mask 13.
            
            prefab = threeWayPipePrefab;
            
            // Check missing side to determine rotation
            if ((mask & 2) == 0) rotation = 0;   // Missing Up -> Point Down (Left-Right-Down)
            if ((mask & 4) == 0) rotation = 90;  // Missing Left -> Point Right (Up-Down-Right)
            if ((mask & 8) == 0) rotation = 180; // Missing Down -> Point Up (Left-Right-Up)
            if ((mask & 1) == 0) rotation = 270; // Missing Right -> Point Left (Up-Down-Left)
        }
        else if (count == 4)
        {
            prefab = fourWayPipePrefab;
            rotation = 0;
        }

        if (prefab != null)
        {
            // Apply Manual Offsets
            if (prefab == straightPipePrefab) rotation += straightRotationOffset;
            else if (prefab == cornerPipePrefab) rotation += cornerRotationOffset;
            else if (prefab == threeWayPipePrefab) rotation += threeWayRotationOffset;
            else if (prefab == fourWayPipePrefab) rotation += fourWayRotationOffset;

            GameObject obj = Instantiate(prefab, pos, Quaternion.Euler(0, 0, rotation), targetParent);
            gridObjects[pos] = obj;

            // Apply scale and position offset only to connectors, straight pipes remain unchanged
            if (prefab == cornerPipePrefab)
            {
                obj.transform.localScale = cornerPipeScale;
                obj.transform.position = (Vector2)obj.transform.position + cornerPipeOffset;
            }
            else if (prefab == threeWayPipePrefab)
            {
                obj.transform.localScale = threeWayPipeScale;
                obj.transform.position = (Vector2)obj.transform.position + threeWayPipeOffset;
            }
            else if (prefab == fourWayPipePrefab)
            {
                obj.transform.localScale = fourWayPipeScale;
                obj.transform.position = (Vector2)obj.transform.position + fourWayPipeOffset;
            }
            // Note: straight pipes are NOT scaled/offset and use prefab's original scale and position
            
            // Set Layer
            int layerIndex = LayerMask.NameToLayer(pipeLayerName);
            if (layerIndex != -1)
            {
                SetLayerRecursively(obj, layerIndex);
            }

            // Adjust Sorting Order: Joints in Front
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (prefab == straightPipePrefab)
                {
                    sr.sortingOrder = 0; // Back
                }
                else
                {
                    sr.sortingOrder = 1; // Front (Corner, 3-Way, 4-Way)
                }
            }
        }
    }

    private Vector2 Snap(Vector2 pos)
    {
        // Modified to allow "Floating Grids" (Off-grid placement)
        // Extremely high precision (5 decimals) to match exact marker positions from InputManager
        // This ensures that even subtle floating point drifts don't cause misalignment.
        float x = Mathf.Round(pos.x * 100000f) / 100000f;
        float y = Mathf.Round(pos.y * 100000f) / 100000f;
        return new Vector2(x, y);
    }

    public void RemovePipeGroup(GameObject groupObj)
    {
        if (groupObj == null) return;
        
        List<Vector2> positionsToRemove = new List<Vector2>();

        // 1. Identify all grid positions belonging to this group
        // We can scan the gridObjects dictionary to find objects that satisfy child relationship
        // OR iterate children of groupObj (more efficient if visual objects match grid positions)
        
        foreach (Transform child in groupObj.transform)
        {
            Vector2 pos = Snap(child.position);
            positionsToRemove.Add(pos);
        }

        // 2. Remove each pipe logically
        foreach (Vector2 pos in positionsToRemove)
        {
            // Remove Connection Data
            if (gridConnections.ContainsKey(pos)) gridConnections.Remove(pos);
            
            // Remove from gridObjects map
            if (gridObjects.ContainsKey(pos)) gridObjects.Remove(pos);
        }

        // 3. Update Neighbors for the hole created
        // (We do this AFTER removing all connections in the batch to avoid unnecessary updates mid-deletion)
        foreach (Vector2 pos in positionsToRemove)
        {
            UpdateNeighborsOf(pos);
        }

        // 4. Destroy the Group Object
        Destroy(groupObj);
    }

    private void UpdateNeighborsOf(Vector2 pos)
    {
        Vector2[] directions = { Vector2.right, Vector2.left, Vector2.up, Vector2.down };
        
        foreach (var dir in directions)
        {
            Vector2 neighborPos = Snap(pos + dir * gridSize);
            if (gridConnections.ContainsKey(neighborPos))
            {
                // Remove connection pointing TO the deleted node
                Vector2 dirToDeleted = -dir; 
                int maskToRemove = GetDirectionMask(dirToDeleted);
                
                gridConnections[neighborPos] &= ~maskToRemove;
                
                UpdatePipeVisualAt(neighborPos);
            }
        }
    }

    public void RemovePipeAt(Vector2 pos)
    {
        pos = Snap(pos);
        if (!gridConnections.ContainsKey(pos)) return;

        // 1. Remove Object
        if (gridObjects.ContainsKey(pos) && gridObjects[pos] != null)
        {
            Destroy(gridObjects[pos]);
            gridObjects.Remove(pos);
        }

        // 2. Remove Connection Data
        gridConnections.Remove(pos);
        
        // 3. Update Neighbors
        UpdateNeighborsOf(pos);
    }

    public void ClearAllPipes()
    {
        foreach (var kvp in gridObjects)
        {
            if (kvp.Value) Destroy(kvp.Value);
        }
        gridObjects.Clear();
        gridConnections.Clear();
    }
    
    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}