using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class SSEClient : MonoBehaviour
{
    private string sseUrl = "";
    private UnityWebRequest currentRequest;
    public Coroutine sseCoroutine;

    public Action<string> OnMessageReceived = null;

    public static SSEClient Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            Instance = this;
        }
        
        DontDestroyOnLoad(this);

        // Auto connect on start if needed, or wait for explicit call
        // In Bangpla it constructs URL in Awake but Connects in Start or explicit call
    }

    void Start()
    {
        // Optional: Auto connect
        StartCoroutine(ConnectToSSE());
    }

    public IEnumerator ConnectToSSE()
    {
        string site = StoreData.GetOrgId().ToLower();
        // Handle special case for NKCH if needed, copying logic from Bangpla
        string orgParam = (site == "nkch") ? "NKCH" : site;
        
        // Use ApiService config if possible or hardcode base url
        // Bangpla uses hardcoded base url in SSEClient. We can use ApiService.controlUrl if we want to be cleaner
        // But for direct port, I'll use the URL string from Bangpla but maybe point to `ApiService` instance?
        // Let's hardcode for now to match Bangpla's standalone nature or use the one in ApiService if accessible.
        // Bangpla: $"https://wma-control.qualitybrain.tech/api/control/sse/{...}/main-status"
        
        // string baseUrl = "https://wma-control.qualitybrain.tech"; 
        string baseUrl = "https://limbic-control-service-uat.qualitybrain.tech"; // UAT from user request
        
        sseUrl = $"{baseUrl}/api/control/sse/{orgParam}/main-status";
        
        Debug.Log("SSE Connecting to: " + sseUrl);

        try
        {
            currentRequest = new UnityWebRequest(sseUrl, "GET");
            currentRequest.downloadHandler = new StreamingDownloadHandler(HandleMessage);
            
            // Authorization if needed (Bangpla SSEClient doesn't seem to set Auth header in the file I saw? 
            // Wait, let me check Step 16.
            // Step 16 code: `currentRequest = new UnityWebRequest(sseUrl, "GET");` 
            // No Auth header set in the `ConnectToSSE` method I saw in Step 16? 
            // Wait, `SSEService.cs` (Step 15) DOES set Auth header. `SSEClient.cs` (Step 16) DOES NOT.
            // The prompt asks to copy Bangpla_Anim. User wanted "strat stop according to Folder API in Bangpla_Anim... receive API SSE correctly like Bangpla_Anim".
            // Bangpla seems to have TWO SSE scripts. `SSEService.cs` and `SSEClient.cs`. 
            // `SSEService.cs` connects to `integration` events. `SSEClient.cs` connects to `control/sse/.../main-status`.
            // The user request mentions "strat stop" -> control. So `SSEClient.cs` (control/sse) is likely the "main-status" one.
            // `SSEClient.cs` in Step 16 does NOT use Auth header. It just calls URL.
            // I will follow `SSEClient.cs` from Step 16.
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception during SSE setup: " + ex.Message);
            yield break;
        }

        yield return currentRequest.SendWebRequest();

        if (currentRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("SSE Error: " + currentRequest.error);
            // Reconnect?
            yield return new WaitForSeconds(5);
            StartCoroutine(ConnectToSSE());
        }
    }

    private void HandleMessage(string msg) {
        // Log the raw message received from SSE
        Debug.Log($"[SSEClient] Raw Message: {msg}");
        
        if (OnMessageReceived != null) {
            OnMessageReceived.Invoke(msg);
        }
        
        // Also dispatch to DeviceComponents via static event or similar if we want to decouple
        // Or we can let DeviceComponents subscribe to Instance.OnMessageReceived
    }

    public void Disconnect()
    {
        if (sseCoroutine != null)
        {
            StopCoroutine(sseCoroutine);
            sseCoroutine = null;
        }

        if (currentRequest != null)
        {
            currentRequest.Abort();
            currentRequest.Dispose();
            currentRequest = null;
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }
}

public class StreamingDownloadHandler : DownloadHandlerScript
{
    private Action<string> onMessageReceived;
    private StringBuilder buffer = new StringBuilder();

    public StreamingDownloadHandler(Action<string> callback) : base()
    {
        this.onMessageReceived = callback;
    }

    protected override bool ReceiveData(byte[] data, int dataLength)
    {
        if (data == null || dataLength == 0)
            return false;

        string newText = Encoding.UTF8.GetString(data, 0, dataLength);
        buffer.Append(newText);

        ProcessBuffer();

        return true;
    }

    private void ProcessBuffer()
    {
        string content = buffer.ToString();
        int index;
        while ((index = content.IndexOf("\n\n", StringComparison.Ordinal)) != -1)
        {
            string eventBlock = content.Substring(0, index);
            content = content.Substring(index + 2);
            ProcessSSEBlock(eventBlock);
        }

        // Keep remaining partial data
        buffer.Clear();
        buffer.Append(content);
    }

    private void ProcessSSEBlock(string block)
    {
        string[] lines = block.Split('\n');
        StringBuilder dataBuilder = new StringBuilder();

        foreach (var line in lines)
        {   
            if (line.StartsWith("data:"))
            {
                dataBuilder.AppendLine(line.Substring(5).Trim());
            }
        }

        string finalData = dataBuilder.ToString().Trim();
        if (!string.IsNullOrEmpty(finalData))
        {
            onMessageReceived?.Invoke(finalData);
        }
    }
}
