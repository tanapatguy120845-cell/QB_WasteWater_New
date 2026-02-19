using System;
using System.Collections.Generic;

[Serializable]
public class CommandResponse
{
    public string message;
    public string status;
}

[Serializable]
public class LoginResponse
{
    public string access_token;
    public string token_type;
}

[Serializable]
public class User
{
    public string id;
    public string username;
    public string full_name;
    public string org_name;
}

[Serializable]
public class StatusBarResponse
{
    public string org;
    public StatusBarData data;
}

[Serializable]
public class StatusBarData
{
    public float total_energy_kwh;
    public float inlet_volume_m3;
    public float outlet_volume_m3;
}

[Serializable]
public class AreaSettingResponce
{
    public string message;
}

[Serializable]
public class ScheduleResponse
{
    public string message;
}

[Serializable]
public class IntegrationEventsResponse
{
    public float uptime;
    public float downtime;
}

// For SSE
[Serializable]
public class SSEMessage
{
    public string status;
    public List<SSEDeviceData> data;
    public string timestamp;
}

[Serializable]
public class SSEDeviceData
{
    public string _id;
    public string topic;
    public string status; 
    public SSEPayload payload;
}

[Serializable]
public class SSEPayload
{
    public string RemLoc;
    public string automanual;
    public string submode;
    public string status;
    public string OVL;
    public string runtime;
    public string DT;
    public string disable;
    public string EMR;
}
