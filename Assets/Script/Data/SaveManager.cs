using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using System.Text.RegularExpressions;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("UI References")]
    public ObjectPlacement placementScript; // ลากสคริปต์ ObjectPlacement มาใส่
    public EditButton uiController;         // ลากสคริปต์ EditButton มาใส่ <!-- NEW -->

    private string savePath;
    private bool isEditMode = false;
    
    /// <summary>
    /// ตรวจสอบว่าอยู่ใน Edit Mode หรือไม่
    /// </summary>
    public bool IsEditMode => isEditMode;

    
    [System.Serializable]
    public struct PrefabMapping
    {
        public string typeID; // e.g. "Tank01", "valve"
        public GameObject prefab;
    }

    [Header("Prefab Library (Mapping Types)")]
    public List<PrefabMapping> tankLibrary = new List<PrefabMapping>();   // Map: "Tank01" -> PrefabA
    public List<PrefabMapping> deviceLibrary = new List<PrefabMapping>(); // Map: "valve" -> PrefabB

[Header("Settings")]
public Transform levelRoot; // (ถ้ามี) วัตถุที่เป็นโฟลเดอร์เก็บ Tank ใน Hierarchy

[Header("Remote Upload")]
public bool uploadOnSave = true;
public string layoutUploadBaseUrl = "https://limbic-maker-service-uat.qualitybrain.tech/api/layouts/"; // use /api/layouts/ by default (sanitized at runtime)

    private void Start()
    {
        // บังคับให้เริ่มเกมใน View Mode เสมอ
        isEditMode = false;
        if (uiController != null) uiController.ShowLobby(); 
        
        Debug.Log("[SaveManager] Game Started: Forced View Mode");
    }

    private void Awake()
    {
        Instance = this;
        savePath = Application.persistentDataPath + "/tank_layout.json";

        // หากผู้ใช้ยังตั้งค่าใน Inspector เป็น '/layouts' (ไม่มี '/api') ให้แก้ให้อัตโนมัติ
        if (!string.IsNullOrEmpty(layoutUploadBaseUrl))
        {
            if (layoutUploadBaseUrl.Contains("/layouts") && !layoutUploadBaseUrl.Contains("/api/"))
            {
                layoutUploadBaseUrl = layoutUploadBaseUrl.Replace("/layouts", "/api/layouts");
                Debug.Log($"[SaveManager] Auto-fixed layoutUploadBaseUrl to: {layoutUploadBaseUrl}");
            }
        }
    }

    // --- ฟังก์ชันหลักที่ผูกกับปุ่ม EDIT ---
    public void ToggleEditMode()
    {
        isEditMode = !isEditMode;

        if (isEditMode)
        {
            Debug.Log("[SaveManager] Enter Edit Mode");
            if (uiController != null) uiController.ShowEditMenu();
        }
        else
        {
            Debug.Log("[SaveManager] Exit Edit Mode (via Toggle)");
            if (uiController != null) uiController.ShowLobby();
        }
    }

    /// <summary>
    /// ฟังก์ชันสำหรับออกจากโหมดแก้ไขโดยเฉพาะ (ใช้ผูกกับปุ่ม BACK)
    /// </summary>
    public void ExitEditMode()
    {
        isEditMode = false;
        Debug.Log("[SaveManager] Exit Edit Mode (via ExitEditMode)");
        if (uiController != null) uiController.ShowLobby();
    }

    public void SaveAllData()
    {
        GameSaveData rootData = new GameSaveData();
        TankData[] tanksInScene = FindObjectsOfType<TankData>();

        foreach (TankData tank in tanksInScene)
        {
            ObjectSaveData tData = new ObjectSaveData
            {
                id = tank.tankID,
                category = "group",
                // 🌟 Auto-Detect Type from Library:
                type = GetTankTypeID(tank), 
                name = tank.displayName,
                position = new Vector2(tank.transform.position.x, tank.transform.position.y)
            };

            // ดึงข้อมูลอุปกรณ์ลูกๆ (รวม topic สำหรับ local save)
            foreach (var deviceComp in tank.GetComponentsInChildren<DeviceComponent>())
            {
                var dData = deviceComp.ToData(tank.transform); // Create temp data for position/id

                ChildSaveData cData = new ChildSaveData
                {
                    id = dData.deviceID,
                    category = "device",
                    // 🌟 Auto-Detect Type from Library:
                    type = GetDeviceTypeID(deviceComp), // Now we pass the Component!
                    name = dData.displayName, 
                    position = new Vector2(dData.localPosition.x, dData.localPosition.y),
                    topic = dData.topic 
                };
                tData.children.Add(cData);
            }

            rootData.objects.Add(tData);
        }

        // --- เขียนไฟล์ local ที่เก็บ topic ---
        string jsonLocal = JsonUtility.ToJson(rootData, true);
        
        // Use PlayerPrefs for WebGL compatibility
        #if UNITY_WEBGL && !UNITY_EDITOR
        PlayerPrefs.SetString("tank_layout", jsonLocal);
        PlayerPrefs.Save();
        #else
        File.WriteAllText(savePath, jsonLocal);
        #endif
        
        Debug.Log("=== NEW JSON FORMAT (local) ===\n" + jsonLocal);

        // --- สร้างสำเนาเพื่อส่งไป server โดยตัด topic ทิ้ง ---
        if (uploadOnSave)
        {
            GameSaveData uploadData = new GameSaveData();
            uploadData.name = rootData.name;
            uploadData.plant_image = rootData.plant_image;
            foreach (var obj in rootData.objects)
            {
                ObjectSaveData o = new ObjectSaveData { id = obj.id, category = obj.category, type = obj.type, name = obj.name, position = obj.position };
                foreach (var ch in obj.children)
                {
                    ChildSaveData ch2 = new ChildSaveData { id = ch.id, category = ch.category, type = ch.type, name = ch.name, position = ch.position };
                    o.children.Add(ch2); // note: topic intentionally not copied
                }
                uploadData.objects.Add(o);
            }

            string jsonUpload = JsonUtility.ToJson(uploadData, true);
            Debug.Log("=== UPLOAD JSON (topic stripped) ===\n" + jsonUpload);
            StartCoroutine(SendLayoutToServer(jsonUpload));
        }
    }

    public bool loadFromRemote = true; // ถ้า true จะพยายามดึงข้อมูลจาก /api/layouts/{org} ก่อน แล้ว fallback เป็นไฟล์ local

    public void LoadGame()
    {
        // ออกจาก Edit Mode ทันทีเมื่อกด LOAD (ปิดใช้งานชั่วคราวเพื่อทดสอบ)
        // isEditMode = false;
        // if (uiController != null) uiController.ShowLobby(); // NEW: บังคับให้หน้าจอเป็น Lobby เสมอ
        // 
        // Debug.Log("[SaveManager] กด LOAD → ออกจากโหมดแก้ไข (View Mode)");
        
        if (loadFromRemote)
        {
            StartCoroutine(LoadLayoutFromServer());
            return;
        }

        LoadFromLocal();

        Debug.Log("โหลดข้อมูลทั้งหมดสำเร็จ!");
    }

    private void LoadFromLocal()
    {
        string json = "";
        
        // Use PlayerPrefs for WebGL compatibility
        #if UNITY_WEBGL && !UNITY_EDITOR
        json = PlayerPrefs.GetString("tank_layout", "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[SaveManager] No local save data found.");
            return;
        }
        #else
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("[SaveManager] No local save file found.");
            return;
        }
        json = File.ReadAllText(savePath);
        #endif
        
        Debug.Log("Loaded JSON: " + json);

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
        Debug.Log("Parsed GameSaveData:\n" + JsonUtility.ToJson(data, true));

        ApplySaveData(data);
    }

    private IEnumerator LoadLayoutFromServer()
    {
        // รอให้ AuthManager authenticate เสร็จก่อน (ถ้ายังไม่เสร็จ)
        if (AuthManager.Instance != null && !AuthManager.Instance.IsAuthComplete)
        {
            Debug.Log("[SaveManager] Waiting for AuthManager to complete...");
            yield return AuthManager.Instance.WaitForAuth();
        }

        string token = PlayerPrefs.GetString("AUTH_TOKEN", null);
        string org = PlayerPrefs.GetString("AUTH_ORG", null);
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(org))
        {
            Debug.LogWarning("[SaveManager] No auth token or org saved. Falling back to local load.");
            LoadFromLocal();
            yield break;
        }

        // sanitize base URL: collapse duplicate slashes but preserve protocol (http:// or https://)
        string origBase = layoutUploadBaseUrl ?? string.Empty;
        string safeBase = origBase.Trim();
        if (safeBase.Contains("://"))
        {
            var parts = safeBase.Split(new string[]{"://"}, System.StringSplitOptions.None);
            string scheme = parts[0];
            string rest = parts.Length > 1 ? parts[1] : string.Empty;
            rest = Regex.Replace(rest, "/{2,}", "/").TrimEnd('/');
            safeBase = scheme + "://" + rest;
        }
        else
        {
            safeBase = Regex.Replace(safeBase, "/{2,}", "/").TrimEnd('/');
        }

        string url = safeBase + "/" + UnityWebRequest.EscapeURL(org);
        Debug.Log($"[SaveManager] GET layout from {url} (sanitized from '{origBase}')");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return www.SendWebRequest();

            // --- Detailed debug: response headers, status code, truncated body ---
            var respTextDbg = www.downloadHandler != null ? www.downloadHandler.text : string.Empty;
            var respCodeDbg = www.responseCode;
            var respHeaders = www.GetResponseHeaders();
            var sb = new StringBuilder();
            if (respHeaders != null)
            {
                foreach (var kv in respHeaders)
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
            }
            else sb.AppendLine("<no response headers>");

            string truncated = respTextDbg.Length > 2000 ? respTextDbg.Substring(0, 2000) + "... (truncated)" : respTextDbg;
            string maskedTokenDbg = token != null && token.Length > 8 ? token.Substring(0, 6) + "..." + token.Substring(token.Length - 4) : token;
            Debug.Log($"[SaveManager] GET Response debug - code={respCodeDbg}, token={maskedTokenDbg}\nHeaders:\n{sb}\nBody (truncated):\n{truncated}");

#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError($"[SaveManager] Fetch failed: {www.error} - {www.downloadHandler.text} (responseCode={www.responseCode})");
                if (www.responseCode == 404)
                    Debug.LogError($"[SaveManager] 404 Not Found — check endpoint and that org '{org}' exists. URL: {url}");

                // fallback to local file if available
                LoadFromLocal();
                yield break;
            }

            string respText = www.downloadHandler.text;
            Debug.Log($"[SaveManager] Fetched JSON: {respText}");

            GameSaveData data = null;
            try
            {
                data = JsonUtility.FromJson<GameSaveData>(respText);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to parse server response: {ex.Message}");
            }

            if (data == null || data.objects == null)
            {
                Debug.LogWarning("[SaveManager] Server returned empty/invalid data. Falling back to local load.");
                LoadFromLocal();
                yield break;
            }

            Debug.Log("Parsed GameSaveData (from server):\n" + JsonUtility.ToJson(data, true));
            ApplySaveData(data);
        }
    }

    private void ApplySaveData(GameSaveData data)
    {
        if (data == null || data.objects == null)
        {
            Debug.LogWarning("[SaveManager] No data to apply.");
            return;
        }

        ClearCurrentScene();

        foreach (var tData in data.objects)
        {
            // 1. ค้นหา Prefab ถังน้ำจาก Library
            GameObject prefabToUse = null;

            // ค้นหาตาม Type ที่บันทึกไว้
            if (!string.IsNullOrEmpty(tData.type))
            {
                var mapping = tankLibrary.Find(x => x.typeID == tData.type);
                if (mapping.prefab != null) prefabToUse = mapping.prefab;
            }

            // Fallback: ถ้าหาไม่เจอ หรือ Type เป็นค่าว่าง ให้ใช้ตัวแรกใน Library (ถ้ามี)
            if (prefabToUse == null && tankLibrary.Count > 0)
            {
                prefabToUse = tankLibrary[0].prefab;
                Debug.LogWarning($"[SaveManager] Tank Type '{tData.type}' not found. Using default: {tankLibrary[0].typeID}");
            }

            if (prefabToUse != null)
            {
                GameObject newTank = Instantiate(prefabToUse, tData.position, Quaternion.identity, levelRoot);
            
                TankData tankComp = newTank.GetComponent<TankData>();
                if (tankComp != null)
                {
                    tankComp.tankID = tData.id;          // คืนค่า ID เดิม
                    tankComp.displayName = tData.name;   // 🌟 คืนชื่อที่ตั้งไว้เข้าสู่ displayName
                    tankComp.tankType = tData.type;      // คืนค่า Type
                }

                Debug.Log($"โหลด Tank: {tData.name} สำเร็จ! ({tData.type})");

                // 2. สร้างอุปกรณ์ภายใน (Devices)
                foreach (var dData in tData.children)
                {
                    GameObject prefabToSpawn = GetPrefabByDeviceType(dData.type);
                    
                    if (prefabToSpawn != null)
                    {
                        // สร้างออกมา
                        GameObject newDevice = Instantiate(prefabToSpawn, newTank.transform);
                        newDevice.transform.localPosition = dData.position;

                        // 🌟 ดึงสคริปต์ DeviceComponent ออกมาเพื่อคืนค่าชื่อแสดงผล และ topic (ถ้ามี)
                        DeviceComponent devComp = newDevice.GetComponent<DeviceComponent>();
                        if (devComp != null)
                        {
                            devComp.deviceID = dData.id;      // คืนค่า ID เดิม
                            devComp.displayName = dData.name; // คืนค่าชื่อแสดงผลที่คุณเคยตั้งไว้
                            devComp.topic = dData.topic;      // ตั้ง topic ที่ได้จาก data (อาจเป็น null/empty)
                            devComp.deviceType = dData.type;  // Restore type (important!)
                        }
                        
                        Debug.Log($"โหลด Device: {dData.name} สำเร็จ! (Type: {dData.type})");
                    }
                }
            }
            else
            {
                 Debug.LogError($"[SaveManager] Failed to load Tank. No matching prefab in library for type: '{tData.type}' and Library is empty!");
            }
        }

        // 🌟 เริ่มการเชื่อมต่อ SSE หลังจากโหลด layout สำเร็จ
        if (SSEManager.Instance != null)
        {
            SSEManager.Instance.StartSSEConnection();
            Debug.Log("[SaveManager] Started SSE connection for real-time updates");
        }

        // 🌟 สั่งให้กล้องซูมไปหาวัตถุทั้งหมดที่เพิ่งสร้างขึ้นมา (ครอบคลุมทั้งโหลด Local และ Server)
        if (CameraController.Instance != null)
        {
            // ใช้ Invoke เล็กน้อยเพื่อให้ Unity มั่นใจว่าโหลด Object ครบทุกตัวก่อนคำนวณตำแหน่ง
            Invoke(nameof(ExecuteFitAll), 0.1f);
        }
    }

    // ฟังก์ชันช่วยเลือก Prefab ตาม "Type" ที่เซฟไว้
    private GameObject GetPrefabByDeviceType(string type)
    {
        if (string.IsNullOrEmpty(type))
        {
             // ถ้า Type ว่าง ให้ลองหาตัวแรก
             if (deviceLibrary.Count > 0) return deviceLibrary[0].prefab;
             return null;
        }

        // 1. ค้นหาจาก Library ที่ตั้งค่าไว้ใน Inspector
        var mapping = deviceLibrary.Find(x => x.typeID == type);
        if (mapping.prefab != null) return mapping.prefab;

        // 2. Fallback: ถ้าหาไม่เจอ ให้ใช้ตัวแรกใน Library (ถ้ามี)
        if (deviceLibrary.Count > 0)
        {
            Debug.LogWarning($"[SaveManager] Device Type '{type}' not found. Using default: {deviceLibrary[0].typeID}");
            return deviceLibrary[0].prefab;
        }

        Debug.LogError($"[SaveManager] Critical: No Device found for type '{type}' and Library is empty.");
        return null;
    }

    // --- Helper Functions for Auto-Detection ---

    private string GetTankTypeID(TankData tank)
    {
        // 1. ถ้ามีการตั้งค่า Manual ไว้ (และไม่ใช่ค่า Default) ให้ใช้ค่านั้น
        if (!string.IsNullOrEmpty(tank.tankType) && tank.tankType != "group")
            return tank.tankType;

        // 2. ถ้าไม่มี ให้ลองหาจาก Library โดยดูว่า Prefab ตัวไหนชื่อเหมือนกัน (หรือ Clone มา)
        // หมายเหตุ: วิธีนี้จะเปรียบเทียบจากชื่อ Prefab เดิม
        string cleanName = tank.gameObject.name.Replace("(Clone)", "").Trim();
        foreach (var map in tankLibrary)
        {
            if (map.prefab != null && map.prefab.name == cleanName)
            {
                return map.typeID;
            }
        }

        // 3. Fallback: ถ้าหาไม่เจอจริงๆ ให้ลองใช้ชื่อ Prefab เป็น Type เลย (เผื่อบังเอิญตรงกัน)
        // หรือจะ return "group" ก็ได้
        return cleanName; 
    }

    private string GetDeviceTypeID(DeviceComponent device)
    {
        // 1. ถ้ามีการตั้งค่า Manual ไว้ (และไม่ใช่ค่า Default) ให้ใช้ค่านั้น
        if (!string.IsNullOrEmpty(device.deviceType) && device.deviceType != "Device" && device.deviceType != "pump")
            return device.deviceType;

        // 2. ลองหาจาก Library
        string cleanName = device.gameObject.name.Replace("(Clone)", "").Trim();
        foreach (var map in deviceLibrary)
        {
            if (map.prefab != null && map.prefab.name == cleanName)
            {
                return map.typeID;
            }
        }

        // 3. Fallback
        return cleanName; // ใช้ชื่อ Prefab ส่งไปเลย ถ้าไม่มีใน Library
    }

    private IEnumerator SendLayoutToServer(string json)
    {
        string token = PlayerPrefs.GetString("AUTH_TOKEN", null);
        string org = PlayerPrefs.GetString("AUTH_ORG", null);
        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(org))
        {
            Debug.LogWarning("[SaveManager] No auth token or org saved. Skipping upload.");
            yield break;
        }

        // sanitize base URL: collapse duplicate slashes but preserve protocol (http:// or https://)
        string origBase = layoutUploadBaseUrl ?? string.Empty;
        string safeBase = origBase.Trim();
        if (safeBase.Contains("://"))
        {
            var parts = safeBase.Split(new string[]{"://"}, System.StringSplitOptions.None);
            string scheme = parts[0];
            string rest = parts.Length > 1 ? parts[1] : string.Empty;
            rest = Regex.Replace(rest, "/{2,}", "/").TrimEnd('/');
            safeBase = scheme + "://" + rest;
        }
        else
        {
            safeBase = Regex.Replace(safeBase, "/{2,}", "/").TrimEnd('/');
        }

        string url = safeBase + "/" + UnityWebRequest.EscapeURL(org);
        Debug.Log($"[SaveManager] Uploading layout to {url} (sanitized from '{origBase}')");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // log headers (mask token for safety)
        string maskedToken = token != null && token.Length > 8 ? token.Substring(0, 6) + "..." + token.Substring(token.Length - 4) : token;
        Debug.Log($"[SaveManager] Request headers: Content-Type=application/json, Authorization=Bearer {maskedToken} (masked)");

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Authorization", $"Bearer {token}");

            yield return www.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
#else
            if (www.isNetworkError || www.isHttpError)
#endif
            {
                Debug.LogError($"[SaveManager] Upload failed: {www.error} - {www.downloadHandler.text} (responseCode={www.responseCode})");
                if (www.responseCode == 404)
                    Debug.LogError($"[SaveManager] 404 Not Found — check `layoutUploadBaseUrl` (no double slashes), verify endpoint path/method and that org '{org}' exists. Current URL: {url}");
                yield break;
            }

            Debug.Log($"[SaveManager] Upload succeeded: {www.responseCode} - {www.downloadHandler.text}");
        }
    }

    private void ClearCurrentScene()
    {
        TankData[] tanks = FindObjectsOfType<TankData>();
        foreach (var t in tanks) Destroy(t.gameObject);
    }

    private void ExecuteFitAll()
    {
        CameraController.Instance.FitAllObjects();
    }
}