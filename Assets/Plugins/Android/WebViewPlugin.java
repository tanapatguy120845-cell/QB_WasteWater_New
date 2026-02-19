package com.qualitybrain.webviewplugin;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Typeface;
import android.graphics.drawable.GradientDrawable;
import android.os.Build;
import android.util.Log;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebChromeClient;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

public class WebViewPlugin {

    private static final String TAG = "WebViewPlugin";
    private static FrameLayout container;
    private static WebView webView;
    private static boolean isAdded = false;

    // ========== Show WebView ==========
    // width/height = 0 means fullscreen
    public static void showWebView(final String url, final int left, final int top,
                                   final int width, final int height) {
        final Activity activity = UnityPlayer.currentActivity;
        if (activity == null) {
            Log.e(TAG, "showWebView: Activity is null!");
            return;
        }
        Log.d(TAG, "showWebView called: url=" + url + " left=" + left + " top=" + top + " w=" + width + " h=" + height);

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                try {
                    // If already created, just show and reload
                    if (container != null && isAdded) {
                        container.setVisibility(View.VISIBLE);
                        container.bringToFront();
                        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                            container.setElevation(100f);
                        }
                        webView.loadUrl(url);
                        Log.d(TAG, "Reusing existing WebView");
                        return;
                    }

                    // --- Container ---
                    container = new FrameLayout(activity);
                    container.setBackgroundColor(Color.WHITE);

                    // --- WebView ---
                    webView = new WebView(activity);
                    WebSettings settings = webView.getSettings();
                    settings.setJavaScriptEnabled(true);
                    settings.setDomStorageEnabled(true);
                    settings.setMediaPlaybackRequiresUserGesture(false);
                    settings.setAllowFileAccess(true);
                    settings.setDatabaseEnabled(true);
                    settings.setCacheMode(WebSettings.LOAD_DEFAULT);
                    settings.setUseWideViewPort(true);
                    settings.setLoadWithOverviewMode(true);
                    settings.setSupportZoom(true);
                    settings.setBuiltInZoomControls(true);
                    settings.setDisplayZoomControls(false);
                    settings.setUserAgentString(settings.getUserAgentString()
                            + " Mobile"); // Ensure mobile version loads

                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                        settings.setMixedContentMode(WebSettings.MIXED_CONTENT_ALWAYS_ALLOW);
                    }

                    webView.setWebViewClient(new WebViewClient() {
                        @Override
                        public boolean shouldOverrideUrlLoading(WebView view, String url) {
                            view.loadUrl(url);
                            return true;
                        }

                        @Override
                        public void onPageFinished(WebView view, String url) {
                            super.onPageFinished(view, url);
                            Log.d(TAG, "Page finished: " + url);
                            try {
                                UnityPlayer.UnitySendMessage("SimpleWebView",
                                        "OnWebViewPageLoaded", url);
                            } catch (Exception e) {
                                Log.w(TAG, "UnitySendMessage OnWebViewPageLoaded failed: " + e.getMessage());
                            }
                        }
                    });

                    webView.setWebChromeClient(new WebChromeClient());
                    webView.loadUrl(url);

                    // Add WebView to container (fill)
                    container.addView(webView, new FrameLayout.LayoutParams(
                            ViewGroup.LayoutParams.MATCH_PARENT,
                            ViewGroup.LayoutParams.MATCH_PARENT));

                    // --- Close Button ---
                    float density = activity.getResources().getDisplayMetrics().density;
                    int btnSize = (int) (40 * density);
                    int margin = (int) (16 * density);

                    TextView closeBtn = new TextView(activity);
                    closeBtn.setText("\u2715"); // ✕
                    closeBtn.setTextSize(TypedValue.COMPLEX_UNIT_SP, 20);
                    closeBtn.setTextColor(Color.WHITE);
                    closeBtn.setTypeface(Typeface.DEFAULT_BOLD);
                    closeBtn.setGravity(Gravity.CENTER);

                    GradientDrawable bg = new GradientDrawable();
                    bg.setShape(GradientDrawable.OVAL);
                    bg.setColor(Color.argb(200, 30, 30, 30));
                    closeBtn.setBackground(bg);

                    closeBtn.setOnClickListener(new View.OnClickListener() {
                        @Override
                        public void onClick(View v) {
                            hideWebView();
                            try {
                                UnityPlayer.UnitySendMessage("SimpleWebView",
                                        "OnWebViewClosed", "");
                            } catch (Exception e) {
                                Log.w(TAG, "UnitySendMessage OnWebViewClosed failed: " + e.getMessage());
                            }
                        }
                    });

                    FrameLayout.LayoutParams btnParams =
                            new FrameLayout.LayoutParams(btnSize, btnSize);
                    btnParams.gravity = Gravity.TOP | Gravity.END;
                    btnParams.setMargins(0, margin, margin, 0);
                    container.addView(closeBtn, btnParams);

                    // --- Add container to Activity's DecorView (on top of everything) ---
                    FrameLayout.LayoutParams layoutParams;
                    if (width > 0 && height > 0) {
                        layoutParams = new FrameLayout.LayoutParams(width, height);
                        layoutParams.leftMargin = left;
                        layoutParams.topMargin = top;
                        layoutParams.gravity = Gravity.TOP | Gravity.START;
                    } else {
                        // Fullscreen
                        layoutParams = new FrameLayout.LayoutParams(
                                ViewGroup.LayoutParams.MATCH_PARENT,
                                ViewGroup.LayoutParams.MATCH_PARENT);
                    }

                    // Use DecorView to ensure WebView is on top of Unity's SurfaceView
                    ViewGroup decorView = (ViewGroup) activity.getWindow().getDecorView();
                    decorView.addView(container, layoutParams);
                    container.bringToFront();
                    container.requestFocus();

                    // Set elevation to ensure it's above Unity surface (API 21+)
                    if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                        container.setElevation(100f);
                    }

                    isAdded = true;
                    Log.d(TAG, "WebView created and added successfully");
                } catch (Exception e) {
                    Log.e(TAG, "Error in showWebView: " + e.getMessage(), e);
                }
            }
        });
    }

    // ========== Hide WebView (keep instance) ==========
    public static void hideWebView() {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (container != null) {
                    container.setVisibility(View.GONE);
                }
            }
        });
    }

    // ========== Destroy WebView (release memory) ==========
    public static void destroyWebView() {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (webView != null) {
                    webView.stopLoading();
                    webView.clearHistory();
                    webView.clearCache(true);
                    webView.destroy();
                    webView = null;
                }
                if (container != null) {
                    ViewGroup parent = (ViewGroup) container.getParent();
                    if (parent != null) {
                        parent.removeView(container);
                    }
                    container = null;
                }
                isAdded = false;
            }
        });
    }

    // ========== Navigation ==========
    public static boolean canGoBack() {
        return webView != null && webView.canGoBack();
    }

    public static void goBack() {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (webView != null && webView.canGoBack()) {
                    webView.goBack();
                }
            }
        });
    }

    public static void loadUrl(final String url) {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (webView != null) {
                    webView.loadUrl(url);
                }
            }
        });
    }

    // ========== Evaluate JavaScript ==========
    public static void evaluateJavascript(final String js) {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (webView != null && Build.VERSION.SDK_INT >= Build.VERSION_CODES.KITKAT) {
                    webView.evaluateJavascript(js, null);
                }
            }
        });
    }
}
