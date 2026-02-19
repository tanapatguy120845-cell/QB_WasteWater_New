using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class GameSaveData
{
    public string name = "New Layout";
    public string plant_image = " ";
    public List<ObjectSaveData> objects = new List<ObjectSaveData>();
}

[System.Serializable]
public class ObjectSaveData
{
    public string id;          
    public string category;    
    public string type;        
    public string name;        
    public Vector2 position;   
    public List<ChildSaveData> children = new List<ChildSaveData>();
}

[System.Serializable]
public class ChildSaveData
{
    public string id;
    public string category;
    public string type;
    public string name;
    public Vector2 position;
    public string topic; // optional topic from server, stored locally but not uploaded back
    public ChildProperties properties; // optional properties (e.g. data_key for sensors)
}

[System.Serializable]
public class ChildProperties
{
    public string data_key;
}

// [System.Serializable] 
// public class TankSaveData
// {
//     public string tankID;
//     public string displayName;
//     public Vector3 position;
//     public List<DeviceData> devices = new List<DeviceData>(); 
// }