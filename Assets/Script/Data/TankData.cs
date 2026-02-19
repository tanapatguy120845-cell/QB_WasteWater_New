using UnityEngine;
using System.Collections.Generic;
using PGroup;
using Unity.VisualScripting;

public class TankData : MonoBehaviour
{
    public string tankID;
    public string displayName = "Unnamed Tank 2D";
    public string tankType = "group"; // Default type

    [Header("Capacity Settings")]
    public int maxCapacity = 3;

    [Header("Water Settings (2D)")]
    public WaterLevelController waterLevelScript; 

    [Header("Devices")]
    public List<GameObject> internalDevices = new List<GameObject>();

    [SerializeField] private GameObject[] sensorFloat;

    public bool CanAddDevice()
    {
        RefreshDeviceList();
        return internalDevices.Count < maxCapacity;
    }

    public void SetName(string newName)
    {
        displayName = newName;
        Debug.Log($"[TankData] เปลี่ยนชื่อถังเป็น: {newName}");
    }

    private void Update()
    {
        RefreshDeviceList();
    }
    
    private void Awake()
    {
        if (string.IsNullOrEmpty(tankID))
            tankID = "Tank2D_" + Random.Range(100, 999);
        
        if (waterLevelScript == null)
            waterLevelScript = GetComponentInChildren<WaterLevelController>();
    }

    public void RefreshDeviceList()
    {
        internalDevices.Clear();
        foreach (Transform child in transform)
        {
            if (child.CompareTag("Device") || child.CompareTag("Float"))
            {
                internalDevices.Add(child.gameObject);
            }
        }
    }

    public List<DeviceData> GetInternalDevicesData()
    {
        List<DeviceData> deviceList = new List<DeviceData>();
        DeviceComponent[] components = GetComponentsInChildren<DeviceComponent>();
        foreach (var dc in components)
        {
            deviceList.Add(dc.ToData(this.transform));
        }
        return deviceList;
    }

    public void UpdateWaterLevel(int index)
    {
        for (int i = 0; i < sensorFloat.Length; i++)
        {
            if(sensorFloat[i].GetComponentInChildren<FloatSensor>() != null)
            {
                sensorFloat[i].GetComponentInChildren<FloatSensor>().ShowFloating(false);
            }
        }
        for (int i = 0; i < index; i++)
        {
            if (sensorFloat[i].GetComponentInChildren<FloatSensor>() != null)
                sensorFloat[i].GetComponentInChildren<FloatSensor>().ShowFloating(true);

            if (sensorFloat[i + 5].GetComponentInChildren<FloatSensor>() != null)
                sensorFloat[i + 5].GetComponentInChildren<FloatSensor>().ShowFloating(true);
        }
        
    }
}