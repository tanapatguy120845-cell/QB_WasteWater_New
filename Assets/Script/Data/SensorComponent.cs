using UnityEngine;

[DisallowMultipleComponent]
public class SensorComponent : MonoBehaviour
{
    [Header("Sensor Info")]
    public string sensorID;
    public string displayName = "Unnamed Sensor";
    public string sensorType;
    public bool isPlaced = false;

    [Header("Properties")]
    public string dataKey; // จะเก็บเป็น properties.data_key ใน JSON

    [Header("Placement Offset")]
    public float offsetY = -0.2f;

    private void Start()
    {
        // สุ่ม ID เฉพาะตอนวางใหม่ (ไม่มีค่า) — ถ้า Load มาจะมี ID อยู่แล้ว
        if (string.IsNullOrEmpty(sensorID))
        {
            sensorID = "SEN_" + Random.Range(1000, 9999);
        }

        if (string.IsNullOrEmpty(displayName))
        {
            displayName = gameObject.name.Replace("(Clone)", "").Trim();
        }

        if (string.IsNullOrEmpty(sensorType))
        {
            sensorType = gameObject.tag;
        }
    }

    // ฟังก์ชันสำหรับเปลี่ยนชื่อจาก UI Rename
    public void SetDisplayName(string newName)
    {
        displayName = newName;
        Debug.Log($"[Sensor] เปลี่ยนชื่อแสดงผลเป็น: {newName}");
    }

    public void SetDataKey(string newKey)
    {
        dataKey = newKey;
        Debug.Log($"[Sensor] เปลี่ยน Data Key เป็น: {newKey}");
    }

    public SensorData ToData(Transform tankTransform)
    {
        return new SensorData
        {
            sensorID = this.sensorID,
            displayName = this.displayName,
            sensorType = this.sensorType,
            localPosition = tankTransform.InverseTransformPoint(transform.position),
            localRotation = transform.localRotation,
            dataKey = this.dataKey
        };
    }

    public void OnPlaced(Transform tank)
    {
        isPlaced = true;
        transform.SetParent(tank);

        Vector3 pos = transform.localPosition;
        pos.z = -0.1f;
        transform.localPosition = pos;
    }
}
