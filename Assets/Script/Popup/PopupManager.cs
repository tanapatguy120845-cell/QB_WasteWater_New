using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PopupManager - Singleton สำหรับจัดการ popup ทั้งหมด
/// ใช้สำหรับ Instantiate และจัดการ SelectMachine และ ManagerPopup
/// </summary>
public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup Prefabs")]
    [Tooltip("PrefabSelectMachine prefab")]
    public GameObject selectMachinePrefab;
    
    [Tooltip("PrefabManagerPopup prefab")]
    public GameObject managerPopupPrefab;

    [Header("Canvas References")]
    [Tooltip("Canvas สำหรับแสดง popup")]
    public Transform popupContainer;

    [Header("Popup Position Settings")]
    [Tooltip("ระยะห่างแนวตั้งจากอุปกรณ์ถึง Popup (pixels)")]
    public float verticalOffset = 20f; // ลดจาก 50 เหลือ 20

    [Tooltip("ระยะห่างแนวนอนจากอุปกรณ์ (pixels) - ใช้สำหรับปรับซ้ายขวา")]
    public float horizontalOffset = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // ถ้าไม่ได้ assign popupContainer ให้หา Canvas อัตโนมัติ
        if (popupContainer == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
                popupContainer = canvas.transform;
        }
    }

    /// <summary>
    /// แสดง SelectMachine popup เหนืออุปกรณ์
    /// </summary>
    /// <param name="machineId">ชื่อ/ID ของเครื่อง</param>
    /// <param name="tankId">ชื่อ/ID ของ Tank</param>
    /// <param name="worldPosition">ตำแหน่งในโลก (World Position) ของอุปกรณ์</param>
    /// <param name="onConfirm">Callback เมื่อกด "เลือก"</param>
    /// <param name="onCancel">Callback เมื่อกด "ปิด"</param>
    /// <returns>GameObject ของ SelectMachine popup</returns>
    public GameObject ShowSelectMachine(string machineId, string tankId, Vector3 worldPosition, UnityAction<string, string> onConfirm = null, UnityAction<string, string> onCancel = null)
    {
        if (selectMachinePrefab == null)
        {
            Debug.LogError("PopupManager: selectMachinePrefab is not assigned!");
            return null;
        }

        // ลบ popup เก่าถ้ามี
        CloseSelectMachine();

        // สร้าง popup ใหม่
        GameObject popupObj = Instantiate(selectMachinePrefab, popupContainer);
        
        // ตั้งตำแหน่งเหนืออุปกรณ์
        PositionPopupAboveObject(popupObj, worldPosition);

        // ตั้งค่า popup
        SelectMachine selectMachine = popupObj.GetComponent<SelectMachine>();
        if (selectMachine != null)
        {
            selectMachine.SetPanel(machineId, tankId, onConfirm, onCancel);
        }

        return popupObj;
    }

    /// <summary>
    /// ตั้งตำแหน่ง popup เหนืออุปกรณ์
    /// </summary>
    private void PositionPopupAboveObject(GameObject popup, Vector3 worldPosition)
    {
        RectTransform rt = popup.GetComponent<RectTransform>();
        if (rt == null) return;

        RectTransform canvasRect = popupContainer as RectTransform;
        if (canvasRect == null) return;

        // แปลง World Position เป็น Screen Position
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        // แปลง Screen Position เป็น Local Position ใน Canvas
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, Camera.main, out localPoint);
        
        // ตั้งตำแหน่งเบื้องต้น
        rt.anchoredPosition = localPoint;
        
        // รอ 1 frame เพื่อให้ Unity คำนวณขนาดของ Popup
        Canvas.ForceUpdateCanvases();
        
        // คำนวณตำแหน่งที่ต้องการ (เหนืออุปกรณ์)
        Vector2 desiredPosition = localPoint;
        desiredPosition.y += verticalOffset;
        desiredPosition.x += horizontalOffset;
        
        // ตรวจสอบขอบเขตและปรับตำแหน่งให้อยู่ในหน้าจอ
        Vector2 finalPosition = ClampPositionToScreen(rt, canvasRect, desiredPosition);
        
        rt.anchoredPosition = finalPosition;
    }
    
    /// <summary>
    /// ปรับตำแหน่ง Popup ให้อยู่ในขอบเขตหน้าจอ
    /// </summary>
    private Vector2 ClampPositionToScreen(RectTransform popupRect, RectTransform canvasRect, Vector2 desiredPosition)
    {
        Vector2 finalPos = desiredPosition;
        
        // คำนวณขอบเขตของ Canvas
        Vector2 canvasSize = canvasRect.sizeDelta;
        float canvasHalfWidth = canvasSize.x / 2f;
        float canvasHalfHeight = canvasSize.y / 2f;
        
        // คำนวณขอบเขตของ Popup
        Vector2 popupSize = popupRect.sizeDelta;
        float popupHalfWidth = popupSize.x / 2f;
        float popupHalfHeight = popupSize.y / 2f;
        
        // คำนวณ offset จาก Pivot (ถ้า Pivot ไม่ใช่ center)
        Vector2 pivot = popupRect.pivot;
        float pivotOffsetX = (pivot.x - 0.5f) * popupSize.x;
        float pivotOffsetY = (pivot.y - 0.5f) * popupSize.y;
        
        // ตรวจสอบขอบบน
        float top = finalPos.y + pivotOffsetY + popupHalfHeight;
        if (top > canvasHalfHeight)
        {
            finalPos.y = canvasHalfHeight - popupHalfHeight - pivotOffsetY - 10f; // เผื่อขอบ 10px
        }
        
        // ตรวจสอบขอบล่าง
        float bottom = finalPos.y + pivotOffsetY - popupHalfHeight;
        if (bottom < -canvasHalfHeight)
        {
            finalPos.y = -canvasHalfHeight + popupHalfHeight - pivotOffsetY + 10f;
        }
        
        // ตรวจสอบขอบซ้าย
        float left = finalPos.x + pivotOffsetX - popupHalfWidth;
        if (left < -canvasHalfWidth)
        {
            finalPos.x = -canvasHalfWidth + popupHalfWidth - pivotOffsetX + 10f;
        }
        
        // ตรวจสอบขอบขวา
        float right = finalPos.x + pivotOffsetX + popupHalfWidth;
        if (right > canvasHalfWidth)
        {
            finalPos.x = canvasHalfWidth - popupHalfWidth - pivotOffsetX - 10f;
        }
        
        return finalPos;
    }

    /// <summary>
    /// ปิด SelectMachine popup ทั้งหมด
    /// </summary>
    public void CloseSelectMachine()
    {
        GameObject existing = GameObject.Find("PrefabSelectMachine(Clone)");
        if (existing != null)
            Destroy(existing);
    }

    /// <summary>
    /// แสดง ManagerPopup modal
    /// </summary>
    /// <param name="machineId">ชื่อ/ID ของเครื่อง</param>
    /// <param name="tankId">ชื่อ/ID ของ Tank</param>
    /// <returns>GameObject ของ ManagerPopup</returns>
    public GameObject ShowManagerPopup(string machineId, string tankId)
    {
        return ShowManagerPopup(machineId, tankId, null);
    }

    /// <summary>
    /// แสดง ManagerPopup modal พร้อม DeviceComponent reference
    /// </summary>
    /// <param name="machineId">ชื่อ/ID ของเครื่อง</param>
    /// <param name="tankId">ชื่อ/ID ของ Tank</param>
    /// <param name="device">DeviceComponent reference สำหรับส่งคำสั่งควบคุม</param>
    /// <returns>GameObject ของ ManagerPopup</returns>
    public GameObject ShowManagerPopup(string machineId, string tankId, DeviceComponent device)
    {
        if (managerPopupPrefab == null)
        {
            Debug.LogError("PopupManager: managerPopupPrefab is not assigned!");
            return null;
        }

        // เช็คว่ามี popup อยู่แล้วหรือไม่
        if (GameObject.Find("PrefabManagerPopup(Clone)") != null)
        {
            Debug.LogWarning("PopupManager: ManagerPopup is already open!");
            return null;
        }

        // สร้าง popup ใหม่
        GameObject popupObj = Instantiate(managerPopupPrefab, popupContainer);

        // ตั้งค่า popup พร้อม DeviceComponent
        ManagerPopup managerPopup = popupObj.GetComponent<ManagerPopup>();
        if (managerPopup != null)
        {
            managerPopup.SetManagerPopup(machineId, tankId, device);
        }

        return popupObj;
    }

    /// <summary>
    /// ปิด ManagerPopup
    /// </summary>
    public void CloseManagerPopup()
    {
        GameObject existing = GameObject.Find("PrefabManagerPopup(Clone)");
        if (existing != null)
            Destroy(existing);
    }

    /// <summary>
    /// ปิด popup ทั้งหมด
    /// </summary>
    public void CloseAllPopups()
    {
        CloseSelectMachine();
        CloseManagerPopup();
    }
}
