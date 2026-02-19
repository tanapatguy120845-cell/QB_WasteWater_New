using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// SSEManager - จัดการการเชื่อมต่อ Server-Sent Events (SSE) 
/// สำหรับอัปเดตสถานะอุปกรณ์แบบ Real-time
/// </summary>
// SSEManager - จัดการข้อมูลจาก SSEClient และอัปเดต DeviceComponent
public class SSEManager : MonoBehaviour
{
    public static SSEManager Instance;
    
    [Header("Debug")]
    public bool showDebugLogs = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start() {
        // Auto-create SSEClient if it doesn't exist
        if (SSEClient.Instance == null) {
            Debug.Log("[SSEManager] SSEClient not found. Adding it to this GameObject.");
            gameObject.AddComponent<SSEClient>();
            // SSEClient.Awake() will run immediately upon AddComponent, setting Instance
        }

        if (SSEClient.Instance != null) {
            SSEClient.Instance.OnMessageReceived += ProcessSSEData;
            // Ensure connection starts
            StartSSEConnection();
        }
    }

    public void StartSSEConnection() {
        if (SSEClient.Instance != null && SSEClient.Instance.sseCoroutine == null) {
            StartCoroutine(SSEClient.Instance.ConnectToSSE());
        }
    }

    void OnDestroy() {
        if (SSEClient.Instance != null) {
            SSEClient.Instance.OnMessageReceived -= ProcessSSEData;
        }
    }

    private void ProcessSSEData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData)) return;
        
        // แสดงข้อมูลพอดี 1 device (ประมาณ 600 ตัวอักษร)
        if (showDebugLogs) Debug.Log($"[SSEManager] Received: {jsonData.Substring(0, Mathf.Min(600, jsonData.Length))}...");
        
        try
        {
            // Use DTOs from DTOs.cs (assumed visible)
            SSEMessage message = JsonUtility.FromJson<SSEMessage>(jsonData);
            if (message != null && message.data != null)
            {
                UpdateDevicesFromSSE(message.data);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SSEManager] JSON parse error: {ex.Message}");
        }
    }

    private void UpdateDevicesFromSSE(List<SSEDeviceData> devices)
    {
        DeviceComponent[] allDevices = FindObjectsOfType<DeviceComponent>();
        
        foreach (var sseDevice in devices)
        {
            if (string.IsNullOrEmpty(sseDevice.topic)) continue;
            
            foreach (var deviceComp in allDevices)
            {
                if (!string.IsNullOrEmpty(deviceComp.topic) && deviceComp.topic == sseDevice.topic)
                {
                    // Note: UpdateFromSSE argument types might need check. 
                    // DeviceComponent.UpdateFromSSE takes (string status, SSEPayload payload).
                    // Our DTO SSEDeviceData has 'payload' field of type SSEPayload.
                    deviceComp.UpdateFromSSE(sseDevice.status, sseDevice.payload);
                    
                    if (showDebugLogs) 
                        Debug.Log($"[SSEManager] Updated device '{deviceComp.displayName}' topic='{sseDevice.topic}' status='{sseDevice.status}'");
                }
            }
        }
    }
}
