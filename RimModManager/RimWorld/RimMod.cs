namespace RimModManager.RimWorld
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld.Fluffy;
    using RimModManager.RimWorld.Sorting;
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

        public List<RimMessage> Messages { get; } = [];

        public bool HasWarnings { get; set; }

        public bool HasErrors { get; set; }

        public List<ModReference> LoadBefore { get; } = [];

        public List<ModReference> LoadAfter { get; } = [];

        IEnumerable<RimMod> INode<RimMod>.Dependencies => Dependencies;

        public bool? LoadBottom { get; set; }

        public List<RimMod> Dependencies = [];
        public List<RimMod> Dependants = [];

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
            if (severity == RimSeverity.Warn) HasWarnings = true;
            if (severity == RimSeverity.Error) HasErrors = true;
            RimMessage msg = new(this, message, severity);
            Messages.Add(msg);
        }

        public void ClearMessages()
        {
            HasWarnings = false;
            HasErrors = false;
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
                ModKind.Unknown => Colors.White,
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