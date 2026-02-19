using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// MachineImageLibrary - ScriptableObject สำหรับจัดเก็บรูปภาพเครื่องจักรตามประเภท
/// ใช้สำหรับกรณีที่ต้องการจัดการรูปภาพแบบ centralized
/// หรือเปลี่ยนรูปภาพแบบ dynamic ตาม SSE payload
/// </summary>
[CreateAssetMenu(fileName = "MachineImageLibrary", menuName = "WMA/Machine Image Library")]
public class MachineImageLibrary : ScriptableObject
{
    [System.Serializable]
    public class MachineImageMapping
    {
        [Tooltip("Device Type ID (ต้องตรงกับ DeviceComponent.deviceType)")]
        public string deviceType;
        
        [Tooltip("รูปภาพเครื่องจักร (สำหรับแสดงใน Popup)")]
        public Sprite machineSprite;
        
        [Tooltip("คำอธิบายเพิ่มเติม")]
        public string description;
    }

    [Header("Machine Image Mappings")]
    [Tooltip("รายการ mapping ระหว่าง deviceType และรูปภาพ")]
    public List<MachineImageMapping> mappings = new List<MachineImageMapping>();

    /// <summary>
    /// ค้นหา Sprite จาก deviceType
    /// </summary>
    public Sprite GetSpriteByDeviceType(string deviceType)
    {
        if (string.IsNullOrEmpty(deviceType))
        {
            Debug.LogWarning("[MachineImageLibrary] deviceType is null or empty!");
            return null;
        }

        // ค้นหาแบบ exact match
        var mapping = mappings.Find(m => m.deviceType == deviceType);
        if (mapping != null && mapping.machineSprite != null)
        {
            return mapping.machineSprite;
        }

        // ค้นหาแบบ case-insensitive
        mapping = mappings.Find(m => 
            !string.IsNullOrEmpty(m.deviceType) && 
            m.deviceType.ToLower() == deviceType.ToLower()
        );
        
        if (mapping != null && mapping.machineSprite != null)
        {
            return mapping.machineSprite;
        }

        Debug.LogWarning($"[MachineImageLibrary] No sprite found for deviceType: '{deviceType}'");
        return null;
    }

    /// <summary>
    /// ตรวจสอบว่ามี Sprite สำหรับ deviceType นี้หรือไม่
    /// </summary>
    public bool HasSpriteForDeviceType(string deviceType)
    {
        return GetSpriteByDeviceType(deviceType) != null;
    }

    /// <summary>
    /// เพิ่ม mapping ใหม่
    /// </summary>
    public void AddMapping(string deviceType, Sprite sprite, string description = "")
    {
        // ตรวจสอบว่ามีอยู่แล้วหรือไม่
        var existing = mappings.Find(m => m.deviceType == deviceType);
        if (existing != null)
        {
            Debug.LogWarning($"[MachineImageLibrary] Mapping for '{deviceType}' already exists. Updating...");
            existing.machineSprite = sprite;
            existing.description = description;
        }
        else
        {
            mappings.Add(new MachineImageMapping
            {
                deviceType = deviceType,
                machineSprite = sprite,
                description = description
            });
        }
    }

    /// <summary>
    /// ลบ mapping
    /// </summary>
    public bool RemoveMapping(string deviceType)
    {
        var mapping = mappings.Find(m => m.deviceType == deviceType);
        if (mapping != null)
        {
            mappings.Remove(mapping);
            return true;
        }
        return false;
    }
}
