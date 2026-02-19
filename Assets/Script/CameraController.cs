using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;

    [Header("Pan Settings")]
    private Vector3 dragOrigin;
    private Camera cam;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.5f;
    public float minZoom = 2f;
    public float maxZoom = 20f;

    [Header("Maximum Position")]
    public float maxDistance = 50;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandlePan();   // ข้อ 1: เลื่อนจอด้วยคลิกขวา
        HandleZoom();  // ระบบซูมด้วยลูกกลิ้งเมาส์
    }

    // --- 1. ระบบเลื่อนจอ (Right Click Pan) ---
    void HandlePan()
    {
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            dragOrigin = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        }

        if (Mouse.current.rightButton.isPressed)
        {
            Vector3 currentPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Vector3 difference = dragOrigin - currentPos;
            Vector3 newPos = transform.position + difference;

            // Clamp ขอบเขต
            newPos.x = Mathf.Clamp(newPos.x, -maxDistance, maxDistance);
            newPos.y = Mathf.Clamp(newPos.y, -maxDistance, maxDistance);

            transform.position = newPos;
        }
    }

    // --- 2. ระบบซูม (Scroll Wheel Zoom) ---
    void HandleZoom()
    {
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0)
        {
            float newSize = cam.orthographicSize - (scroll * zoomSpeed * Time.deltaTime);
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }

    // --- 3. ระบบขยายให้เห็นทั้งหมด (Fit All Objects) ---
    public void FitAllObjects()
    {
        TankData[] tanks = FindObjectsOfType<TankData>();
        if (tanks == null || tanks.Length == 0) return;

        Bounds bounds = new Bounds();
        bool boundsInitialized = false;

        foreach (var tank in tanks)
        {
            // รวมขอบเขตจาก Renderers ทั้งหมดในตัว Tank (รวมลูกๆ ด้วย)
            Renderer[] renderers = tank.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!boundsInitialized)
                {
                    bounds = r.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            // ถ้าไม่มี Renderer เลย ให้ใช้ตำแหน่ง Transform แทน
            if (!boundsInitialized)
            {
                if (!boundsInitialized)
                {
                    bounds = new Bounds(tank.transform.position, Vector3.zero);
                    boundsInitialized = true;
                }
                else
                {
                    bounds.Encapsulate(tank.transform.position);
                }
            }
        }

        if (!boundsInitialized) return;

        // ย้ายกล้องไปตรงกลางชุดวัตถุ
        Vector3 center = bounds.center;
        center.z = -10f; 
        transform.position = center;

        // คำนวณระยะ Zoom ให้เห็นครบทุกตัว (Orthographic Size)
        float screenRatio = (float)Screen.width / (float)Screen.height;
        float targetSize;

        // เทียบสัดส่วน กว้าง vs สูง อันไหนใหญ่กว่าให้ยึดอันนั้น
        if (bounds.size.x / screenRatio > bounds.size.y)
            targetSize = bounds.size.x / screenRatio;
        else
            targetSize = bounds.size.y;

        cam.orthographicSize = (targetSize / 2f) + 2f; // +2f padding
    }
}