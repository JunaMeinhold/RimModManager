namespace RimModManager.Steam
{
    using CefSharp;
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.WebView;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;

    public class SteamWorkshopBrowser : ImWindow
    {
        private const string WorkshopUrl = "https://steamcommunity.com/app/294100/workshop/";
        private WebView webView = null!;
        private readonly SteamController steamController = new();

        private ulong? steamId;
        private bool subscribed;

        public override string Name { get; } = "Workshop Browser";

        public ulong? SteamId
        {
            get => steamId;
            set
            {
                if (value.HasValue)
                {
                    subscribed = steamController.IsWorkshopItemSubscribed(value.Value);
                }

                steamId = value;
            }
        }

        public override void Init()
        {
            steamController.Init();
            webView = new(WorkshopUrl);
            webView.LifeSpanHandler = new SteamWorkshopLifeSpanHandler(webView);
            webView.RequestHandler = new SteamWorkshopRequestHandler(this);
        }

        public override void Dispose()
        {
            webView.Dispose();
            steamController.Dispose();
        }

        public override void DrawWindow(ImGuiWindowFlags overwriteFlags)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0));
            base.DrawWindow(overwriteFlags);
            ImGui.PopStyleVar();
        }

        public override unsafe void DrawContent()
        {
            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            ImGui.BeginChild("MainPanel"u8);

            if (ImGui.Button(builder.BuildLabel(MaterialIcons.ArrowBack)))
            {
                if (webView.CanGoBack)
                {
                    webView.Back();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button(builder.BuildLabel(MaterialIcons.ArrowForward)))
            {
                if (webView.CanGoForward)
                {
                    webView.Forward();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button(builder.BuildLabel(MaterialIcons.Refresh)))
            {
                webView.Reload();
            }
            ImGui.SameLine();
            if (ImGui.Button(builder.BuildLabel(MaterialIcons.Home)))
            {
                webView.LoadUrl(WorkshopUrl);
            }
            ImGui.SameLine();
            string address = webView.Address;
            if (ImGui.InputText("##Url"u8, ref address, 2048, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                webView.LoadUrl(address);
            }

            if (steamId.HasValue)
            {
                ImGui.SameLine();

                if (subscribed)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, 0xff2d00e1);
                    if (ImGui.Button(builder.BuildLabel(MaterialIcons.Remove, " Unsubscribe"u8)))
                    {
                        steamController.UnsubscribeToWorkshopItem(steamId.Value);
                        subscribed = false;
                    }
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, 0xff107e5c);
                    if (ImGui.Button(builder.BuildLabel(MaterialIcons.Add, " Subscribe"u8)))
                    {
                        steamController.SubscribeToWorkshopItem(steamId.Value);
                        subscribed = true;
                    }
                }

                ImGui.PopStyleColor();
            }

            webView.Size = (Hexa.NET.Mathematics.Point2)ImGui.GetContentRegionAvail();
            webView.Draw("WebView"u8);
            bool isHovered = ImGui.IsItemHovered();

            if (isHovered)
            {
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle + 1))
                {
                    if (webView.CanGoBack)
                    {
                        webView.Back();
                    }
                }
                if (ImGui.IsMouseClicked(ImGuiMouseButton.Middle + 2))
                {
                    if (webView.CanGoForward)
                    {
                        webView.Forward();
                    }
                }
            }

            ImGui.EndChild();
        }
    }

    public static unsafe class StrBuilderExtensions
    {
        public static byte* BuildLabel(this StrBuilder builder, char icon)
        {
            builder.Reset();
            builder.Append(icon);
            builder.End();
            return builder;
        }

        public static byte* BuildLabel(this StrBuilder builder, char icon, ReadOnlySpan<byte> text)
        {
            builder.Reset();
            builder.Append(icon);
            builder.Append(text);
            builder.End();
            return builder;
        }
    }
}