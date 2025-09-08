namespace RimModManager.RimWorld
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities;
    using Hexa.NET.Utilities.Text;
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

        public bool IsBaseMod => Kind == ModKind.Base;

        public bool IsLocalMod => Kind == ModKind.Local;

        public bool IsSteamMod => Kind == ModKind.Steam;

        public ModFlags Flags { get; set; }

        public bool IsUpdateAvailable => (Flags & ModFlags.UpdateAvailable) != 0;

        public bool IsActive { get; set; }

        public string? Path { get; set; } = null!;

        public long? SteamId { get; set; }

        public ModMetadata Metadata { get; set; } = null!;

        public FluffyModManifest? FluffyManifest { get; set; }

        public string Name => Metadata.Name ?? Metadata.PackageId;

        public string PackageId => Metadata.PackageId;

        public string PreviewImagePath => System.IO.Path.Combine(Path ?? string.Empty, "About", "preview.png");

        public RimMessageCollection Messages { get; set; } = new() { CountInactive = true };

        public bool HasWarnings => Messages.WarningsCount != 0;

        public bool HasErrors => Messages.ErrorsCount != 0;

        public ModReferenceCollection LoadBefore { get; private set; } = [];

        public ModReferenceCollection LoadAfter { get; private set; } = [];

        IEnumerable<RimMod> INode<RimMod>.Dependencies => Dependencies;

        public bool? LoadBottom { get; set; }

        public List<RimMod> Dependencies { get; } = [];

        public List<RimMod> Dependants { get; } = [];

        public Dictionary<string, RimProperty> Properties { get; } = [];

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

        public static unsafe bool LeafButton(byte* label, uint color, ref Vector2 cursor, float size)
        {
            return LeafButton(label, color, ref cursor, new Vector2(size));
        }

        public static unsafe bool LeafButton(byte* label, uint color, ref Vector2 cursor, Vector2 size)
        {
            var draw = ImGui.GetWindowDrawList();
            cursor.X -= size.X;

            ImRect rect = new(cursor, cursor + size);

            var id = ImGui.GetID(label);
            if (!ImGuiP.ItemAdd(rect, id))
            {
                return false;
            }

            var end = ImGuiP.FindRenderedTextEnd(label);
            draw.AddText(cursor, color, label, end);
            bool itemHovered = false, held = false;

            return ImGuiP.ButtonBehavior(rect, id, ref itemHovered, ref held);
        }

        public unsafe bool DrawMessages(StrBuilder builder, bool hovered, float width)
        {
            // ABGR
            const uint yellow = 0xff00ffff;
            const uint red = 0xff0000ff;
            uint lightSkyBlue = Colors.LightSkyBlue.ToUIntABGR();

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
                    builder.Append("##"u8);
                    builder.Append(PackageId);
                    builder.End();

                    LeafButton(builder, red, ref min, lineHeight);

                    if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
                    {
                        hoveredMessages = true;
                        foreach (var mes in Messages)
                        {
                            if (mes.Severity != RimSeverity.Error) continue;
                            ImGui.Text(mes.Message);
                        }
                        ImGui.EndTooltip();
                    }
                }

                if (Messages.WarningsCount > 0)
                {
                    builder.Reset();
                    builder.Append(FontAwesome.Warning);
                    builder.Append("##"u8);
                    builder.Append(PackageId);
                    builder.End();

                    LeafButton(builder, yellow, ref min, lineHeight);
                    if (ImGui.IsItemHovered() && ImGui.BeginTooltip())
                    {
                        hoveredMessages = true;
                        foreach (var mes in Messages)
                        {
                            if (mes.Severity != RimSeverity.Warn) continue;
                            ImGui.Text(mes.Message);
                        }
                        ImGui.EndTooltip();
                    }
                }

                if (IsUpdateAvailable)
                {
                    builder.Reset();
                    builder.Append(FontAwesome.Download);
                    builder.Append("##"u8);
                    builder.Append(PackageId);
                    builder.End();

                    LeafButton(builder, lightSkyBlue, ref min, lineHeight);
                    if (ImGui.IsItemHovered())
                    {
                        hoveredMessages = true;
                        ImGui.SetTooltip("Updates available!");
                    }
                }
            }
            return hoveredMessages;
        }

        public unsafe void DrawTooltip(StrBuilder builder)
        {
            if (ImGui.BeginItemTooltip())
            {
                ImGui.Text(BuildText(builder, "Name: "u8, Name));
                ImGui.Text(BuildTextList(builder, "Author: "u8, Metadata.Authors));
                ImGui.Text(BuildText(builder, "PackageID: "u8, PackageId));
                ImGui.Text(BuildText(builder, "Version: "u8, Metadata.ModVersion ?? "Unknown"));
                if (Kind != ModKind.Unknown)
                {
                    ImGui.Text(BuildText(builder, "Path: "u8, Path!));
                }

                ImGui.EndTooltip();
            }
        }

        public StrBuilder BuildLabel(StrBuilder builder, int id)
        {
            builder.Reset();
            builder.Append(Metadata.Name ?? Metadata.PackageId);
            builder.Append("##"u8);
            builder.Append(id);
            builder.End();
            return builder;
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

        public unsafe void DrawIcon(StrBuilder builder)
        {
            builder.Reset();
            builder.Append(GetIcon());
            builder.End();
            ImGui.TextColored(GetIconColor(), builder);
        }

        public void DrawContextMenu(RimLoadOrder loadOrder)
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

            if (ImGui.MenuItem("Edit Rules"))
            {
                ModRuleDialog dialog = new(this, loadOrder);
                dialog.Show();
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
}