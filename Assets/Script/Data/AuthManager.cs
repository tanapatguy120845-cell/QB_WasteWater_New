using UnityEngine;
using System;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

[System.Serializable]
public class AuthRequest {
    public string org_name;
    public string username;
    public string password;
}

[System.Serializable]
public class AuthResponseData {
    public string access_token;
    public string token_type;
}

[System.Serializable]
public class AuthResponse {
    public AuthResponseData data;
}

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    /// <summary>
    /// จะเป็น true เมื่อ authentication เสร็จสมบูรณ์ (ไม่ว่าจะสำเร็จหรือไม่)
    /// </summary>
    public bool IsAuthComplete { get; private set; }

    /// <summary>
    /// จะเป็น true เมื่อ authentication สำเร็จและได้ token แล้ว
    /// </summary>
    public bool IsAuthenticated { get; private set; }

    /// <summary>
    /// Event ที่จะ fire เมื่อ auth เสร็จ (bool = สำเร็จหรือไม่)
    /// </summary>
    public event Action<bool> OnAuthComplete;

    [Header("Token Mode")]
    [Tooltip("true = รับ token จากเว็บ (React sendMessage), false = login เอง (แบบเดิม)")]
    public bool receiveTokenFromWeb = true;

    [Header("Credentials (ใช้เฉพาะเมื่อ receiveTokenFromWeb = false)")]
    public string org_name = "demo";
    public string username = "guy";
    public string password = "User@1234";

    [Header("Endpoints & Settings")]
    public string loginUrl = "https://limbic-authenticate-service-uat.qualitybrain.tech/api/auth/login";
    public bool authenticateOnStart = true;
    public string playerPrefsKey = "AUTH_TOKEN";
    public string playerPrefsOrgKey = "AUTH_ORG";
    public bool debugLogs = true;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // ═══ บังคับใช้โหมดรับ token จากเว็บเท่านั้น ═══
        // (ป้องกันค่าใน Inspector override เป็น false)
        receiveTokenFromWeb = true;

        if (receiveTokenFromWeb)
        {
            // โหมดรับ token จากเว็บ: ลองอ่านจาก localStorage (WebGL) ก่อน
            // ถ้ายังไม่มี ก็รอ ReceiveToken() จาก React sendMessage
            TryLoadTokenFromLocalStorage();

            if (debugLogs) Debug.Log("[AuthManager] Mode: รับ token จากเว็บ (รอ ReceiveToken)");
        }
        // ═══ ปิดระบบ Login เอง — comment ไว้ก่อน ═══
        // else if (authenticateOnStart)
        // {
        //     if (debugLogs) Debug.Log("[AuthManager] Mode: Login เอง");
        //     StartCoroutine(AuthenticateCoroutine());
        // }
    }

    // ═══════════════════════════════════════════════════
    //  รับ Token จากเว็บ (React → Unity)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// รับ token จาก React ผ่าน sendMessage("AuthManager", "ReceiveToken", token)
    /// ทำงานได้ทุกแพลตฟอร์ม (WebGL, Android, iOS)
    /// </summary>
    public void ReceiveToken(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[AuthManager] ReceiveToken ได้รับ token ว่าง!");
            IsAuthComplete = true;
            IsAuthenticated = false;
            OnAuthComplete?.Invoke(false);
            return;
        }

        if (debugLogs)
        {
            string preview = token.Length > 30 ? token.Substring(0, 30) + "..." : token;
            Debug.Log($"[AuthManager] ReceiveToken: ได้รับ token จากเว็บ: {preview}");
        }

        // บันทึก token ลง PlayerPrefs
        PlayerPrefs.SetString(playerPrefsKey, token);
        PlayerPrefs.Save();

        IsAuthComplete = true;
        IsAuthenticated = true;
        OnAuthComplete?.Invoke(true);

        if (debugLogs) Debug.Log("[AuthManager] ✅ Token จากเว็บถูกบันทึกและพร้อมใช้งานแล้ว!");
    }

    /// <summary>
    /// สำหรับ WebGL: ลองอ่าน token จาก localStorage ที่ React เซ็ตไว้
    /// </summary>
    private void TryLoadTokenFromLocalStorage()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // อ่านจาก localStorage ที่ React เซ็ตไว้ (unity_auth_token)
        string token = GetWebLocalStorage("unity_auth_token");
        if (!string.IsNullOrEmpty(token))
        {
            if (debugLogs) Debug.Log("[AuthManager] พบ token ใน localStorage!");
            ReceiveToken(token);
            return;
        }
        if (debugLogs) Debug.Log("[AuthManager] ไม่พบ token ใน localStorage — รอ sendMessage จาก React...");
#else
        // Android/iOS/Editor: เช็ค PlayerPrefs ก่อน (อาจมี token เก่าอยู่)
        string existing = PlayerPrefs.GetString(playerPrefsKey, null);
        if (!string.IsNullOrEmpty(existing))
        {
            if (debugLogs) Debug.Log("[AuthManager] พบ token เดิมใน PlayerPrefs — ใช้ต่อ");
            IsAuthComplete = true;
            IsAuthenticated = true;
            OnAuthComplete?.Invoke(true);
            return;
        }
        if (debugLogs) Debug.Log("[AuthManager] ไม่พบ token — รอ ReceiveToken() หรือ postMessage จาก WebView...");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern string GetWebLocalStorage(string key);
#else
    private static string GetWebLocalStorage(string key) { return null; }
#endif

    // ═══════════════════════════════════════════════════
    //  Login เอง (โหมดเดิม — ใช้เมื่อ receiveTokenFromWeb = false)
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// Coroutine ที่รอจนกว่า auth จะเสร็จ — ใช้ yield return ใน script อื่นได้
    /// </summary>
    public IEnumerator WaitForAuth()
    {
        while (!IsAuthComplete)
            yield return null;
    }

    public IEnumerator AuthenticateCoroutine()
    {
        AuthRequest req = new AuthRequest { org_name = org_name, username = username, password = password };
        string json = JsonUtility.ToJson(req);
        if (debugLogs) Debug.Log($"[AuthManager] POST {loginUrl} with payload: {json}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using (UnityWebRequest www = new UnityWebRequest(loginUrl, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError)
#else
            if (www.isNetworkError)
#endif
            {
                Debug.LogError($"[AuthManager] Network error: {www.error}");
                IsAuthComplete = true;
                IsAuthenticated = false;
                OnAuthComplete?.Invoke(false);
                yield break;
            }

            string respText = www.downloadHandler.text;
            long httpCode = www.responseCode;
            if (debugLogs) Debug.Log($"[AuthManager] HTTP {httpCode} Response: {respText}");

            if (httpCode == 401 || httpCode == 403)
            {
                Debug.LogError($"[AuthManager] Authentication failed (HTTP {httpCode}). ตรวจสอบ credentials (org_name, username, password) ใน Inspector.");
                IsAuthComplete = true;
                IsAuthenticated = false;
                OnAuthComplete?.Invoke(false);
                yield break;
            }

            if (string.IsNullOrEmpty(respText))
            {
                Debug.LogError("[AuthManager] Response body is empty.");
                IsAuthComplete = true;
                IsAuthenticated = false;
                OnAuthComplete?.Invoke(false);
                yield break;
            }

            try
            {
                AuthResponse resp = JsonUtility.FromJson<AuthResponse>(respText);
                if (resp != null && resp.data != null && !string.IsNullOrEmpty(resp.data.access_token))
                {
                    PlayerPrefs.SetString(playerPrefsKey, resp.data.access_token);
                    PlayerPrefs.SetString(playerPrefsOrgKey, org_name);
                    PlayerPrefs.SetString("AUTH_PASSWORD", password);
                    
                    PlayerPrefs.Save();
                    if (debugLogs) Debug.Log($"[AuthManager] Token saved to PlayerPrefs key '{playerPrefsKey}', org '{playerPrefsOrgKey}', and password saved.");
                    
                    IsAuthComplete = true;
                    IsAuthenticated = true;
                    OnAuthComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[AuthManager] No access_token found in response. HTTP {httpCode}. Response body: {respText}");
                    Debug.LogWarning("[AuthManager] ตรวจสอบว่า credentials ถูกต้องและ API endpoint ทำงานปกติ");
                    IsAuthComplete = true;
                    IsAuthenticated = false;
                    OnAuthComplete?.Invoke(false);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AuthManager] Failed to parse response: {ex.Message}\nResponse was: {respText}");
                IsAuthComplete = true;
                IsAuthenticated = false;
                OnAuthComplete?.Invoke(false);
            }
        }
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    public string GetSavedToken()
    {
        return PlayerPrefs.GetString(playerPrefsKey, null);
    }

    public string GetSavedOrgName()
    {
        return PlayerPrefs.GetString(playerPrefsOrgKey, null);
    }

    public void ClearSavedToken()
    {
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();
        IsAuthenticated = false;
        if (debugLogs) Debug.Log("[AuthManager] Token cleared");
    }

    public void ClearSavedOrgName()
    {
        PlayerPrefs.DeleteKey(playerPrefsOrgKey);
        PlayerPrefs.Save();
        if (debugLogs) Debug.Log("[AuthManager] Org cleared");
    }
}