using UnityEngine;

[DisallowMultipleComponent]
public class DeviceComponent : MonoBehaviour
{
    [Header("Device Info")]
    public string deviceID;      
    public string displayName = "Unnamed Device";
    public string deviceType;
    public bool isPlaced = false;

    [Header("Placement Offset")]
    public float offsetY = -0.2f; 

    [Header("Runtime Status (from SSE)")]
    public string currentStatus = "unknown"; // "start", "stop", "error"
    
    [Header("Visual Feedback")]
    public Color startColor = Color.green; // สีเมื่อ status = start
    private Color originalColor = Color.white;
    private SpriteRenderer spriteRenderer;
    private bool hasStoredOriginalColor = false;

    [Header("Machine Image (for Popup)")]
    public Sprite machineSprite; // รูปภาพเครื่องจักรสำหรับแสดงใน ManagerPopup

    public string topic; // optional topic (set when loading from server or by logic)
    public string remLoc; // [NEW] "1" = Remote, "0" = Local

    private void Start()
    {
        // เก็บ SpriteRenderer และสีดั้งเดิม
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && !hasStoredOriginalColor)
        {
            originalColor = spriteRenderer.color;
            hasStoredOriginalColor = true;
        }

        // ให้สุ่ม ID เฉพาะตอนที่ "ไม่มีค่า" เท่านั้น (เช่น ตอนลากวางใหม่)
        // ถ้าเป็นการ Load ค่า deviceID จะถูกส่งมาจาก SaveManager เรียบร้อยแล้วก่อนถึงบรรทัดนี้
        if (string.IsNullOrEmpty(deviceID))
        {
            deviceID = "DEV_" + Random.Range(1000, 9999);
        }
        
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        if (string.IsNullOrEmpty(deviceType))
        {
            deviceType = gameObject.tag;
        }
    }

    // ฟังก์ชันสำหรับเปลี่ยนชื่อจาก UI Rename
    public void SetDeviceID(string newID)
    {
        deviceID = newID;
        //gameObject.name = newID;
    }

    public void SetDisplayName(string newName)
    {
        displayName = newName;
        Debug.Log($"[Device] เปลี่ยนชื่อแสดงผลเป็น: {newName}");
    }

    public System.Action<string> OnStatusChanged;
    public System.Action<SSEPayload> OnPayloadChanged; // [NEW] แจ้งเตือนเมื่อข้อมูล Payload เปลี่ยน (เช่น RemLoc)

    /// <summary>
    /// ตรวจสอบว่าสถานะหมายถึง "กำลังทำงาน" จริงๆ หรือไม่
    /// รองรับ: "start" เท่านั้น
    /// </summary>
    private bool IsRunningStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return false;
        string normalized = status.ToLower().Trim();
        return normalized == "start";
    }

    /// <summary>
    /// ตรวจสอบว่าสถานะหมายถึง "หยุด" จริงๆ หรือไม่
    /// รองรับ: "stop", "stopped" เท่านั้น
    /// </summary>
    private bool IsStoppedStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return false;
        string normalized = status.ToLower().Trim();
        return normalized == "stop" || normalized == "stopped";
    }

    /// <summary>
    /// ตรวจสอบว่าควร ignore status นี้หรือไม่
    /// "unknown" และ "inprogress" ไม่ใช่สถานะจริง ให้ ignore
    /// </summary>
    private bool ShouldIgnoreStatus(string newStatus)
    {
        if (string.IsNullOrEmpty(newStatus)) return true;
        string normalized = newStatus.ToLower().Trim();
        
        // Ignore "unknown" และ "inprogress" เหมือน Bangpla
        if (normalized == "unknown" || normalized == "inprogress")
        {
            Debug.Log($"[Device] {displayName} Ignoring temporary status: '{newStatus}'");
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// อัปเดตสถานะจาก SSE และเปลี่ยนสี visual
    /// </summary>
    public void UpdateFromSSE(string status, SSEPayload payload)
    {
        // Ignore status ชั่วคราว (unknown, inprogress)
        if (ShouldIgnoreStatus(status))
        {
            return;
        }

        currentStatus = status;

        // [NEW] บันทึกค่า RemLoc และแจ้งเตือน Payload
        if (payload != null)
        {
            this.remLoc = payload.RemLoc;
            OnPayloadChanged?.Invoke(payload);
        }

        // เก็บสีดั้งเดิมถ้ายังไม่เคยเก็บ
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null && !hasStoredOriginalColor)
        {
            originalColor = spriteRenderer.color;
            hasStoredOriginalColor = true;
        }

        // เปลี่ยนสีตามสถานะ
        if (spriteRenderer != null)
        {
            if (IsRunningStatus(status))
            {
                spriteRenderer.color = startColor; // สีเขียว
                Debug.Log($"[Device] {displayName} status={status} → สีเขียว");
            }
            else if (IsStoppedStatus(status))
            {
                spriteRenderer.color = originalColor; // สีดั้งเดิม
                Debug.Log($"[Device] {displayName} status={status} → สีปกติ");
            }
            else
            {
                // สถานะอื่นๆ (error, unknown)
                spriteRenderer.color = originalColor;
                Debug.Log($"[Device] {displayName} status={status} → สีปกติ (unknown status)");
            }
        }

        // แจ้งเตือนผู้ที่ subscribe (เช่น ManagerPopup)
        OnStatusChanged?.Invoke(status);
    }

    public DeviceData ToData(Transform tankTransform)
    {
        return new DeviceData
        {
            deviceID = this.deviceID,
            displayName = this.displayName,
            deviceType = this.deviceType,
            localPosition = tankTransform.InverseTransformPoint(transform.position),
            localRotation = transform.localRotation,
            topic = this.topic
        };
    }

    public void OnPlaced(Transform tank)
    {
        isPlaced = true;
        transform.SetParent(tank);
        
        Vector3 pos = transform.localPosition;
        pos.z = -0.1f;
        transform.localPosition = pos;
    }
}
