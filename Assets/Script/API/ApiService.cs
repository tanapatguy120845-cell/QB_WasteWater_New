using UnityEngine;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;

public class ApiService : MonoBehaviour
{
    // URLs can be adjustable or hardcoded based on environment
    public string controlUrl = "https://limbic-control-service-uat.qualitybrain.tech"; 
    // Using UAT for testing if needed: "https://wma-control-uat.qualitybrain.tech"
    public string authUrl = "https://limbic-authenticate-service-uat.qualitybrain.tech";
    public string reportUrl = "https://wma-report.qualitybrain.tech";
    public string integrationUrl = "https://scada-dashboard.qualitybrain.tech";

    private string GetUrl(string urlType){
        string url = "";
        switch(urlType.ToLower()){
            case "control": url = controlUrl; break;
            case "auth": url = authUrl; break;
            case "report": url = reportUrl; break;
            case "integration": url = integrationUrl; break;
        }
        return url;
    }

    public string GetFullUrl(string urlType, string endpoint){
        string url = GetUrl(urlType);
        if (string.IsNullOrEmpty(url)) return "";
        return $"{url}{endpoint}";
    }

    public async Task<T> Post<T>(string urlType, string endpoint, object body = null)
    {
        string url = GetUrl(urlType);
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("URL is empty or invalid.");
            return default;
        }

        using var request = new UnityWebRequest($"{url}{endpoint}", "POST");
        request.downloadHandler = new DownloadHandlerBuffer();

        if (body != null)
        {
            // JsonUtility cannot serialize anonymous objects nicely. 
            // We assume 'body' is a class/struct marked [Serializable].
            string json = JsonUtility.ToJson(body);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"POST {endpoint} BODY => {json}");
        }

        if (urlType.ToLower() == "control")
        {
            string token = StoreData.GetToken();
            if (!string.IsNullOrEmpty(token))
                request.SetRequestHeader("Authorization", $"Bearer {token}");
        }

        var operation = request.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"POST Error: {request.error} : {request.downloadHandler.text}");
            return default;
        }

        if (string.IsNullOrWhiteSpace(request.downloadHandler.text))
        {
            return default;
        }

        try
        {
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
        catch
        {
            return default;
        }
    }

    public async Task<T> Get<T>(string urlType, string endpoint)
    {
        string url = GetUrl(urlType);
        if(string.IsNullOrEmpty(url)) return default;

        using var request = UnityWebRequest.Get($"{url}{endpoint}");
        if (urlType.ToLower() == "control" || urlType.ToLower() == "auth")
        {
            string token = StoreData.GetToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", $"Bearer {token}");
            }
        }

        var operation = request.SendWebRequest();
        while (!operation.isDone) await Task.Yield();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"GET {endpoint} Error: {request.error}");
            return default;
        }

        try
        {
            return JsonUtility.FromJson<T>(request.downloadHandler.text);
        }
        catch
        {
            return default;
        }
    }
}
