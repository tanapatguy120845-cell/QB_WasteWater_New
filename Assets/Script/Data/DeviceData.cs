using UnityEngine;

[System.Serializable]
public class DeviceData
{
    public string deviceID;
    public string deviceType;
    public string displayName;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public string topic; // optional topic stored locally
}
