#if BUILD_STEAM_BROWSER

namespace RimModManager.Steam
{
    using CefSharp;
    using CefSharp.Handler;

    internal class SteamWorkshopRequestHandler : RequestHandler
    {
        private readonly SteamWorkshopBrowser browser;


        public SteamWorkshopRequestHandler(SteamWorkshopBrowser browser)
        {
            this.browser = browser;
        }

        private static readonly string[] baseAddresses = ["https://steamcommunity.com/sharedfiles/filedetails/", "https://steamcommunity.com/workshop/filedetails/"];
        private const string targetParameter = "?id=";

        protected override bool OnBeforeBrowse(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame, IRequest request, bool userGesture, bool isRedirect)
        {
            bool result = false;
            foreach (var baseAddress in baseAddresses)
            {
                if (request.Url.StartsWith(baseAddress))
                {
                    var span = request.Url.AsSpan(baseAddress.Length);
                    int idx = span.IndexOf(targetParameter);
                    if (idx != -1)
                    {
                        span = span[(idx + targetParameter.Length)..];
                        int end = span.IndexOf('&');
                        if (end == -1) end = span.Length;
                        span = span[..end];
                        if (ulong.TryParse(span, out var steamId))
                        {
                            this.browser.SteamId = steamId;
                            result = true;
                            break;
                        }
                    }
                }
            }

            if (!result)
            {
                this.browser.SteamId = null;
            }

            return base.OnBeforeBrowse(chromiumWebBrowser, browser, frame, request, userGesture, isRedirect);
        }
    }
}

#endif