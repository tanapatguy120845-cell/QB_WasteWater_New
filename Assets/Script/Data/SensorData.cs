using UnityEngine;

[System.Serializable]
public class SensorData
{
    public string sensorID;
    public string sensorType;
    public string displayName;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public string dataKey; // properties.data_key
}
