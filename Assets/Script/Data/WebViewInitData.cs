using System;

/// <summary>
/// Data models สำหรับส่ง INIT_CONTROL ไปยัง WebView
/// ใช้กับ JsonUtility.ToJson() เพื่อสร้าง JSON payload
/// </summary>

[Serializable]
public class WebViewInitMessage
{
    public string type = "INIT_CONTROL";
    public WebViewInitPayload payload;
}

[Serializable]
public class WebViewInitPayload
{
    public string token;
    public WebViewDeviceInfo device;
    public bool plantStatus = true;
    public WebViewPlantInfo[] plantData;
    public int screenWidth;  // ขนาด WebView ที่ Unity ตั้ง (pixel)
    public int screenHeight; // ขนาด WebView ที่ Unity ตั้ง (pixel)
}

[Serializable]
public class WebViewDeviceInfo
{
    public string _id;
    public string id;
    public string name;
    public string topic_name;
    public string mainStatus;   // "start" | "stop" | "cooldown" | "overload" | "maintenance"
    public string device_code;
}

[Serializable]
public class WebViewPlantInfo
{
    public int id;
    public string name;
    public string thai_name;
    public string location_name;
    public string topic;
    public string type;
}
