using System;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Lightweight cross-platform WebView wrapper.
/// Supports Android, iOS, and WebGL — no third-party plugin required.
///
/// Usage:
///   SimpleWebView.Instance.Show("https://www.youtube.com");
///   SimpleWebView.Instance.Hide();
/// </summary>
public class SimpleWebView : MonoBehaviour
{
    // ───────── Singleton ─────────
    private static SimpleWebView _instance;

    public static SimpleWebView Instance
    {
        get
        {
            if (_instance == null)
            {
                // GameObject name MUST be "SimpleWebView" — native code sends messages here
                GameObject go = new GameObject("SimpleWebView");
                _instance = go.AddComponent<SimpleWebView>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // ───────── Events ─────────
    /// <summary>Fired when the user taps the close button on the native WebView.</summary>
    public event Action OnClosed;

    /// <summary>Fired when the WebView finishes loading a page.</summary>
    public event Action<string> OnPageLoaded;

    /// <summary>Fired when the WebView sends data back to Unity via postMessage.</summary>
    public event Action<string> OnMessageReceived;

    /// <summary>True while the WebView is visible.</summary>
    public bool IsVisible { get; private set; }

    // ───────── Native bindings (iOS / WebGL) ─────────
#if (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void WebView_Show(string url, int x, int y, int w, int h);
    [DllImport("__Internal")] private static extern void WebView_Hide();
    [DllImport("__Internal")] private static extern void WebView_Destroy();
    [DllImport("__Internal")] private static extern void WebView_GoBack();
    [DllImport("__Internal")] private static extern bool WebView_CanGoBack();
    [DllImport("__Internal")] private static extern void WebView_LoadUrl(string url);
    [DllImport("__Internal")] private static extern void WebView_EvaluateJS(string js);
#endif

    // ───────── Lifecycle ─────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            DestroyWebView();
            _instance = null;
        }
    }

    // ═══════════════════════════════════════════════════
    //  Public API
    // ═══════════════════════════════════════════════════

    /// <summary>Show WebView fullscreen with the given URL.</summary>
    public void Show(string url)
    {
        Show(url, 0, 0, 0, 0);
    }

    /// <summary>
    /// Show WebView at a specific position and size (in screen pixels).
    /// Pass width=0, height=0 for fullscreen.
    /// </summary>
    public void Show(string url, int x, int y, int width, int height)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("showWebView", url, x, y, width, height);
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_Show(url, x, y, width, height);
#else
        // Editor / unsupported platform → open in system browser
        Debug.Log("[SimpleWebView] Opening in system browser: " + url);
        Application.OpenURL(url);
#endif
        IsVisible = true;
    }

    /// <summary>Hide the WebView but keep it in memory for quick re-show.</summary>
    public void Hide()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("hideWebView");
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_Hide();
#endif
        IsVisible = false;
    }

    /// <summary>Destroy the WebView and release all resources.</summary>
    public void DestroyWebView()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("destroyWebView");
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_Destroy();
#endif
        IsVisible = false;
    }

    /// <summary>Navigate back in the WebView history.</summary>
    public void GoBack()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("goBack");
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_GoBack();
#endif
    }

    /// <summary>Check whether the WebView can navigate back.</summary>
    public bool CanGoBack()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            return plugin.CallStatic<bool>("canGoBack");
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        return WebView_CanGoBack();
#else
        return false;
#endif
    }

    /// <summary>Load a new URL without hiding/showing.</summary>
    public void LoadUrl(string url)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("loadUrl", url);
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_LoadUrl(url);
#endif
    }

    /// <summary>
    /// Execute JavaScript code inside the WebView.
    /// Use this to send data to the web page via window.postMessage(), etc.
    /// </summary>
    public void EvaluateJS(string js)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (AndroidJavaClass plugin =
            new AndroidJavaClass("com.qualitybrain.webviewplugin.WebViewPlugin"))
        {
            plugin.CallStatic("evaluateJavascript", js);
        }
#elif (UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR
        WebView_EvaluateJS(js);
#else
        Debug.Log("[SimpleWebView] EvaluateJS (Editor): " + js);
#endif
    }

    /// <summary>
    /// ส่งข้อมูลไปยัง WebView ผ่าน window.postMessage()
    /// WebGL: ส่ง pure JSON (WebView_EvaluateJS จะ JSON.parse แล้ว postMessage ให้)
    /// Android/iOS: ส่ง JS code ตรงๆ
    /// </summary>
    public void PostMessage(string jsonData)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: WebView_EvaluateJS ทำ JSON.parse(js) แล้ว contentWindow.postMessage(parsed, '*')
        // ดังนั้นส่ง pure JSON ไปตรงๆ
        EvaluateJS(jsonData);
#else
        // Android/iOS/Editor: evaluateJavascript รัน JS code จริงๆ
        string js = $"window.postMessage({jsonData}, '*');";
        EvaluateJS(js);
#endif
        Debug.Log("[SimpleWebView] PostMessage sent: " + jsonData);
    }

    // ═══════════════════════════════════════════════════
    //  Native → Unity callback
    // ═══════════════════════════════════════════════════

    /// <summary>Called from native when the close button is tapped.</summary>
    // ReSharper disable once UnusedMember.Local
    private void OnWebViewClosed(string message)
    {
        IsVisible = false;
        OnClosed?.Invoke();
        Debug.Log("[SimpleWebView] Closed by user");
    }

    /// <summary>Called from native when the WebView finishes loading a page.</summary>
    // ReSharper disable once UnusedMember.Local
    private void OnWebViewPageLoaded(string url)
    {
        Debug.Log("[SimpleWebView] Page loaded: " + url);
        OnPageLoaded?.Invoke(url);
    }

    /// <summary>
    /// Called from native (jslib) when the web page sends data via postMessage.
    /// JSON string จะถูกส่งมาเป็น parameter.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private void OnWebViewMessageReceived(string jsonMessage)
    {
        Debug.Log("[SimpleWebView] Message received from web: " + jsonMessage);
        OnMessageReceived?.Invoke(jsonMessage);
    }
}
