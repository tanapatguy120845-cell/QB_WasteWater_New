mergeInto(LibraryManager.library, {

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
            // อัพเดตตำแหน่งใหม่ทุกครั้ง (กรณี resize/scroll)
            var c = document.getElementById('unity-canvas')
                || document.querySelector('#unity-container canvas')
                || document.querySelector('canvas');
            if (c && width > 0 && height > 0) {
                var r = c.getBoundingClientRect();
                existing.style.left = (r.left + x) + 'px';
                existing.style.top  = (r.top  + y) + 'px';
                existing.style.width  = width + 'px';
                existing.style.height = height + 'px';
            }
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

        // ── หา Unity Canvas เพื่อคำนวณตำแหน่งจริงบนหน้าจอ ──
        var canvas = document.getElementById('unity-canvas')
            || document.querySelector('#unity-container canvas')
            || document.querySelector('canvas')
            || null;

        var canvasRect = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };

        if (width > 0 && height > 0) {
            // x, y จาก Unity = พิกัดภายใน Canvas → ต้องบวก offset ของ Canvas บนหน้าเว็บ
            overlay.style.left   = (canvasRect.left + x) + 'px';
            overlay.style.top    = (canvasRect.top  + y) + 'px';
            overlay.style.width  = width + 'px';
            overlay.style.height = height + 'px';
            overlay.style.borderRadius = '8px';
        } else {
            // Fullscreen = เต็มพื้นที่ Canvas เท่านั้น
            overlay.style.left   = canvasRect.left + 'px';
            overlay.style.top    = canvasRect.top + 'px';
            overlay.style.width  = canvasRect.width + 'px';
            overlay.style.height = canvasRect.height + 'px';
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

    // ========== Check if WebView is visible ==========
    WebView_IsVisible: function () {
        var el = document.getElementById('unity-webview-container');
        return el ? (el.style.display !== 'none' ? 1 : 0) : 0;
    },

    // ========== Navigate to URL ==========
    WebView_LoadUrl: function (urlPtr) {
        var url = UTF8ToString(urlPtr);
        var el = document.getElementById('unity-webview-container');
        if (el) {
            var iframe = el.querySelector('iframe');
            if (iframe) iframe.src = url;
        }
    },

    // ========== Go Back ==========
    WebView_GoBack: function () {
        var el = document.getElementById('unity-webview-container');
        if (el) {
            var iframe = el.querySelector('iframe');
            try { iframe.contentWindow.history.back(); } catch (e) {}
        }
    },

    // ========== Can Go Back ==========
    WebView_CanGoBack: function () {
        // iframe ข้าม origin ไม่สามารถเช็ค history ได้โดยตรง — return true เสมอถ้ามี iframe อยู่
        var el = document.getElementById('unity-webview-container');
        return el ? 1 : 0;
    },

    // ========== Evaluate JavaScript in WebView ==========
    WebView_EvaluateJS: function (jsPtr) {
        var js = UTF8ToString(jsPtr);
        var el = document.getElementById('unity-webview-container');
        if (el) {
            var iframe = el.querySelector('iframe');
            try {
                iframe.contentWindow.postMessage({ type: 'evalJS', code: js }, '*');
            } catch (e) {
                try { iframe.contentWindow.eval(js); } catch (e2) {}
            }
        }
    }
});
