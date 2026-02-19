#import <Foundation/Foundation.h>
#import <WebKit/WebKit.h>
#import <UIKit/UIKit.h>

// Forward declarations
extern void UnitySendMessage(const char* obj, const char* method, const char* msg);
extern UIViewController* UnityGetGLViewController(void);

// ============================================================
// Helper class for close button action
// ============================================================
@interface WebViewCloseHelper : NSObject
+ (void)closeTapped;
@end

@interface WebViewNavigationDelegate : NSObject <WKNavigationDelegate>
@end

// ============================================================
// Static references
// ============================================================
static WKWebView   *_webView       = nil;
static UIView      *_containerView = nil;
static UIButton    *_closeButton   = nil;
static WebViewNavigationDelegate *_navDelegate = nil;

// ============================================================
// C Interface — called from C# via [DllImport("__Internal")]
// ============================================================
extern "C" {

    UIViewController* _GetRootVC() {
        return UnityGetGLViewController();
    }

    void WebView_Show(const char* urlCStr, int x, int y, int width, int height) {
        UIViewController *vc = _GetRootVC();
        if (!vc) return;

        NSString *urlString = [NSString stringWithUTF8String:urlCStr];

        dispatch_async(dispatch_get_main_queue(), ^{
            // Already created — just show & reload
            if (_containerView != nil) {
                _containerView.hidden = NO;
                [_webView loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:urlString]]];
                return;
            }

            // Determine frame
            CGRect frame;
            if (width > 0 && height > 0) {
                frame = CGRectMake(x, y, width, height);
            } else {
                frame = vc.view.bounds; // fullscreen
            }

            // --- Container ---
            _containerView = [[UIView alloc] initWithFrame:frame];
            _containerView.backgroundColor = [UIColor whiteColor];
            _containerView.autoresizingMask = UIViewAutoresizingFlexibleWidth |
                                              UIViewAutoresizingFlexibleHeight;

            // --- WKWebView ---
            WKWebViewConfiguration *config = [[WKWebViewConfiguration alloc] init];
            config.allowsInlineMediaPlayback = YES;
            if (@available(iOS 10.0, *)) {
                config.mediaTypesRequiringUserActionForPlayback = WKAudiovisualMediaTypeNone;
            }

            _webView = [[WKWebView alloc] initWithFrame:_containerView.bounds
                                          configuration:config];
            _webView.autoresizingMask = UIViewAutoresizingFlexibleWidth |
                                        UIViewAutoresizingFlexibleHeight;
            _webView.allowsBackForwardNavigationGestures = YES;

            _navDelegate = [[WebViewNavigationDelegate alloc] init];
            _webView.navigationDelegate = _navDelegate;

            [_webView loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:urlString]]];
            [_containerView addSubview:_webView];

            // --- Close Button ---
            CGFloat btnSize = 36.0;
            CGFloat margin  = 16.0;

            _closeButton = [UIButton buttonWithType:UIButtonTypeCustom];
            _closeButton.frame = CGRectMake(frame.size.width - btnSize - margin,
                                            margin, btnSize, btnSize);
            _closeButton.autoresizingMask = UIViewAutoresizingFlexibleLeftMargin |
                                            UIViewAutoresizingFlexibleBottomMargin;

            [_closeButton setTitle:@"\u2715" forState:UIControlStateNormal]; // ✕
            [_closeButton setTitleColor:[UIColor whiteColor] forState:UIControlStateNormal];
            _closeButton.titleLabel.font = [UIFont boldSystemFontOfSize:18];
            _closeButton.backgroundColor = [[UIColor blackColor] colorWithAlphaComponent:0.75];
            _closeButton.layer.cornerRadius = btnSize / 2.0;
            _closeButton.clipsToBounds = YES;

            [_closeButton addTarget:[WebViewCloseHelper class]
                             action:@selector(closeTapped)
                   forControlEvents:UIControlEventTouchUpInside];
            [_containerView addSubview:_closeButton];

            // --- Add to view hierarchy ---
            [vc.view addSubview:_containerView];
        });
    }

    void WebView_Hide() {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_containerView != nil) {
                _containerView.hidden = YES;
            }
        });
    }

    void WebView_Destroy() {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_webView != nil) {
                [_webView stopLoading];
                [_webView removeFromSuperview];
                _webView = nil;
            }
            if (_containerView != nil) {
                [_containerView removeFromSuperview];
                _containerView = nil;
            }
            _closeButton = nil;
        });
    }

    void WebView_GoBack() {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_webView != nil && [_webView canGoBack]) {
                [_webView goBack];
            }
        });
    }

    bool WebView_CanGoBack() {
        return _webView != nil && [_webView canGoBack];
    }

    void WebView_LoadUrl(const char* urlCStr) {
        NSString *urlString = [NSString stringWithUTF8String:urlCStr];
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_webView != nil) {
                [_webView loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:urlString]]];
            }
        });
    }

    void WebView_EvaluateJS(const char* jsCStr) {
        NSString *jsString = [NSString stringWithUTF8String:jsCStr];
        dispatch_async(dispatch_get_main_queue(), ^{
            if (_webView != nil) {
                [_webView evaluateJavaScript:jsString completionHandler:nil];
            }
        });
    }
}

// ============================================================
// Close button handler
// ============================================================
@implementation WebViewCloseHelper

+ (void)closeTapped {
    WebView_Hide();
    UnitySendMessage("SimpleWebView", "OnWebViewClosed", "");
}

@end

@implementation WebViewNavigationDelegate

- (void)webView:(WKWebView *)webView didFinishNavigation:(WKNavigation *)navigation {
    NSString *url = webView.URL.absoluteString ?: @"";
    UnitySendMessage("SimpleWebView", "OnWebViewPageLoaded", [url UTF8String]);
}

@end
