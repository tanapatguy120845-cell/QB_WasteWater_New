using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// WebViewTester — ใช้ปุ่มที่สร้างเองใน Inspector เปิด/ปิด WebView
/// เมื่อกด "เปิด" จะส่ง INIT_CONTROL data (hardcoded) + token จาก AuthManager ไปยัง WebView
///
/// วิธีใช้:
///   1. สร้าง Button "เปิด" และ "ปิด" ใน Canvas ของคุณเอง
///   2. ลาก Button มาใส่ใน Inspector ที่ช่อง openButton / closeButton
/// </summary>
public class WebViewTester : MonoBehaviour
{
    public static WebViewTester Instance { get; private set; }

    [Header("URL ที่จะเปิดใน WebView")]
    public string testUrl = "https://scada-dashboard.qualitybrain.tech";

    [Header("UI Buttons — ลาก Button จาก Canvas มาใส่")]
    public Button openButton;
    public Button closeButton;

    [Header("Settings")]
    [Tooltip("รอกี่วินาทีหลังเปิด WebView แล้วค่อยส่ง INIT_CONTROL (รอให้หน้าโหลดเสร็จ)")]
    public float sendDelay = 2f;

    [Header("WebView Size")]
    [Tooltip("ถ้าเปิดไว้ จะใช้ขนาดแบบ pixel คงที่ทุกหน้าจอ")]
    public bool useFixedPixelSize = true;
    [Tooltip("ความกว้าง WebView แบบ pixel")]
    public int fixedWidth = 900;
    [Tooltip("ความสูง WebView แบบ pixel")]
    public int fixedHeight = 640;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool _dataSent = false;
    private Coroutine _sendCoroutine;
    private DeviceComponent _currentDevice;
    private int _currentWebViewWidth = 900;
    private int _currentWebViewHeight = 640;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (openButton != null)
        {
            openButton.onClick.AddListener(OnOpenClicked);
        }
        else
        {
            Debug.LogWarning("[WebViewTester] openButton ยังไม่ได้กำหนด! ลาก Button มาใส่ใน Inspector");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseClicked);
            closeButton.interactable = false;
        }
        else
        {
            Debug.LogWarning("[WebViewTester] closeButton ยังไม่ได้กำหนด! ลาก Button มาใส่ใน Inspector");
        }

        SimpleWebView.Instance.OnClosed += OnWebViewClosedByUser;
        SimpleWebView.Instance.OnPageLoaded += OnPageLoaded;
        SimpleWebView.Instance.OnMessageReceived += OnMessageFromWeb;
    }

    /// <summary>
    /// เปิด WebView พร้อมส่งข้อมูล device จริงจาก DeviceComponent
    /// เรียกจาก SelectMachine เมื่อกดเลือกเครื่อง
    /// </summary>
    public void OpenWebViewForDevice(DeviceComponent device, float widthPercent = 2f / 3f, float heightPercent = 2f / 3f)
    {
        _currentDevice = device;

        if (device != null)
            Debug.Log($"[WebViewTester] \ud83d\udccc เลือก Device: {device.displayName} (ID={device.deviceID}, type={device.deviceType}, status={device.currentStatus})");
        else
            Debug.LogWarning("[WebViewTester] \u26a0\ufe0f device == null — จะใช้ข้อมูล fallback");

        OpenWebViewUsingSettings(widthPercent, heightPercent);
    }

    public void OpenWebViewUsingSettings(float widthPercent, float heightPercent)
    {
        if (useFixedPixelSize)
        {
            OpenWebViewWithPixels(fixedWidth, fixedHeight);
        }
        else
        {
            OpenWebViewWithPercent(widthPercent, heightPercent);
        }
    }

    public void OpenWebViewWithPercent(float widthPercent, float heightPercent)
    {
        float wPercent = Mathf.Clamp01(widthPercent);
        float hPercent = Mathf.Clamp01(heightPercent);

        int width = Mathf.RoundToInt(Screen.width * wPercent);
        int height = Mathf.RoundToInt(Screen.height * hPercent);
        int x = Mathf.RoundToInt((Screen.width - width) * 0.5f);
        int y = Mathf.RoundToInt((Screen.height - height) * 0.5f);

        OpenWebViewWithRect(x, y, width, height);
    }

    public void OpenWebViewWithPixels(int width, int height)
    {
        int clampedWidth = Mathf.Clamp(width, 1, Screen.width);
        int clampedHeight = Mathf.Clamp(height, 1, Screen.height);
        int x = Mathf.RoundToInt((Screen.width - clampedWidth) * 0.5f);
        int y = Mathf.RoundToInt((Screen.height - clampedHeight) * 0.5f);

        OpenWebViewWithRect(x, y, clampedWidth, clampedHeight);
    }

    public void OpenWebViewWithRect(int x, int y, int width, int height)
    {
        Debug.Log($"[WebViewTester] \ud83d\udd35 Opening WebView: {testUrl} (x={x}, y={y}, w={width}, h={height})");

        _currentWebViewWidth = width;
        _currentWebViewHeight = height;
        _dataSent = false;
        SimpleWebView.Instance.Show(testUrl, x, y, width, height);

        if (openButton != null) openButton.interactable = false;
        if (closeButton != null) closeButton.interactable = true;

        if (_sendCoroutine != null) StopCoroutine(_sendCoroutine);
        _sendCoroutine = StartCoroutine(SendAfterDelay());
    }

    void OnOpenClicked()
    {
        _currentDevice = null;
        OpenWebViewUsingSettings(1f, 1f);
    }

    /// <summary>
    /// Fallback: รอ sendDelay วินาที แล้วส่ง (กรณี OnPageLoaded ไม่ fire เช่นใน Editor)
    /// </summary>
    IEnumerator SendAfterDelay()
    {
        Debug.Log($"[WebViewTester] ⏳ รอ {sendDelay} วินาที ก่อนส่ง INIT_CONTROL (fallback)...");
        yield return new WaitForSeconds(sendDelay);

        if (!_dataSent)
        {
            Debug.Log("[WebViewTester] ⏳ OnPageLoaded ไม่ fire — ส่งข้อมูลผ่าน fallback delay แทน");
            SendInitControlData();
        }
    }

    void OnCloseClicked()
    {
        Debug.Log("[WebViewTester] 🔴 Closing WebView");

        _dataSent = false;        _currentDevice = null;        if (_sendCoroutine != null) { StopCoroutine(_sendCoroutine); _sendCoroutine = null; }
        SimpleWebView.Instance.Hide();

        if (openButton != null) openButton.interactable = true;
        if (closeButton != null) closeButton.interactable = false;
    }

    /// <summary>
    /// เมื่อ WebView โหลดหน้าเสร็จ (native callback)
    /// ไม่ส่ง INIT_CONTROL ทันที — รอ EMBED_LOADED จาก React แทน
    /// </summary>
    void OnPageLoaded(string url)
    {
        Debug.Log($"[WebViewTester] ✅ OnPageLoaded fired! url={url}");
        Debug.Log("[WebViewTester] ⏳ รอ EMBED_LOADED จาก React ก่อนส่ง INIT_CONTROL...");

        // หยุด fallback coroutine เดิม เพราะจะใช้ EMBED_LOADED เป็นตัว trigger แทน
        if (_sendCoroutine != null) { StopCoroutine(_sendCoroutine); _sendCoroutine = null; }
    }

    /// <summary>
    /// สร้างและส่ง INIT_CONTROL JSON (hardcoded) ไปยัง WebView
    /// เฉพาะ token ดึงจาก AuthManager
    /// </summary>
    void SendInitControlData()
    {
        _dataSent = true;

        // ── ดึง token จาก AuthManager ──
        string token = "";
        if (AuthManager.Instance != null)
        {
            token = AuthManager.Instance.GetSavedToken();
            if (string.IsNullOrEmpty(token))
                Debug.LogWarning("[WebViewTester] ❌ ยังไม่มี token! ตรวจสอบว่า AuthManager login สำเร็จแล้ว");
        }
        else
        {
            Debug.LogWarning("[WebViewTester] ❌ AuthManager.Instance == null");
        }

        // ── สร้าง device info จาก DeviceComponent จริง ──
        WebViewDeviceInfo deviceInfo;
        if (_currentDevice != null)
        {
            deviceInfo = new WebViewDeviceInfo
            {
                _id = _currentDevice.deviceID,
                id = _currentDevice.deviceID,
                name = _currentDevice.displayName,
                topic_name = !string.IsNullOrEmpty(_currentDevice.topic) ? _currentDevice.topic : "",
                mainStatus = !string.IsNullOrEmpty(_currentDevice.currentStatus) ? _currentDevice.currentStatus : "unknown",
                device_code = _currentDevice.deviceType
            };
        }
        else
        {
            // Fallback — ไม่มี device (เช่น กดจากปุ่ม Open โดยตรง)
            deviceInfo = new WebViewDeviceInfo
            {
                _id = "unknown",
                id = "unknown",
                name = "Unknown Device",
                topic_name = "",
                mainStatus = "unknown",
                device_code = "UNKNOWN"
            };
        }

        // ── Hardcoded plant data ──
        WebViewPlantInfo[] plants = new WebViewPlantInfo[]
        {
            new WebViewPlantInfo
            {
                id = 1,
                name = "Plant A",
                thai_name = "โรงงาน ก",
                location_name = "Location 1",
                topic = "plant/topic",
                type = "type1"
            }
        };

        // ── สร้าง message ──
        WebViewInitMessage message = new WebViewInitMessage
        {
            type = "INIT_CONTROL",
            payload = new WebViewInitPayload
            {
                token = token,
                device = deviceInfo,
                plantStatus = true,
                plantData = plants,
                screenWidth = _currentWebViewWidth,
                screenHeight = _currentWebViewHeight
            }
        };

        string json = JsonUtility.ToJson(message, true);
        
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log("[WebViewTester] 📤 กำลังส่ง INIT_CONTROL ไปยัง WebView");
        Debug.Log($"[WebViewTester] URL: {testUrl}");
        Debug.Log($"[WebViewTester] Token: {(string.IsNullOrEmpty(token) ? "❌ ไม่มี token!" : token.Substring(0, Mathf.Min(20, token.Length)) + "...")}");
        Debug.Log($"[WebViewTester] Device._id: {deviceInfo._id}");
        Debug.Log($"[WebViewTester] Device.id: {deviceInfo.id}");
        Debug.Log($"[WebViewTester] Device.name: {deviceInfo.name}");
        Debug.Log($"[WebViewTester] Device.topic_name: {deviceInfo.topic_name}");
        Debug.Log($"[WebViewTester] Device.mainStatus: {deviceInfo.mainStatus}");
        Debug.Log($"[WebViewTester] Device.device_code: {deviceInfo.device_code}");
        Debug.Log($"[WebViewTester] PlantStatus: {message.payload.plantStatus}");
        Debug.Log($"[WebViewTester] PlantData count: {plants.Length}");
        for (int i = 0; i < plants.Length; i++)
            Debug.Log($"[WebViewTester]   Plant[{i}]: id={plants[i].id}, name={plants[i].name}, thai_name={plants[i].thai_name}");
        Debug.Log($"[WebViewTester] Full JSON:\n{json}");
        Debug.Log("══════════════════════════════════════════════════");

        string compactJson = JsonUtility.ToJson(message);
        SimpleWebView.Instance.PostMessage(compactJson);
        
        Debug.Log("[WebViewTester] ✅ PostMessage ส่งเรียบร้อยแล้ว!");
    }

    void OnWebViewClosedByUser()
    {
        Debug.Log("[WebViewTester] WebView closed by user (✕ button)");
        _dataSent = false;
        if (_sendCoroutine != null) { StopCoroutine(_sendCoroutine); _sendCoroutine = null; }

        if (openButton != null) openButton.interactable = true;
        if (closeButton != null) closeButton.interactable = false;
    }

    // ═══════════════════════════════════════════════════
    //  รับข้อมูลจากหน้าเว็บ (Web → Unity)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// เมื่อหน้าเว็บส่ง postMessage กลับมา Unity
    /// รองรับ format: { "type": "TOKEN", "token": "xxx" }
    /// หรือ: { "type": "AUTH_TOKEN", "access_token": "xxx" }
    /// หรือ: { "token": "xxx" } (ไม่มี type)
    /// </summary>
    void OnMessageFromWeb(string jsonMessage)
    {
        Debug.Log("══════════════════════════════════════════════════");
        Debug.Log($"[WebViewTester] 📩 ได้รับ message จากเว็บ: {jsonMessage}");

        try
        {
            WebMessage msg = JsonUtility.FromJson<WebMessage>(jsonMessage);

            // ── EMBED_LOADED: React พร้อมรับข้อมูลแล้ว → ส่ง INIT_CONTROL ──
            if (msg.type == "EMBED_LOADED")
            {
                Debug.Log("[WebViewTester] 🟢 React พร้อมแล้ว! กำลังส่ง INIT_CONTROL...");
                if (!_dataSent)
                {
                    SendInitControlData();
                }
                else
                {
                    Debug.Log("[WebViewTester] ℹ️ ส่ง INIT_CONTROL ไปแล้ว ไม่ส่งซ้ำ");
                }
                Debug.Log("══════════════════════════════════════════════════");
                return;
            }

            // ── CONTROL_READY: React ประมวลผล INIT_CONTROL สำเร็จ ──
            if (msg.type == "CONTROL_READY")
            {
                Debug.Log("[WebViewTester] ✅ React ตอบกลับ CONTROL_READY — การส่งข้อมูลสำเร็จ!");
                Debug.Log("══════════════════════════════════════════════════");
                return;
            }

            // ── Token handling (เดิม) ──
            string token = null;

            if (!string.IsNullOrEmpty(msg.token))
                token = msg.token;
            else if (!string.IsNullOrEmpty(msg.access_token))
                token = msg.access_token;
            else if (!string.IsNullOrEmpty(msg.data))
                token = msg.data;

            if (!string.IsNullOrEmpty(token))
            {
                PlayerPrefs.SetString("AUTH_TOKEN", token);
                PlayerPrefs.Save();

                if (AuthManager.Instance != null)
                {
                    AuthManager.Instance.ReceiveToken(token);
                }

                Debug.Log($"[WebViewTester] ✅ Token ได้รับและบันทึกแล้ว!");
                Debug.Log($"[WebViewTester] Token (ตัดสั้น): {token.Substring(0, Mathf.Min(30, token.Length))}...");
                Debug.Log($"[WebViewTester] Message type: {msg.type}");
            }
            else
            {
                Debug.Log($"[WebViewTester] ℹ️ ได้รับ message แต่ไม่มี token: type={msg.type}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[WebViewTester] ⚠️ Parse message ไม่ได้: {ex.Message}");
        }

        Debug.Log("══════════════════════════════════════════════════");
    }

    void OnDestroy()
    {
        if (SimpleWebView.Instance != null)
        {
            SimpleWebView.Instance.OnClosed -= OnWebViewClosedByUser;
            SimpleWebView.Instance.OnPageLoaded -= OnPageLoaded;
            SimpleWebView.Instance.OnMessageReceived -= OnMessageFromWeb;
        }
    }
}

/// <summary>
/// รูปแบบ message ที่รับจากหน้าเว็บ (flexible — รองรับหลาย format)
/// </summary>
[System.Serializable]
public class WebMessage
{
    public string type;          // เช่น "TOKEN", "AUTH_TOKEN", "LOGIN_SUCCESS"
    public string token;         // token โดยตรง
    public string access_token;  // access_token (บางระบบใช้ชื่อนี้)
    public string data;          // data ทั่วไป
}
