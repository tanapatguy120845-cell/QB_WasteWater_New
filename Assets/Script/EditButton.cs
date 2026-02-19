using UnityEngine;

public class EditButton : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject lobbyUI;    // หน้า Lobby (ที่มีปุ่ม Edit)
    public GameObject loadButton; // ปุ่ม Load (แยกกัน)
    public GameObject editMenuUI; // หน้าแก้ไข (Category/Placement Menu)

    /// <summary>
    /// สลับไปหน้าแก้ไข (ซ่อน Lobby และปุ่ม Load, เปิดเมนูแก้)
    /// </summary>
    public void ShowEditMenu()
    {
        if (lobbyUI != null) lobbyUI.SetActive(true);
        if (loadButton != null) loadButton.SetActive(false);
        if (editMenuUI != null) editMenuUI.SetActive(false);
    }

    /// <summary>
    /// สลับไปหน้า Lobby (เปิด Lobby และปุ่ม Load, ซ่อนเมนูแก้)
    /// </summary>
    public void ShowLobby()
    {
        if (lobbyUI != null) lobbyUI.SetActive(false);
        if (loadButton != null) loadButton.SetActive(true);
        if (editMenuUI != null) editMenuUI.SetActive(true);
    }
}
