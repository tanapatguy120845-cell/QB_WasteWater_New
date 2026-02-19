using PGroup;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PipeInputManager : MonoBehaviour
{
    [Header("References")]
    public NewPipeBuilder pipeBuilder;
    private LineRenderer lineRenderer; // Internal reference
    private GameObject lineSegmentPrefab; // Internal reference (optional)
    public GameObject waterPrefab; // Prefab for Water Flow
    
    // ...

    void Update()
    {
        // Cancel/Exit Mode on ESC
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isBuildingEnabled) 
            {
                ToggleBuildingMode();
            }
            else if (selectedGroup != null)
            {
                DeselectGroup();
            }
            return;
        }

        // DELETE SELECTED GROUP
        if (!isBuildingEnabled && selectedGroup != null && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            DeleteSelectedGroup();
            return;
        }

        // SPAWN WATER FLOW (Spacebar when Group Selected)
        if (!isBuildingEnabled && selectedGroup != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            SpawnWaterFlow();
        }

        // SELECTION INPUT (When NOT building)
        if (!isBuildingEnabled && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleSelectionInput();
            return;
        }

        if (!isBuildingEnabled) return;

        // ... Normal Building Input ...
        // 1. SPACEBAR: Lift Pen
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            LiftPen();
        }
        
        // 2. LEFT CLICK: Place Point
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!GameplayController.instance.isOnBuildPipe) return;
            HandleClick();
        }

        // 3. RIGHT CLICK: Build All
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            BuildAll();
        }

        // 4. Update Rubber Band (Preview)
        if (isDrawing)
        {
            UpdateRubberBand();
        }
    }

    void SpawnWaterFlow()
    {
        if (selectedGroup == null || waterPrefab == null) return;

        PipeGroupPath pathData = selectedGroup.GetComponent<PipeGroupPath>();
        if (pathData != null)
        {
            // Toggle Continuous Flow
            pathData.ToggleFlow(waterPrefab, 0.5f); // Spawn every 0.5s
        }
        else
        {
            Debug.LogWarning("Selected Group has no Path Data!");
        }
    }
    public GameObject nodePrefab; 
    public LayerMask nodeLayer;   
    public float minDistance = 0.5f;
    public float lineWidth = 0.1f; // New Setting




    public bool isBuildingEnabled = false; 

    // ... (Lists) ...
    private List<List<Vector2>> storedPaths = new List<List<Vector2>>();
    private List<GameObject> storedLineObjects = new List<GameObject>(); 

    private List<Vector2> currentPath = new List<Vector2>();
    private bool isDrawing = false;
    private Camera mainCamera;
    
    private List<GameObject> tempNodes = new List<GameObject>();

    void Start()
    {
        mainCamera = Camera.main;
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
            
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
        }
    }

    public void ToggleBuildingMode()
    {
        isBuildingEnabled = !isBuildingEnabled;
        GameplayController.instance.ButtonPipeBuild(isBuildingEnabled);

        if (!isBuildingEnabled)
        {
            ClearAllStoredPaths();
        }
    }
    
    public void SetBuildingMode(bool state)
    {
        isBuildingEnabled = state;
        
         if (!isBuildingEnabled)
        {
            ClearAllStoredPaths();
        }
    }

    void ClearAllStoredPaths()
    {
        currentPath.Clear();
        isDrawing = false;
        
        if (lineRenderer) lineRenderer.positionCount = 0;
        
        storedPaths.Clear();
        
        foreach (var obj in storedLineObjects) Destroy(obj);
        storedLineObjects.Clear();
        
        ClearAllNodes();
    }

    private Transform selectedGroup;
    private Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();



    void SelectGroup(Transform group)
    {
        if (selectedGroup == group) return;
        
        DeselectGroup(); 
        
        selectedGroup = group;
        originalColors.Clear();
        
        // Highlight logic: Get all Renderers in group (Recursive)
        SpriteRenderer[] renderers = selectedGroup.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            // Store original color
            originalColors[sr] = sr.color;
            
            // Apply Highlight
            sr.color = Color.red; 
        }
    }

    void DeselectGroup()
    {
        if (selectedGroup != null)
        {
            // Restore Colors
            foreach (var kvp in originalColors)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.color = kvp.Value;
                }
            }
        }
        
        selectedGroup = null;
        originalColors.Clear();
    }

    void DeleteSelectedGroup()
    {
        if (selectedGroup != null && pipeBuilder != null)
        {
            pipeBuilder.RemovePipeGroup(selectedGroup.gameObject);
            selectedGroup = null;
            originalColors.Clear();
        }
    }



    void HandleSelectionInput()
    {
        Vector2 mousePos = GetMousePosition();
        
        // Debug: Check what we are clicking
        // Raycast but ignore WaterBounds layer (Layer 2 = Ignore Raycast)
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, layerMask); 
        
        if (hit.collider != null)
        {
            // Debug Loop to find Group
            Transform current = hit.collider.transform;
            while (current != null)
            {
                if (current.name.StartsWith("PipeGroup"))
                {
                    SelectGroup(current);
                    return;
                }
                current = current.parent;
            }
            Debug.Log("Hit object but no PipeGroup parent found: " + hit.collider.name);
        }
        else
        {
             // Debug.Log("Clicked Empty Space");
        }
        
        DeselectGroup();
    }




    void HandleClick()
    {
        Vector2 mousePos = GetMousePosition();
        Vector2 targetPos = SnapToGrid(mousePos);
        
        // Raycast check for existing nodes
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0.1f, nodeLayer);
        bool clickedNode = false;
        if (hit.collider != null)
        {
            targetPos = hit.collider.transform.position;
            clickedNode = true;
        }

        //First Click
        if (!isDrawing)
        {
            Debug.Log("Not Drawing");
            // Start NEW active segment
            currentPath.Clear();
            currentPath.Add(targetPos);
            isDrawing = true;
            
            // Show node
            TrySpawnNode(targetPos);
        }
        //Second Click
        else
        {
            Debug.Log("Drawing");
            // Add to CURRENT segment
            // Prevent duplicate adjacent points
            if (currentPath.Count > 0 && Vector2.Distance(currentPath[currentPath.Count-1], targetPos) < 0.1f) return;

            if (currentPath[0].x == targetPos.x || currentPath[0].y == targetPos.y)
            {
                currentPath.Add(targetPos);
                if (!clickedNode) TrySpawnNode(targetPos);

                // AUTO-LIFT: If we have formed a segment (2 points), commit it immediately.
                if (currentPath.Count >= 2)
                {
                    LiftPen();
                }
            }
        }
    }

    void LiftPen()
    {
        if (!isDrawing) return;
        if (currentPath.Count < 2 && currentPath.Count > 0) 
        {
            // Only 1 point? Just cancel it.
            currentPath.Clear();
            isDrawing = false;
            if (lineRenderer) lineRenderer.positionCount = 0;
            return;
        }

        if (currentPath.Count >= 2)
        {
            // Store the path
            storedPaths.Add(new List<Vector2>(currentPath));
            
            // Create a permanent visual for this segment
            CreateVisualSegment(currentPath);
        }

        // Reset current drawing state
        currentPath.Clear();
        isDrawing = false;
        
        // Hide preview line
        if (lineRenderer) lineRenderer.positionCount = 0;


        BuildAll();
    }

    void BuildAll()
    {
        // Capture any currently active drawing
        //LiftPen();

        if (pipeBuilder != null)
        {
            // Build all stored paths as a single batch (One "Piece")
            pipeBuilder.BuildPipeBatch(storedPaths);
        }

        // Cleanup
        storedPaths.Clear();
        
        // Destroy visual lines
        foreach (var obj in storedLineObjects) Destroy(obj);
        storedLineObjects.Clear();

        // Destroy temp nodes (replaced by pipes)
        ClearAllNodes();
    }

    void CreateVisualSegment(List<Vector2> pathPoints)
    {
        GameObject lineObj;
        
        if (lineSegmentPrefab != null)
        {
            lineObj = Instantiate(lineSegmentPrefab, Vector3.zero, Quaternion.identity);
             // Apply Width to Prefab instance
            LineRenderer lrPrefab = lineObj.GetComponent<LineRenderer>();
            if (lrPrefab)
            {
                lrPrefab.startWidth = lineWidth;
                lrPrefab.endWidth = lineWidth;
            }
        }
        else
        {
            // Fallback: Create a simple object with LineRenderer dynamically
            lineObj = new GameObject("SegmentLine");
            LineRenderer lrNew = lineObj.AddComponent<LineRenderer>();
            
            // Copy settings from current renderer if possible
            if (lineRenderer != null)
            {
                lrNew.material = lineRenderer.material;
                lrNew.startColor = lineRenderer.startColor;
                lrNew.endColor = lineRenderer.endColor;
                lrNew.numCapVertices = lineRenderer.numCapVertices;
            }
            // Apply Width
            lrNew.startWidth = lineWidth;
            lrNew.endWidth = lineWidth;
        }

        LineRenderer lr = lineObj.GetComponent<LineRenderer>();
        if (lr != null)
        {
            lr.positionCount = pathPoints.Count;
            for (int i = 0; i < pathPoints.Count; i++)
            {
                lr.SetPosition(i, new Vector3(pathPoints[i].x, pathPoints[i].y, 0));
            }
        }
        storedLineObjects.Add(lineObj);
    }

    void UpdateRubberBand()
    {
        if (lineRenderer == null || currentPath.Count == 0) return;
        
        // Draw the static part of the line
        Vector2 mousePos = GetMousePosition();
        Vector2 cursorTarget = SnapToGrid(mousePos);
        
        // Raycast check for cursor snapping to nodes
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, 0.1f, nodeLayer);
        if (hit.collider != null) cursorTarget = hit.collider.transform.position;

        lineRenderer.positionCount = currentPath.Count + 1;
        for (int i = 0; i < currentPath.Count; i++)
        {
            lineRenderer.SetPosition(i, new Vector3(currentPath[i].x, currentPath[i].y, 0));
        }
        // Rubber band to cursor
        lineRenderer.SetPosition(currentPath.Count, new Vector3(cursorTarget.x, cursorTarget.y, 0));
    }

    void TrySpawnNode(Vector2 pos)
    {
        Collider2D hit = Physics2D.OverlapCircle(pos, 0.1f, nodeLayer);
        if (hit == null && nodePrefab != null)
        {
            GameObject node = Instantiate(nodePrefab, pos, Quaternion.identity, transform);
            tempNodes.Add(node);
        }
    }

    Vector2 SnapToGrid(Vector2 pos)
    {
        float gridSize = (pipeBuilder != null) ? pipeBuilder.gridSize : 1f;
        float x = Mathf.Round(pos.x / gridSize) * gridSize;
        float y = Mathf.Round(pos.y / gridSize) * gridSize;
        return new Vector2(x, y);
    } 

    Vector2 GetMousePosition()
    {
        Camera cam = (mainCamera != null) ? mainCamera : Camera.main;
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mousePos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, -cam.transform.position.z);
        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
        return new Vector2(worldPos.x, worldPos.y);
    }

    public void ClearAllNodes()
    {
        foreach(var node in tempNodes)
        {
            if(node) Destroy(node);
        }
        tempNodes.Clear();
    }
} // Close Class
