using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SelectMachine - แสดง popup เล็กๆ เหนืออุปกรณ์ที่วางใน Tank
/// เมื่อคลิกปุ่ม "เลือก" จะเปิด PrefabManagerPopup
/// </summary>
public class SelectMachine : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI machineNameTxt;
    public TextMeshProUGUI tankNameTxt;
    public Button selectButton;
    public Button bgButton; // ปุ่มพื้นหลังสำหรับปิด popup เมื่อคลิกที่อื่น

    private string machineId;
    private string tankId;
    private DeviceComponent linkedDevice; // reference ไปยัง DeviceComponent
    private UnityAction<string, string> onConfirm;
    private UnityAction<string, string> onCancel;

    void Start()
    {
        // Setup button listeners
        if (selectButton != null)
            selectButton.onClick.AddListener(OnSelectClick);
        
        // BG button สำหรับปิด popup เมื่อคลิกที่อื่น (คลิกนอก popup)
        if (bgButton != null)
            bgButton.onClick.AddListener(ClosePopup);
    }

    /// <summary>
    /// ตั้งค่า popup และแสดงผล (เวอร์ชันใหม่รับ DeviceComponent)
    /// </summary>
    public void SetPanel(string machineId, string tankId, DeviceComponent device, UnityAction<string, string> onConfirm = null, UnityAction<string, string> onCancel = null)
    {
        this.machineId = machineId;
        this.tankId = tankId;
        this.linkedDevice = device;
        this.onConfirm = onConfirm;
        this.onCancel = onCancel;

        // แสดงชื่อเครื่อง
        if (machineNameTxt != null)
            machineNameTxt.text = !string.IsNullOrEmpty(machineId) ? machineId : "เครื่อง";

        // แสดงชื่อ Tank
        if (tankNameTxt != null)
            tankNameTxt.text = !string.IsNullOrEmpty(tankId) ? tankId : "";

        if (panel != null)
            panel.SetActive(true);
    }

    /// <summary>
    /// ตั้งค่า popup และแสดงผล (เวอร์ชันเดิมเพื่อ backward compatibility)
    /// </summary>
    public void SetPanel(string machineId, string tankId, UnityAction<string, string> onConfirm = null, UnityAction<string, string> onCancel = null)
    {
        SetPanel(machineId, tankId, null, onConfirm, onCancel);
    }

    /// <summary>
    /// เมื่อคลิกปุ่ม "เลือก" - เปิด ManagerPopup
    /// </summary>
    public void OnSelectClick()
    {
        onConfirm?.Invoke(machineId, tankId);

        // เปิด WebView พร้อมส่งข้อมูล Device ที่เลือก
        if (WebViewTester.Instance != null)
        {
            WebViewTester.Instance.OpenWebViewForDevice(linkedDevice);
        }
        else
        {
            Debug.LogWarning("[SelectMachine] WebViewTester.Instance is null. Cannot open WebView.");
        }
        
        // ปิด SelectMachine popup
        Destroy(gameObject);
    }

    /// <summary>
    /// ปิด popup โดยไม่ทำอะไร
    /// </summary>
    public void ClosePopup()
    {
        onCancel?.Invoke(machineId, tankId);
        Destroy(gameObject);
    }
}
