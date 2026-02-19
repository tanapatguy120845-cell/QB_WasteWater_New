mergeInto(LibraryManager.library, {

    // ========== Convert YouTube URL to embeddable format ==========
    _toEmbedUrl: function (url) {
        // youtube.com/watch?v=VIDEO_ID → youtube.com/embed/VIDEO_ID
        var match = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/shorts\/)([A-Za-z0-9_\-]+)/);
        if (match) {
            return 'https://www.youtube.com/embed/' + match[1] + '?autoplay=1&rel=0';
        }
        // youtube.com (homepage) → ไม่สามารถ embed ได้ ต้องเปิดแท็บใหม่
        if (/^https?:\/\/(www\.)?youtube\.com\/?$/.test(url)) {
            return null; // signal to open in new tab
        }
        return url;
    },

    // ========== Show WebView (iframe overlay) ==========
    WebView_Show: function (urlPtr, x, y, width, height) {
        var url = UTF8ToString(urlPtr);

        // แปลง YouTube URL เป็น embed format
        var embedUrl = Module.dynCall ? url : url; // fallback
        try {
            // youtube.com/watch?v=XXX → embed
            var match = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/shorts\/)([A-Za-z0-9_\-]+)/);
            if (match) {
                embedUrl = 'https://www.youtube.com/embed/' + match[1] + '?autoplay=1&rel=0';
            }
            // youtube.com homepage → เปิดในแท็บใหม่แทน
            else if (/^https?:\/\/(www\.)?youtube\.com\/?$/.test(url)) {
                window.open(url, '_blank');
                return;
            }
        } catch (e) { embedUrl = url; }

        // If already exists, just show and reload
        var existing = document.getElementById('unity-webview-container');
        if (existing) {
            existing.style.display = 'flex';
            existing.querySelector('iframe').src = embedUrl;
            return;
        }

        // --- Overlay background ---
        var overlay = document.createElement('div');
        overlay.id = 'unity-webview-container';
        overlay.style.cssText =
            'position:fixed;z-index:9999;display:flex;flex-direction:column;' +
            'background:white;box-shadow:0 8px 32px rgba(0,0,0,0.35);overflow:hidden;';

        if (width > 0 && height > 0) {
            overlay.style.left   = x + 'px';
            overlay.style.top    = y + 'px';
            overlay.style.width  = width + 'px';
            overlay.style.height = height + 'px';
            overlay.style.borderRadius = '8px';
        } else {
            // Fullscreen
            overlay.style.left   = '0';
            overlay.style.top    = '0';
            overlay.style.width  = '100vw';
            overlay.style.height = '100vh';
        }

        // --- Header bar ---
        var header = document.createElement('div');
        header.style.cssText =
            'display:flex;align-items:center;justify-content:space-between;' +
            'padding:6px 12px;background:#222;min-height:36px;flex-shrink:0;';

        var title = document.createElement('span');
        title.textContent = 'WebView';
        title.style.cssText = 'color:white;font-size:14px;font-family:sans-serif;';

        var closeBtn = document.createElement('button');
        closeBtn.innerHTML = '&times;';
        closeBtn.style.cssText =
            'width:28px;height:28px;border:none;border-radius:50%;' +
            'background:rgba(255,255,255,0.2);color:white;font-size:20px;' +
            'cursor:pointer;display:flex;align-items:center;justify-content:center;' +
            'line-height:1;padding:0;';
        closeBtn.onmouseenter = function () { closeBtn.style.background = 'rgba(255,255,255,0.4)'; };
        closeBtn.onmouseleave = function () { closeBtn.style.background = 'rgba(255,255,255,0.2)'; };
        closeBtn.onclick = function () {
            overlay.style.display = 'none';
            try { SendMessage('SimpleWebView', 'OnWebViewClosed', ''); } catch (e) {}
        };

        header.appendChild(title);
        header.appendChild(closeBtn);

        // --- iframe ---
        var iframe = document.createElement('iframe');
        iframe.src = embedUrl;
        iframe.style.cssText = 'flex:1;width:100%;border:none;background:#000;';
        iframe.allow =
            'accelerometer; autoplay; clipboard-write; encrypted-media; ' +
            'gyroscope; picture-in-picture; fullscreen';
        iframe.setAttribute('allowfullscreen', 'true');
        // ห้ามใช้ sandbox กับ YouTube embed เพราะจะถูก block

        // Page loaded callback
        iframe.onload = function() {
            try { SendMessage('SimpleWebView', 'OnWebViewPageLoaded', embedUrl); } catch (e) {}
        };

        overlay.appendChild(header);
        overlay.appendChild(iframe);
        document.body.appendChild(overlay);

        // --- Listen for postMessage from iframe (Web → Unity) ---
        if (!window._unityWebViewListenerAdded) {
            window._unityWebViewListenerAdded = true;
            window.addEventListener('message', function (event) {
                // กรอง message ที่ไม่ใช่ object ออก
                if (!event.data || typeof event.data !== 'object') return;
                // ส่งทุก message ที่เป็น object กลับไป Unity
                try {
                    var jsonStr = JSON.stringify(event.data);
                    SendMessage('SimpleWebView', 'OnWebViewMessageReceived', jsonStr);
                } catch (e) {
                    console.warn('[WebViewPlugin] Failed to forward postMessage:', e);
                }
            });
        }
    },

    // ========== Hide WebView ==========
    WebView_Hide: function () {
        var el = document.getElementById('unity-webview-container');
        if (el) el.style.display = 'none';
    },

    // ========== Destroy WebView ==========
    WebView_Destroy: function () {
        var el = document.getElementById('unity-webview-container');
        if (el) el.parentNode.removeChild(el);
    },

    // ========== Go Back (limited in iframes) ==========
    WebView_GoBack: function () {
        var iframe = document.querySelector('#unity-webview-container iframe');
        if (iframe && iframe.contentWindow) {
            try { iframe.contentWindow.history.back(); } catch (e) {}
        }
    },

    // ========== Can Go Back ==========
    WebView_CanGoBack: function () {
        return false; // Cannot reliably detect in cross-origin iframes
    },

    // ========== Load URL ==========
    WebView_LoadUrl: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        // แปลง YouTube URL เป็น embed format
        var match = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/|youtube\.com\/shorts\/)([A-Za-z0-9_\-]+)/);
        if (match) {
            url = 'https://www.youtube.com/embed/' + match[1] + '?autoplay=1&rel=0';
        }
        var iframe = document.querySelector('#unity-webview-container iframe');
        if (iframe) iframe.src = url;
    },

    // ========== Evaluate JavaScript (postMessage to iframe) ==========
    WebView_EvaluateJS: function (jsPtr) {
        var js = UTF8ToString(jsPtr);
        var iframe = document.querySelector('#unity-webview-container iframe');
        if (iframe && iframe.contentWindow) {
            try {
                iframe.contentWindow.postMessage(JSON.parse(js), '*');
            } catch (e) {
                // If not valid JSON, try eval (same-origin only)
                try { iframe.contentWindow.eval(js); } catch (e2) {}
            }
        }
    },

    // ========== Get localStorage (สำหรับ AuthManager) ==========
    GetWebLocalStorage: function (keyPtr) {
        var key = UTF8ToString(keyPtr);
        try {
            var value = window.localStorage.getItem(key);
            if (value === null) return null;
            
            var bufferSize = lengthBytesUTF8(value) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(value, buffer, bufferSize);
            return buffer;
        } catch (e) {
            console.error('[GetWebLocalStorage] Error:', e);
            return null;
        }
    }
});
