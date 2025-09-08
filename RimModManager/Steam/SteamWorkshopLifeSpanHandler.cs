namespace RimModManager.Steam
{
    using CefSharp;
    using CefSharp.Handler;
    using Hexa.NET.KittyUI.WebView;

    internal class SteamWorkshopLifeSpanHandler : LifeSpanHandler
    {
        private readonly WebView webView;

        public SteamWorkshopLifeSpanHandler(WebView webView)
        {
            this.webView = webView;
        }

        protected override bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            newBrowser = null!;
            webView.Load(targetUrl);
            return true;
        }
    }
}