namespace RimModManager.RimWorld
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using Newtonsoft.Json.Linq;
    using RimModManager.RimWorld.Fluffy;
    using RimModManager.RimWorld.Sorting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.InteropServices;

    public class RimMod : INode<RimMod>
    {
        public const string CorePackageId = "ludeon.rimworld";

        public ModKind Kind { get; set; }

        public bool IsActive { get; set; }

        public string? Path { get; set; } = null!;

        public long? SteamId { get; set; }

        public ModMetadata Metadata { get; set; } = null!;

        public FluffyModManifest? FluffyManifest { get; set; }

        public string Name => Metadata.Name ?? Metadata.PackageId;

        public string PackageId => Metadata.PackageId;

        public string PreviewImagePath => System.IO.Path.Combine(Path ?? string.Empty, "About", "preview.png");

        public RimMessageCollection Messages { get; set; } = new() { CountInactive = true };

        public bool HasWarnings => Messages.WarningsCount > 0;

        public bool HasErrors { get; set; }

        public List<ModReference> LoadBefore { get; private set; } = [];

        public List<ModReference> LoadAfter { get; private set; } = [];

        IEnumerable<RimMod> INode<RimMod>.Dependencies => Dependencies;

        public bool? LoadBottom { get; set; }

        public List<RimMod> Dependencies = [];
        public List<RimMod> Dependants = [];

        public RimMod Clone()
        {
            return new()
            {
                Kind = Kind,
                IsActive = IsActive,
                Path = Path,
                SteamId = SteamId,
                Metadata = Metadata.Clone(),
                FluffyManifest = FluffyManifest?.Clone(),
                Messages = [.. Messages],
                LoadBefore = [.. LoadBefore],
                LoadAfter = [.. LoadAfter],
                LoadBottom = LoadBottom,
            };
        }

        public unsafe bool DrawMessages(StrBuilder builder, bool hovered, float width)
        {
            // ABGR
            const uint yellow = 0xff00ffff;
            const uint red = 0xff0000ff;

            var draw = ImGui.GetWindowDrawList();
            var style = ImGui.GetStyle();

            float lineHeight = ImGui.GetTextLineHeightWithSpacing();
            bool hoveredMessages = false;
            if (Messages.Count > 0)
            {
                Vector2 max = ImGui.GetCursorScreenPos() + new Vector2(width, 0);
                Vector2 min = max - new Vector2(0, lineHeight);
                min.Y += style.ItemSpacing.Y * 0.5f;

                if (Messages.ErrorsCount > 0)
                {
                    builder.Reset();
                    builder.Append(FontAwesome.CircleExclamation);
                    builder.End();
                    min.X -= lineHeight;
                    draw.AddText(min, red, builder);
                }

                if (Messages.WarningsCount > 0)
                {
                    builder.Reset();
                    builder.Append(FontAwesome.Warning);
                    builder.End();
                    min.X -= lineHeight;
                    draw.AddText(min, yellow, builder);
                }

                hoveredMessages = ImGui.IsMouseHoveringRect(min, max);
                if (hovered && hoveredMessages)
                {
                    if (ImGui.BeginTooltip())
                    {
                        foreach (var mes in Messages)
                        {
                            ImGui.Text(mes.Message);
                        }
                        ImGui.EndTooltip();
                    }
                }
            }
            return hoveredMessages;
        }

        public unsafe void DrawTooltip(StrBuilder builder)
        {
            if (ImGui.BeginTooltip())
            {
                ImGui.Text(BuildText(builder, "Name: "u8, Name));
                ImGui.Text(BuildTextList(builder, "Author: "u8, Metadata.Authors));
                ImGui.Text(BuildText(builder, "PackageID: "u8, PackageId));
                ImGui.Text(BuildText(builder, "Version: "u8, Metadata.ModVersion ?? "Unknown"));
                ImGui.Text(BuildText(builder, "Path: "u8, Path!));
                ImGui.EndTooltip();
            }
        }

        private static StrBuilder BuildText(StrBuilder builder, ReadOnlySpan<byte> label, string text)
        {
            builder.Reset();
            builder.Append(label);
            builder.Append(text);
            builder.End();
            return builder;
        }

        private static StrBuilder BuildTextList(StrBuilder builder, ReadOnlySpan<byte> label, List<string> texts)
        {
            builder.Reset();
            builder.Append(label);
            bool first = true;
            foreach (var text in texts)
            {
                if (!first)
                {
                    builder.Append(","u8);
                }
                first = false;

                builder.Append(text);
            }

            builder.End();
            return builder;
        }

        public void DrawContextMenu()
        {
            if (!ImGui.BeginPopupContextItem())
            {
                return;
            }

            if (Path != null)
            {
                if (ImGui.MenuItem("Open in Explorer"u8))
                {
                    OpenFolder(Path);
                }
            }

            if (ImGui.MenuItem("Open URL"u8))
            {
                if (SteamId.HasValue)
                {
                    OpenUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={SteamId.Value}");
                }
                else
                {
                    OpenUrl(Metadata.Url);
                }
            }

            if (SteamId.HasValue)
            {
                if (ImGui.MenuItem("Open in Steam"))
                {
                    OpenUrl($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={SteamId.Value}");
                }
            }

            ImGui.EndPopup();
        }

        private static void OpenUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return;
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
            }
            catch
            {
            }
        }

        private static void OpenFolder(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            try
            {
                ProcessStartInfo psi = new("explorer.exe") { UseShellExecute = true };
                psi.ArgumentList.Add(path);
                Process.Start(psi);
            }
            catch
            {
            }
        }

        public bool IsMod(string id)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(id, PackageId);
        }

        public void ClearSortingState()
        {
            Dependants.Clear();
            Dependencies.Clear();
        }

        public void AddMessage(string message, RimSeverity severity)
        {
            Messages.AddMessage(this, message, severity);
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }

        public char GetIcon()
        {
            return KindToIcon(Kind);
        }

        public Vector4 GetIconColor()
        {
            return KindToColor(Kind);
        }

        public static char KindToIcon(ModKind kind)
        {
            return kind switch
            {
                ModKind.Unknown => FontAwesome.CircleQuestion,
                ModKind.Base => FontAwesome.Star,
                ModKind.Local => FontAwesome.HardDrive,
                ModKind.Steam => FontAwesome.Steam,
                ModKind.All => FontAwesome.List,
                _ => FontAwesome.CircleQuestion
            };
        }

        public static Vector4 KindToColor(ModKind kind)
        {
            return kind switch
            {
                ModKind.Unknown => Colors.Crimson,
                ModKind.Base => Colors.Goldenrod,
                ModKind.Local => Colors.CadetBlue,
                ModKind.Steam => Colors.White,
                ModKind.All => Colors.White,
                _ => Colors.White
            };
        }

        public static RimMod CreateUnknown(string packageId)
        {
            return new() { Kind = ModKind.Unknown, IsActive = false, Metadata = new() { PackageId = packageId, Name = packageId, Authors = ["Unknown"], Description = "Unknown mod.", SupportedVersions = [] } };
        }

        public static RimMod CreateUnknown(string packageId, string name, long? steamId)
        {
            return new() { Kind = ModKind.Unknown, IsActive = false, SteamId = steamId, Metadata = new() { PackageId = packageId, Name = name, Authors = ["Unknown"], Description = "Unknown mod.", SupportedVersions = [] } };
        }

        public override string ToString()
        {
            return $"{Name} ({PackageId})";
        }
    }

    public enum RimSeverity
    {
        Info,
        Warn,
        Error,
    }

    public struct RimMessage
    {
        public RimMod Mod;
        public string Message;
        public RimSeverity Severity;

        public RimMessage(RimMod mod, string message, RimSeverity severity)
        {
            Mod = mod;
            Message = message;
            Severity = severity;
        }
    }
}