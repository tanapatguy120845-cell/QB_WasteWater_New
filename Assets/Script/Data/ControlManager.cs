using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

/// <summary>
/// ControlManager - ส่งคำสั่งควบคุมอุปกรณ์ (start/stop) ไปยัง API
/// </summary>
public class ControlManager : MonoBehaviour
{
    public static ControlManager Instance;

    [Header("Control API Settings")]
    public string controlBaseUrl = "https://limbic-control-service-uat.qualitybrain.tech/api/control/";
    
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

    /// <summary>
    /// ส่งคำสั่ง START ไปยังอุปกรณ์
    /// </summary>
    public void SendStartCommand(string topic, System.Action<bool> onComplete = null)
    {
        StartCoroutine(SendControlCommand(topic, "start", onComplete));
    }

    /// <summary>
    /// ส่งคำสั่ง STOP ไปยังอุปกรณ์
    /// </summary>
    public void SendStopCommand(string topic, System.Action<bool> onComplete = null)
    {
        StartCoroutine(SendControlCommand(topic, "stop", onComplete));
    }

    private IEnumerator SendControlCommand(string topic, string command, System.Action<bool> onComplete)
    {
        string token = PlayerPrefs.GetString("AUTH_TOKEN", null);
        string org = PlayerPrefs.GetString("AUTH_ORG", null);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(org))
        {
            Debug.LogError("[ControlManager] No auth token or org. Cannot send command.");
            onComplete?.Invoke(false);
            yield break;
        }

        // Build URL: {baseUrl}{command}
        string url = $"{controlBaseUrl.TrimEnd('/')}/{command}";
        
        // Create JSON payload
        ControlPayload payload = new ControlPayload
        {
            topic = topic,
            org = org
        };
        string jsonPayload = JsonUtility.ToJson(payload);

        if (showDebugLogs) 
            Debug.Log($"[ControlManager] Sending {command.ToUpper()} to {url} with topic: {topic}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError($"[ControlManager] {command.ToUpper()} failed: {www.error} - {www.downloadHandler.text}");
                onComplete?.Invoke(false);
                yield break;
            }

            if (showDebugLogs) 
                Debug.Log($"[ControlManager] {command.ToUpper()} success: {www.responseCode} - {www.downloadHandler.text}");
            
            onComplete?.Invoke(true);
        }
    }
}

[System.Serializable]
public class ControlPayload
{
    public string topic;
    public string org;
}
