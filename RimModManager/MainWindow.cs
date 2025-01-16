namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.InteropServices;

    public class MainWindow : ImWindow
    {
        private readonly RimModList mods;
        private readonly ModsConfig modsConfig;
        private RimMod? selectedMod;

        private Image2D? previewImage;

        private float splitA = 600;
        private float splitB = 300;

        private FilterState inactiveFilterState;
        private FilterState activeFilterState;

        public MainWindow()
        {
            RimModManagerConfig config = new();
            RimPathAutoDetector.Detect(config);

            mods = RimModLoader.LoadMods(config);
            modsConfig = ModsConfig.Load(config, mods);
            modsConfig.CheckForProblems();

            Flags |= ImGuiWindowFlags.MenuBar;

            inactiveFilterState = new(modsConfig.InactiveMods);
            activeFilterState = new(modsConfig.ActiveMods);
        }

        public RimMod? SelectedMod
        {
            get => selectedMod;
            set
            {
                selectedMod = value;
                previewImage?.Dispose();
                previewImage = null;
                if (selectedMod != null && File.Exists(selectedMod.PreviewImagePath))
                {
                    try
                    {
                        previewImage = Image2D.LoadFromFile(selectedMod.PreviewImagePath);
                    }
                    catch
                    {
                        previewImage = null;
                    }
                }
            }
        }

        protected override string Name { get; } = "Main Window";

        public override unsafe void DrawContent()
        {
            var avail = ImGui.GetContentRegionAvail();

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.MenuItem("Clear"u8))
                {
                    modsConfig.Clear();
                }
                if (ImGui.MenuItem("Restore"u8))
                {
                }
                if (ImGui.MenuItem("Sort"u8))
                {
                    modsConfig.Sort();
                }
                if (ImGui.MenuItem("Save"u8))
                {
                }

                ImGui.EndMenuBar();
            }

            if (ImGui.BeginChild("SidePanel"u8, new(splitA, 0), ImGuiChildFlags.None))
            {
                DisplayInactive("##ModsPanel"u8, "Inactive"u8, new(splitB, 0));
                ImGuiSplitter.VerticalSplitter("SplitB"u8, ref splitB);
                DisplayActive("##LoadOrder"u8, "Active"u8, default);
            }
            ImGui.EndChild();
            ImGuiSplitter.VerticalSplitter("SplitA"u8, ref splitA, 100, avail.X - 100);
            DrawSelection(new(0, 0));
        }

        private void DrawSelection(Vector2 size)
        {
            if (!ImGui.BeginChild("Selection"u8, size, ImGuiChildFlags.None))
            {
                ImGui.EndChild();
                return;
            }

            if (selectedMod == null)
            {
                ImGui.EndChild();
                return;
            }

            var avail = ImGui.GetContentRegionAvail();

            var imageContent = avail * new Vector2(1, 0.6f);

            if (previewImage != null)
            {
                var cur = ImGui.GetCursorPos();
                Vector2 imageSize = new(previewImage.Width, previewImage.Height);
                Vector2 scale = imageContent / imageSize;
                float scaleMin = Math.Min(scale.X, scale.Y);
                ImGui.Image(previewImage, imageSize * scaleMin);
                ImGui.SetCursorPos(cur);
            }
            ImGui.Dummy(imageContent);

            ImGui.Separator();

            ImGui.Text("Name: "u8);
            ImGui.SameLine();
            ImGui.Text(selectedMod.Metadata.Name ?? "<unknown>");
            ImGui.TextLinkOpenURL(selectedMod.Path, selectedMod.Path);
            ImGui.Text("Package Id: "u8);
            ImGui.SameLine();
            ImGui.Text(selectedMod.PackageId);

            ImGui.EndChild();
        }

        private unsafe void DisplayInactive(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size))
            {
                ImGui.EndChild();
                return;
            }

            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);
            BuildLabel(label, ref builder, inactiveFilterState.Mods.Count);

            DrawFilterBar(strId, builder, inactiveFilterState);

            var avail = ImGui.GetContentRegionAvail();
            DisplayMods(strId, label, inactiveFilterState.Mods, avail);
            ImGui.EndChild();
        }

        private static unsafe void DrawFilterBar(ReadOnlySpan<byte> strId, StrBuilder builder, FilterState state)
        {
            builder.Reset();
            builder.Append(KindToIcon(state.Filter));
            builder.End();
            if (ImGui.Button(builder))
            {
                var filter = state.Filter;
                if (filter == ModKind.All)
                {
                    filter = ModKind.Base;
                }
                else
                {
                    filter++;
                }
                state.Filter = filter;
            }
            ImGui.SameLine();

            var avail = ImGui.GetContentRegionAvail();
            var style = ImGui.GetStyle();
            var size = ImGui.CalcTextSize("PackageId"u8).X + style.ItemSpacing.X + style.FramePadding.X * 2;
            ImGui.SetNextItemWidth(avail.X - size);

            var searchString = state.SearchString;
            if (ImGui.InputTextWithHint(strId, "Search ..."u8, ref searchString, 1024))
            {
                state.SearchString = searchString;
            }

            ImGui.SameLine();
            int mode = (int)state.FilterMode;
            if (ImGui.Combo("##Combo", ref mode, "Name\0Autor\0Path\0PackageId\0Messages\0"u8))
            {
                state.FilterMode = (FilterMode)mode;
            }
        }

        private unsafe void DisplayActive(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size))
            {
                ImGui.EndChild();
                return;
            }

            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);
            BuildLabel(label, ref builder, activeFilterState.Mods.Count);

            DrawFilterBar(strId, builder, activeFilterState);

            var avail = ImGui.GetContentRegionAvail();
            avail.Y -= ImGui.GetTextLineHeightWithSpacing();
            DisplayMods(strId, label, activeFilterState.Mods, avail);

            if (modsConfig.WarningsCount > 0)
            {
                builder.Reset();
                builder.Append(MaterialIcons.Warning);
                builder.Append(modsConfig.WarningsCount);
                builder.End();
                ImGui.TextColored(Colors.Yellow, builder);
            }

            if (modsConfig.ErrorsCount > 0)
            {
                if (modsConfig.WarningsCount > 0)
                {
                    ImGui.SameLine();
                }

                builder.Reset();
                builder.Append(MaterialIcons.Error);
                builder.Append(modsConfig.ErrorsCount);
                builder.End();
                ImGui.TextColored(Colors.Red, builder);
            }

            ImGui.EndChild();
        }

        private static unsafe void BuildLabel(ReadOnlySpan<byte> label, ref StrBuilder builder, int modCount)
        {
            var size = ImGui.GetWindowSize();
            builder.Append(label);
            builder.Append(' ');
            builder.Append(modCount);
            builder.End();
            var text = ImGui.CalcTextSize(builder);
            var c = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(c.X + (size.X - text.X) * 0.5f);
            ImGui.Text(builder);
        }

        private unsafe void DisplayMods(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, IReadOnlyList<RimMod> mods, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size, ImGuiChildFlags.FrameStyle))
            {
                ImGui.EndChild();
                return;
            }

            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            var avail = ImGui.GetContentRegionAvail();
            var scroll = ImGui.GetScrollY();
            var lineHeight = ImGui.GetTextLineHeightWithSpacing();

            int start = (int)Math.Floor(scroll / lineHeight) - 1;
            int end = (int)Math.Ceiling(avail.Y / lineHeight) + start + 2;

            start = Math.Max(start, 0);
            end = Math.Min(end, mods.Count);

            if (start > 0)
            {
                ImGui.Dummy(new(1, start * lineHeight));
            }

            builder.Reset();
            builder.Append(MaterialIcons.Warning);
            builder.End();
            float warnWidth = ImGui.CalcTextSize(buffer).X;
            var draw = ImGui.GetWindowDrawList();

            var yellow = 0xff00ffff;
            var red = 0xff0000ff;
            for (int i = start; i < end; i++)
            {
                var mod = mods[i];

                builder.Reset();
                builder.Append(KindToIcon(mod.Kind));
                builder.End();
                ImGui.Text(builder);
                ImGui.SameLine();

                builder.Reset();
                builder.Append(mod.Metadata.Name ?? mod.Metadata.PackageId);
                builder.Append("##"u8);
                builder.Append(i);
                builder.End();
                if (ImGui.Selectable(builder, SelectedMod == mod))
                {
                    SelectedMod = mod;
                }

                ContextMenu(mod);

                var hovered = ImGui.IsItemHovered();
                if (hovered && ImGuiP.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    if (mod.IsActive)
                    {
                        modsConfig.DeactiveMod(mod);
                    }
                    else
                    {
                        modsConfig.ActivateMod(mod);
                    }
                }

                bool hoveredMessages = false;
                if (mod.Messages.Count > 0)
                {
                    Vector2 max = ImGui.GetCursorScreenPos() + new Vector2(avail.X, 0);
                    Vector2 min = max - new Vector2(0, lineHeight);

                    if (mod.HasErrors)
                    {
                        builder.Reset();
                        builder.Append(MaterialIcons.Error);
                        builder.End();
                        min.X -= warnWidth;
                        draw.AddText(min, red, buffer);
                    }
                    else if (mod.HasWarnings)
                    {
                        builder.Reset();
                        builder.Append(MaterialIcons.Warning);
                        builder.End();
                        min.X -= warnWidth;
                        draw.AddText(min, yellow, buffer);
                    }

                    hoveredMessages = ImGui.IsMouseHoveringRect(min, max);
                    if (hovered && hoveredMessages)
                    {
                        if (ImGui.BeginTooltip())
                        {
                            foreach (var mes in mod.Messages)
                            {
                                ImGui.Text(mes.Message);
                            }
                            ImGui.EndTooltip();
                        }
                    }
                }

                if (hovered && !hoveredMessages)
                {
                    if (ImGui.BeginTooltip())
                    {
                        ImGui.Text(BuildText(builder, "Name: "u8, mod.Name));
                        ImGui.Text(BuildTextList(builder, "Author: "u8, mod.Metadata.Authors));
                        ImGui.Text(BuildText(builder, "PackageID: "u8, mod.PackageId));
                        ImGui.Text(BuildText(builder, "Version: "u8, mod.Metadata.ModVersion ?? "Unknown"));
                        ImGui.Text(BuildText(builder, "Path: "u8, mod.Path!));
                        ImGui.EndTooltip();
                    }
                }
            }

            int delta = mods.Count - end;
            if (delta > 0)
            {
                ImGui.Dummy(new(0, delta * lineHeight));
            }

            ImGui.EndChild();
        }

        private static char KindToIcon(ModKind kind)
        {
            return kind switch
            {
                ModKind.Unknown => MaterialIcons.QuestionMark,
                ModKind.Base => MaterialIcons.CheckCircle,
                ModKind.Local => MaterialIcons.Computer,
                ModKind.Steam => MaterialIcons.Public,
                ModKind.All => MaterialIcons.ClearAll,
                _ => MaterialIcons.QuestionMark
            };
        }

        private static void ContextMenu(RimMod mod)
        {
            if (!ImGui.BeginPopupContextItem())
            {
                return;
            }

            if (ImGui.MenuItem("Open in Explorer"))
            {
                OpenFolder(mod.Path);
            }

            if (ImGui.MenuItem("Open URL"))
            {
                OpenUrl(mod.Metadata.Url);
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
    }
}