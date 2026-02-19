using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ManagerPopup - Modal popup แสดงรายละเอียดเครื่อง
/// สามารถปิดได้โดยกดปุ่มกากบาท (X)
/// มี Toggle START/STOP สำหรับควบคุมอุปกรณ์
/// </summary>
public class ManagerPopup : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText; // แสดงชื่อเครื่อง (เช่น "เครื่องสูบน้ำเสีย : SP-1")
    public TextMeshProUGUI tankNameText; // [NEW] แสดงชื่อ Tank (เช่น "Tank00")
    public Button closeButton;

    [Header("Body")]
    public Image machineImage;
    public TextMeshProUGUI statusText;

    [Header("Image Library (Optional)")]
    [Tooltip("Library สำหรับดึงรูปภาพตาม deviceType (ถ้าไม่ตั้งค่าจะใช้ Sprite จาก DeviceComponent)")]
    public MachineImageLibrary imageLibrary;

    [Header("Image Size Settings")]
    [Tooltip("กำหนดขนาดรูปแบบคงที่ (0 = ใช้ขนาดเดิม)")]
    public Vector2 fixedImageSize = Vector2.zero;
    
    [Tooltip("ปรับขนาดให้พอดีกับพื้นที่โดยรักษาสัดส่วน")]
    public bool fitInContainer = true;

    [Header("Control Toggles")]
    public Toggle startToggle;
    public Toggle stopToggle;

    [Header("Status Toggles (Remote/Local)")]
    public Toggle toggleRemote;
    public Toggle toggleLocal;

    [Header("Remote/Local Background Colors")]
    [Tooltip("สีพื้นหลังเมื่อเลือก (Active)")]
    public Color activeColor = new Color(0.5f, 1f, 0.5f, 1f); // สีเขียวอ่อน
    
    [Tooltip("สีพื้นหลังเมื่อไม่เลือก (Inactive)")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 1f); // สีขาว/เทาอ่อน

    private string machineId;
    private string tankId;
    private string deviceTopic; // topic ของอุปกรณ์สำหรับส่งคำสั่ง
    private string currentStatus = "unknown";
    private DeviceComponent linkedDevice; // reference ไปยัง DeviceComponent
    private bool isUpdatingToggles = false; // ป้องกัน infinite loop
    private bool isWaitingForAPI = false; // Lock mechanism - ป้องกันกดซ้ำขณะรอ API

    void Start()
    {
        // Auto-find statusText if not assigned in Inspector
        if (statusText == null)
        {
            // พยายามหา TextMeshProUGUI ที่ชื่อมี "Status" หรือ "สถานะ"
            var allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in allTexts)
            {
                string name = txt.gameObject.name.ToLower();
                if (name.Contains("status") || name.Contains("สถานะ"))
                {
                    statusText = txt;
                    Debug.Log($"[ManagerPopup] Auto-found statusText: {txt.gameObject.name}");
                    break;
                }
            }
            
            // ถ้ายังหาไม่เจอ ให้ warning
            if (statusText == null)
            {
                Debug.LogWarning("[ManagerPopup] statusText not assigned! Please assign in Inspector or create a Text object named 'Status'");
            }
        }

        // Setup close button listener
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePopup);

        // Setup toggle listeners
        if (startToggle != null)
            startToggle.onValueChanged.AddListener(OnStartToggleChanged);
        
        if (stopToggle != null)
            stopToggle.onValueChanged.AddListener(OnStopToggleChanged);
    }

    /// <summary>
    /// ตั้งค่าและแสดง ManagerPopup (เวอร์ชันใหม่รับ DeviceComponent)
    /// </summary>
    public void SetManagerPopup(string machineId, string tankId, DeviceComponent device = null)
    {
        this.machineId = machineId;
        this.tankId = tankId;
        this.linkedDevice = device;

        // ดึง topic และ status จาก DeviceComponent
        if (device != null)
        {
            this.deviceTopic = device.topic;
            this.currentStatus = device.currentStatus;
            
            // Subscribe for real-time updates
            device.OnStatusChanged += OnDeviceStatusChanged;
            device.OnPayloadChanged += OnDevicePayloadChanged; // [NEW] Subscribe payload status

            Debug.Log($"[ManagerPopup] Subscribed to device: {device.displayName} (Topic: {deviceTopic})");
            
            // ตั้งค่าสถานะเริ่มต้นของ Remote/Local
            UpdatePayloadDisplay(device.remLoc);
        }
        else
        {
            Debug.LogWarning("[ManagerPopup] Device is NULL! Cannot subscribe to real-time updates.");
        }

        // แสดงหัวข้อ - ใช้ชื่อเครื่องที่ส่งมาจาก SelectMachine (ซึ่งมาจาก DeviceComponent.displayName)
        if (titleText != null)
            titleText.text = !string.IsNullOrEmpty(machineId) ? $"เครื่องสูบน้ำเสีย : {machineId}" : "รายละเอียดเครื่อง";

        // แสดงชื่อ Tank
        if (tankNameText != null)
            tankNameText.text = !string.IsNullOrEmpty(tankId) ? $"ถัง : {tankId}" : "";

        // [NEW] ตั้งค่ารูปภาพเครื่องจักรจาก DeviceComponent
        UpdateMachineImage();

        UpdateStatusDisplay();
        UpdateToggleStates();

        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (linkedDevice != null)
        {
            linkedDevice.OnStatusChanged -= OnDeviceStatusChanged;
            linkedDevice.OnPayloadChanged -= OnDevicePayloadChanged; // [NEW] Unsubscribe
        }
    }

    /// <summary>
    /// อัปเดตรูปภาพเครื่องจักรจาก DeviceComponent
    /// </summary>
    private void UpdateMachineImage()
    {
        if (machineImage == null) return;

        Sprite spriteToUse = null;

        // 1. ลองดึงจาก MachineImageLibrary ก่อน (ถ้ามี)
        if (imageLibrary != null && linkedDevice != null && !string.IsNullOrEmpty(linkedDevice.deviceType))
        {
            spriteToUse = imageLibrary.GetSpriteByDeviceType(linkedDevice.deviceType);
            if (spriteToUse != null)
            {
                Debug.Log($"[ManagerPopup] Using sprite from ImageLibrary for deviceType: {linkedDevice.deviceType}");
            }
        }

        // 2. ถ้าไม่มีใน Library ให้ลองดึงจาก DeviceComponent.machineSprite
        if (spriteToUse == null && linkedDevice != null && linkedDevice.machineSprite != null)
        {
            spriteToUse = linkedDevice.machineSprite;
            Debug.Log($"[ManagerPopup] Using sprite from DeviceComponent.machineSprite: {linkedDevice.displayName}");
        }

        // 3. Fallback: ถ้ายังไม่มี ให้ใช้ SpriteRenderer จากตัวเครื่องเอง
        if (spriteToUse == null && linkedDevice != null)
        {
            SpriteRenderer deviceSpriteRenderer = linkedDevice.GetComponent<SpriteRenderer>();
            if (deviceSpriteRenderer != null && deviceSpriteRenderer.sprite != null)
            {
                spriteToUse = deviceSpriteRenderer.sprite;
                Debug.Log($"[ManagerPopup] Using sprite from SpriteRenderer: {linkedDevice.displayName}");
            }
        }

        // ตั้งค่า Sprite และปรับให้รักษาสัดส่วน
        if (spriteToUse != null)
        {
            machineImage.sprite = spriteToUse;
            
            // รักษา aspect ratio ของรูปภาพ (ป้องกันบิดเบี้ยว)
            machineImage.preserveAspect = fitInContainer;
            
            // ถ้ากำหนดขนาดคงที่ ให้ใช้ขนาดนั้น
            if (fixedImageSize != Vector2.zero && fixedImageSize.x > 0 && fixedImageSize.y > 0)
            {
                RectTransform imageRect = machineImage.GetComponent<RectTransform>();
                if (imageRect != null)
                {
                    imageRect.sizeDelta = fixedImageSize;
                    Debug.Log($"[ManagerPopup] Set fixed image size: {fixedImageSize}");
                }
            }
            
            Debug.Log($"[ManagerPopup] Updated machine image (preserveAspect={fitInContainer})");
        }
        else
        {
            Debug.LogWarning($"[ManagerPopup] No sprite found for device: {linkedDevice?.displayName ?? "Unknown"}");
        }
    }

    private void OnDevicePayloadChanged(SSEPayload payload)
    {
        if (payload != null)
        {
            UpdatePayloadDisplay(payload.RemLoc);
        }
    }

    /// <summary>
    /// อัปเดตการแสดงผล Toggle Remote/Local ตามค่า RemLoc
    /// 1 = Remote, 0 = Local
    /// </summary>
    private void UpdatePayloadDisplay(string remLoc)
    {
        if (toggleRemote == null || toggleLocal == null) return;

        bool isRemote = (remLoc == "1");

        // ตั้งค่า Toggle (ใช้ SetIsOnWithoutNotify เพื่อไม่ให้ Trigger Event ถ้าไม่ได้ตั้งใจ)
        toggleRemote.SetIsOnWithoutNotify(isRemote);
        toggleLocal.SetIsOnWithoutNotify(!isRemote);

        // เปลี่ยนสีพื้นหลัง (BG) ของ Toggle
        UpdateToggleBackgroundColor(toggleRemote, isRemote);
        UpdateToggleBackgroundColor(toggleLocal, !isRemote);

        Debug.Log($"[ManagerPopup] Updated Remote/Local display: RemLoc={remLoc} (isRemote={isRemote})");
    }

    /// <summary>
    /// เปลี่ยนสีพื้นหลังของ Toggle ตามสถานะ Active/Inactive
    /// </summary>
    private void UpdateToggleBackgroundColor(Toggle toggle, bool isActive)
    {
        if (toggle == null) return;

        // หา Image component ที่เป็น Background (ตัวแรกที่เจอใน children)
        Image bgImage = toggle.GetComponentInChildren<Image>();
        if (bgImage != null)
        {
            bgImage.color = isActive ? activeColor : inactiveColor;
        }

        // เปิด/ปิด Outline ตามสถานะ Active/Inactive
        Outline outline = toggle.GetComponentInChildren<Outline>();
        if (outline != null)
        {
            outline.enabled = isActive;
        }
    }

    private void OnDeviceStatusChanged(string newStatus)
    {
        Debug.Log($"[ManagerPopup] Received status update: {newStatus}");
        
        // Ignore "unknown" และ "inprogress" เหมือน Bangpla
        // เพราะเป็นสถานะชั่วคราว ไม่ใช่สถานะจริง
        if (!string.IsNullOrEmpty(newStatus))
        {
            string normalized = newStatus.ToLower().Trim();
            if (normalized == "unknown" || normalized == "inprogress")
            {
                Debug.Log($"[ManagerPopup] Ignoring temporary status: '{newStatus}'");
                return;
            }
        }
        
        currentStatus = newStatus;
        UpdateStatusDisplay();
        UpdateToggleStates();
    }

    /// <summary>
    /// ตั้งค่าและแสดง ManagerPopup (เวอร์ชันเดิมเพื่อ backward compatibility)
    /// </summary>
    public void SetManagerPopup(string machineId, string tankId)
    {
        SetManagerPopup(machineId, tankId, null);
    }

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
    /// ตรวจสอบว่า status เป็นสถานะชั่วคราวหรือไม่
    /// </summary>
    private bool IsTemporaryStatus(string status)
    {
        if (string.IsNullOrEmpty(status)) return true;
        string normalized = status.ToLower().Trim();
        return normalized == "unknown" || normalized == "inprogress";
    }

    /// <summary>
    /// อัปเดตการแสดงสถานะ
    /// </summary>
    private void UpdateStatusDisplay()
    {
        if (statusText != null)
        {
            if (IsRunningStatus(currentStatus))
            {
                statusText.text = "กำลังทำงาน";
                statusText.color = Color.green;
            }
            else
            {
                // unknown หรือ stopped หรือค่าอื่นๆ ให้แสดงว่าหยุดทำงาน
                statusText.text = "หยุดทำงาน";
                statusText.color = Color.gray;
            }
            Debug.Log($"[ManagerPopup] Display updated to: {statusText.text} (Status: {currentStatus})");
        }
        else
        {
            Debug.LogError("[ManagerPopup] statusText is NULL! Check Inspector.");
        }

        // Update Machine Image Color
        if (machineImage != null)
        {
            if (IsRunningStatus(currentStatus))
            {
                // Use device's start color if available, otherwise default to green
                Color targetColor = (linkedDevice != null) ? linkedDevice.startColor : Color.green;
                machineImage.color = targetColor;
            }
            else
            {
                machineImage.color = Color.white;
            }
        }
    }

    public void UpdateToggleStates()
    {
        isUpdatingToggles = true; // ป้องกัน listener ทำงาน

        bool isRunning = IsRunningStatus(currentStatus);
        bool isStopped = IsStoppedStatus(currentStatus);
        Debug.Log($"[ManagerPopup] Updating Toggles. isRunning={isRunning}, isStopped={isStopped} (Status={currentStatus})");

        if (startToggle != null)
        {
            // เมื่อหยุดอยู่: START toggle isOn=true (highlight), กดได้
            // เมื่อทำงานอยู่: START toggle isOn=false (ไม่ highlight), กดไม่ได้
            startToggle.SetIsOnWithoutNotify(!isRunning);
            startToggle.interactable = !isRunning; // กดได้เมื่อหยุดอยู่
        }
        else
        {
            Debug.LogError("[ManagerPopup] startToggle is NULL!");
        }

        if (stopToggle != null)
        {
            // เมื่อทำงานอยู่: STOP toggle isOn=true (highlight), กดได้
            // เมื่อหยุดอยู่: STOP toggle isOn=false (ไม่ highlight), กดไม่ได้
            stopToggle.SetIsOnWithoutNotify(isRunning);
            stopToggle.interactable = isRunning; // กดได้เมื่อทำงานอยู่
        }
        else
        {
            Debug.LogError("[ManagerPopup] stopToggle is NULL!");
        }

        isUpdatingToggles = false;
    }

    /// <summary>
    /// เมื่อ START toggle เปลี่ยนค่า
    /// </summary>
    public void OnStartToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // ไม่ทำอะไรถ้ากำลังอัปเดตจากโค้ด
        if (isWaitingForAPI) return; // ไม่ทำอะไรถ้ากำลังรอ API
        
        // Toggle เปลี่ยนเป็น ON = user คลิก START
        if (isOn)
        {
            Debug.Log("[ManagerPopup] START toggle clicked");
            SendStartCommand();
        }
    }

    /// <summary>
    /// เมื่อ STOP toggle เปลี่ยนค่า
    /// </summary>
    public void OnStopToggleChanged(bool isOn)
    {
        if (isUpdatingToggles) return; // ไม่ทำอะไรถ้ากำลังอัปเดตจากโค้ด
        if (isWaitingForAPI) return; // ไม่ทำอะไรถ้ากำลังรอ API
        
        // Toggle เปลี่ยนเป็น ON = user คลิก STOP
        if (isOn)
        {
            Debug.Log("[ManagerPopup] STOP toggle clicked");
            SendStopCommand();
        }
    }

    /// <summary>
    /// ส่งคำสั่ง START ไป API
    /// </summary>
    private void SendStartCommand()
    {
        if (string.IsNullOrEmpty(deviceTopic))
        {
            Debug.LogWarning("[ManagerPopup] ไม่พบ topic ของอุปกรณ์");
            return;
        }

        if (isWaitingForAPI)
        {
            Debug.LogWarning("[ManagerPopup] กำลังรอ API อยู่ ไม่สามารถส่งคำสั่งซ้ำได้");
            return;
        }

        Debug.Log($"[ManagerPopup] Sending START command for topic: {deviceTopic}");

        // Lock และ Disable toggles ระหว่างรอ
        isWaitingForAPI = true;
        if (startToggle != null) startToggle.interactable = false;
        if (stopToggle != null) stopToggle.interactable = false;

        // Use APIUseCase instead of ControlManager
        SendStartCommandAsync();
    }

    private async void SendStartCommandAsync() {
        if (string.IsNullOrEmpty(deviceTopic)) {
            isWaitingForAPI = false;
            return;
        }
        
        try {
            // Timeout 10 วินาที เหมือน Bangpla
            var timeoutTask = System.Threading.Tasks.Task.Delay(10000);
            var apiTask = APIUseCase.StartDevice(deviceTopic);
            
            var completedTask = await System.Threading.Tasks.Task.WhenAny(apiTask, timeoutTask);
            
            if (completedTask == timeoutTask) {
                Debug.LogError("[ManagerPopup] START command timeout!");
                isWaitingForAPI = false;
                UpdateToggleStates();
                return;
            }
            
            CommandResponse res = await apiTask;
            if (res != null && (res.status == "success" || res.message == "success")) {
                Debug.Log("[ManagerPopup] START success!");
                currentStatus = "start";
                UpdateStatusDisplay();
                
                if (linkedDevice != null)
                     linkedDevice.currentStatus = "start";
            }
            else {
                Debug.LogError("Start failed: " + (res?.message ?? "unknown error"));
            }
        }
        catch (System.Exception ex) {
            Debug.LogError("Start Exception: " + ex.Message);
        }
        finally {
            isWaitingForAPI = false;
            UpdateToggleStates();
        }
    }

    /// <summary>
    /// ส่งคำสั่ง STOP ไป API
    /// </summary>
    private void SendStopCommand()
    {
        if (string.IsNullOrEmpty(deviceTopic))
        {
            Debug.LogWarning("[ManagerPopup] ไม่พบ topic ของอุปกรณ์");
            return;
        }

        if (isWaitingForAPI)
        {
            Debug.LogWarning("[ManagerPopup] กำลังรอ API อยู่ ไม่สามารถส่งคำสั่งซ้ำได้");
            return;
        }

        Debug.Log($"[ManagerPopup] Sending STOP command for topic: {deviceTopic}");

        // Lock และ Disable toggles ระหว่างรอ
        isWaitingForAPI = true;
        if (startToggle != null) startToggle.interactable = false;
        if (stopToggle != null) stopToggle.interactable = false;

        SendStopCommandAsync();
    }

    private async void SendStopCommandAsync() {
        if (string.IsNullOrEmpty(deviceTopic)) {
            isWaitingForAPI = false;
            return;
        }

        try {
            // Timeout 10 วินาที เหมือน Bangpla
            var timeoutTask = System.Threading.Tasks.Task.Delay(10000);
            var apiTask = APIUseCase.StopDevice(deviceTopic);
            
            var completedTask = await System.Threading.Tasks.Task.WhenAny(apiTask, timeoutTask);
            
            if (completedTask == timeoutTask) {
                Debug.LogError("[ManagerPopup] STOP command timeout!");
                isWaitingForAPI = false;
                UpdateToggleStates();
                return;
            }
            
            CommandResponse res = await apiTask;
            if (res != null && (res.status == "success" || res.message == "success")) {
                Debug.Log("[ManagerPopup] STOP success!");
                currentStatus = "stop";
                UpdateStatusDisplay();
                
                 if (linkedDevice != null)
                     linkedDevice.currentStatus = "stop";
            }
            else {
                 Debug.LogError("Stop failed: " + (res?.message ?? "unknown error"));
            }
        }
        catch (System.Exception ex) {
             Debug.LogError("Stop Exception: " + ex.Message);
        }
        finally {
            isWaitingForAPI = false;
            UpdateToggleStates();
        }
    }

    /// <summary>
    /// ปิด popup (กดกากบาท)
    /// </summary>
    public void ClosePopup()
    {
        Destroy(gameObject);
    }
}

