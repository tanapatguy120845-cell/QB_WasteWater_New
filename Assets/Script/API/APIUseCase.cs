using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

public class APIUseCase
{
    private static ApiService _api;
    private static ApiService api {
        get {
            if (_api == null) {
                // Try to find in scene
                _api = GameObject.FindObjectOfType<ApiService>();
                if (_api == null) {
                    GameObject go = new GameObject("ApiService");
                    _api = go.AddComponent<ApiService>();
                    GameObject.DontDestroyOnLoad(go);
                }
            }
            return _api;
        }
    }


    public static async Task<CommandResponse> StartDevice(string _t, string _p = "")
    {
        // If password argument is empty, try to get from StoreData
        if (string.IsNullOrEmpty(_p)) {
            _p = StoreData.GetPassword();
        }

        // Wrapper class for body to satisfy JsonUtility
        StartBody body = new StartBody { topic = _t, password = _p };
        CommandResponse response = await api.Post<CommandResponse>("control", "/api/control/commands/start", body);
        return response;
    }

    public static async Task<CommandResponse> StopDevice(string _t, string _p = "")
    {
         if (string.IsNullOrEmpty(_p)) {
            _p = StoreData.GetPassword();
        }

        StartBody body = new StartBody { topic = _t, password = _p };
        CommandResponse response = await api.Post<CommandResponse>("control", "/api/control/commands/stop", body);
        return response;
    }

     public static async Task<CommandResponse> SetAutoManual(string _t, string status)
    {
        AutoManualBody body = new AutoManualBody { topic = _t, automanual = status };
        CommandResponse response = await api.Post<CommandResponse>("control", "/api/control/commands/automanual", body);
        return response;
    }

    // Helper classes for JsonUtility
    [System.Serializable]
    private class StartBody {
        public string topic;
        public string password;
    }

    [System.Serializable]
    private class AutoManualBody {
        public string topic;
        public string automanual;
    }
}
