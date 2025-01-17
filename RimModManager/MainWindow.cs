﻿namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;
    using RimModManager.TextureOptimizer;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using System.Runtime.InteropServices;

    public class MainWindow : ImWindow
    {
        private readonly RimModManagerConfig config;
        private RimModList? mods;
        private ModsConfig? modsConfig;
        private RimMod? selectedMod;

        private readonly Lock imageLock = new();
        private Image2D? previewImage;

        private float splitA = 600;
        private float splitB = 300;

        private FilterState? inactiveFilterState;
        private FilterState? activeFilterState;

        private Task? refreshTask;

        public MainWindow()
        {
            Flags |= ImGuiWindowFlags.MenuBar;
            config = RimModManagerConfig.Load(out var isNew);
            if (isNew)
            {
                if (RimPathAutoDetector.Detect(config))
                {
                    config.Save();
                }
                else
                {
                    SelectPathDialog dialog = new(config);
                    dialog.Show((s, r) => { Refresh(); });
                }
            }
            else
            {
                if (!config.CheckPaths())
                {
                    if (RimPathAutoDetector.Detect(config))
                    {
                        config.Save();
                    }
                    else
                    {
                        SelectPathDialog dialog = new(config);
                        dialog.Show((s, r) => { Refresh(); });
                    }
                }
            }
        }

        private void Refresh()
        {
            if (refreshTask != null && !refreshTask.IsCompleted) return;
            refreshTask = Task.Run(() =>
            {
                if (!config.CheckPaths()) return;
                mods = RimModLoader.LoadMods(config);
                modsConfig = ModsConfig.Load(config, mods);
                modsConfig.CheckForProblems();

                inactiveFilterState = new(modsConfig.InactiveMods);
                activeFilterState = new(modsConfig.ActiveMods);
            });
        }

        public RimMod? SelectedMod
        {
            get => selectedMod;
            set
            {
                if (selectedMod == value)
                {
                    return;
                }

                selectedMod = value;
                Task.Run(() =>
                {
                    lock (imageLock)
                    {
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
                });
            }
        }

        protected override string Name { get; } = "Main Window";

        public override void Init()
        {
            Refresh();
        }

        public override void Dispose()
        {
            lock (imageLock)
            {
                previewImage?.Dispose();
            }
        }

        public override unsafe void DrawContent()
        {
            var avail = ImGui.GetContentRegionAvail();

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.MenuItem("File"u8))
                {
                }
                if (ImGui.MenuItem("Edit"u8))
                {
                }
                if (ImGui.MenuItem("Textures"u8))
                {
                    WidgetManager.Register(new OptimizeTexturesWindow(config), true);
                }
                if (ImGui.MenuItem("Help"u8))
                {
                }

                ImGui.EndMenuBar();
            }

            var style = ImGui.GetStyle();
            var height = ImGui.GetTextLineHeight() + style.FramePadding.Y * 2 + style.ItemSpacing.Y + style.FrameBorderSize * 2;
            if (ImGui.BeginChild("SidePanel"u8, new(splitA, 0), ImGuiChildFlags.None))
            {
                var d = ImGui.GetContentRegionAvail();
                DisplayInactive("##ModsPanel"u8, "Inactive"u8, new(splitB, -height));
                ImGuiSplitter.VerticalSplitter("SplitB"u8, ref splitB, 0, float.MaxValue, -height);
                DisplayActive("##LoadOrder"u8, "Active"u8, new(0, -height));

                ImGui.BeginChild("dawd"u8);
                byte* buffer = stackalloc byte[2048];
                StrBuilder builder = new(buffer, 2048);

                if (ImGui.Button(BuildLabel(builder, MaterialIcons.Refresh)))
                {
                    Refresh();
                }
                ImGui.SameLine();
                if (ImGui.Button("Clear"u8))
                {
                    modsConfig?.Clear();
                    RefreshUI();
                }
                ImGui.SameLine();
                if (ImGui.Button("Restore"u8))
                {
                    Refresh();
                }
                ImGui.SameLine();
                if (ImGui.Button("Sort"u8))
                {
                    modsConfig?.Sort();
                    RefreshUI();
                }
                ImGui.SameLine();
                if (ImGui.Button("Save"u8))
                {
                    modsConfig?.Save(config);
                }
                ImGui.SameLine();
                if (ImGui.Button("Run"u8))
                {
                    config.LaunchGame();
                }

                ImGui.EndChild();
            }
            ImGui.EndChild();

            ImGuiSplitter.VerticalSplitter("SplitA"u8, ref splitA, 100, avail.X - 100);
            DrawSelection(new(0, 0), selectedMod);
        }

        private void RefreshUI()
        {
            inactiveFilterState?.Refresh();
            activeFilterState?.Refresh();
        }

        private void DrawSelection(Vector2 size, RimMod? selectedMod)
        {
            if (!ImGui.BeginChild("Selection"u8, size, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar))
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

            ImGui.Separator();

            if (selectedMod.Metadata.Description != null)
            {
                ImGui.Text(selectedMod.Metadata.Description);
            }

            ImGui.EndChild();
        }

        private unsafe void DisplayInactive(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size) || inactiveFilterState == null)
            {
                ImGui.EndChild();
                return;
            }

            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);
            BuildLabel(label, ref builder, inactiveFilterState.Mods.TotalCount, inactiveFilterState.Mods.Count);

            ImGuiManager.PushFont("FA");
            DrawFilterBar(strId, builder, inactiveFilterState);

            var avail = ImGui.GetContentRegionAvail();
            DisplayMods(strId, label, inactiveFilterState.Mods, avail);
            ImGuiManager.PopFont();
            ImGui.EndChild();
        }

        private static unsafe void DrawFilterBar(ReadOnlySpan<byte> strId, StrBuilder builder, FilterState state)
        {
            builder.Reset();
            builder.Append(KindToIcon(state.Filter));
            builder.End();
            ImGui.PushStyleColor(ImGuiCol.Text, KindToColor(state.Filter));
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
            ImGui.PopStyleColor();
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
            if (!ImGui.BeginChild(strId, size) || activeFilterState == null || modsConfig == null)
            {
                ImGui.EndChild();
                return;
            }

            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);
            BuildLabel(label, ref builder, activeFilterState.Mods.TotalCount, activeFilterState.Mods.Count);
            ImGuiManager.PushFont("FA");
            DrawFilterBar(strId, builder, activeFilterState);

            var avail = ImGui.GetContentRegionAvail();
            avail.Y -= ImGui.GetTextLineHeightWithSpacing();
            DisplayMods(strId, label, activeFilterState.Mods, avail);

            if (modsConfig.WarningsCount > 0)
            {
                builder.Reset();
                builder.Append(FontAwesome.Warning);
                builder.Append(modsConfig.WarningsCount);
                builder.End();
                ImGui.TextColored(Colors.Yellow, builder);
                DisplayMessages(builder, modsConfig, RimSeverity.Warn);
            }

            if (modsConfig.ErrorsCount > 0)
            {
                if (modsConfig.WarningsCount > 0)
                {
                    ImGui.SameLine();
                }

                builder.Reset();
                builder.Append(FontAwesome.CircleExclamation);
                builder.Append(modsConfig.ErrorsCount);
                builder.End();
                ImGui.TextColored(Colors.Red, builder);
                DisplayMessages(builder, modsConfig, RimSeverity.Error);
            }
            ImGuiManager.PopFont();
            ImGui.EndChild();
        }

        private unsafe void DisplayMessages(StrBuilder builder, ModsConfig modsConfig, RimSeverity severity)
        {
            if (ImGui.BeginItemTooltip())
            {
                foreach (var msg in modsConfig.Messages)
                {
                    if (msg.Severity != severity) continue;
                    ImGui.Text(BuildModLabel(builder, msg.Mod));
                    ImGui.Indent();
                    ImGui.Text(msg.Message);
                    ImGui.Unindent();
                }
                ImGui.EndTooltip();
            }
        }

        private static unsafe byte* BuildModLabel(StrBuilder builder, RimMod mod)
        {
            builder.Reset();
            builder.Append(mod.Name);
            builder.End();
            return builder;
        }

        private static unsafe void BuildLabel(ReadOnlySpan<byte> label, ref StrBuilder builder, int totalCount, int modCount)
        {
            var size = ImGui.GetWindowSize();
            builder.Append(label);
            builder.Append(" ["u8);
            if (totalCount != modCount)
            {
                builder.Append(modCount);
                builder.Append("/"u8);
                builder.Append(totalCount);
            }
            else
            {
                builder.Append(modCount);
            }
            builder.Append("]"u8);
            builder.End();
            var text = ImGui.CalcTextSize(builder);
            var c = ImGui.GetCursorPos();
            ImGui.SetCursorPosX(c.X + (size.X - text.X) * 0.5f);
            ImGui.Text(builder);
        }

        private unsafe void DisplayMods(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, IReadOnlyList<RimMod> mods, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size, ImGuiChildFlags.FrameStyle) || modsConfig == null || inactiveFilterState == null || activeFilterState == null)
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

            // ABGR
            var yellow = 0xff00ffff;
            var red = 0xff0000ff;
            for (int i = start; i < end; i++)
            {
                var mod = mods[i];

                var dropRectMin = ImGui.GetCursorScreenPos();
                builder.Reset();
                builder.Append(KindToIcon(mod.Kind));
                builder.End();
                ImGui.TextColored(KindToColor(mod.Kind), builder);
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

                if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
                {
                    ImGui.SetDragDropPayload("RimMod"u8, &i, sizeof(int));
                    builder.Reset();
                    builder.Append(KindToIcon(mod.Kind));
                    builder.End();
                    ImGui.TextColored(KindToColor(mod.Kind), builder);
                    ImGui.SameLine();
                    ImGui.Text(mod.Name);
                    ImGui.EndDragDropSource();
                }

                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("RimMod"u8, ImGuiDragDropFlags.AcceptNoDrawDefaultRect | ImGuiDragDropFlags.AcceptBeforeDelivery);

                    if (!payload.IsNull)
                    {
                        int index = *(int*)payload.Data;
                        Vector2 dropRectMax = dropRectMin + new Vector2(avail.X, lineHeight);
                        if (index > i)
                        {
                            dropRectMax.Y = dropRectMin.Y;
                        }
                        else
                        {
                            dropRectMin.Y = dropRectMax.Y;
                        }

                        draw.AddLine(dropRectMin, dropRectMax, ImGui.GetColorU32(ImGuiCol.DragDropTarget), 3.5f);

                        if (payload.IsDelivery())
                        {
                            modsConfig.Move(mods[index], i);
                            RefreshUI();
                        }
                    }
                    ImGui.EndDragDropTarget();
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
                    inactiveFilterState.Refresh();
                    activeFilterState.Refresh();
                }

                bool hoveredMessages = false;
                if (mod.Messages.Count > 0)
                {
                    Vector2 max = ImGui.GetCursorScreenPos() + new Vector2(avail.X, 0);
                    Vector2 min = max - new Vector2(0, lineHeight);

                    if (mod.HasErrors)
                    {
                        builder.Reset();
                        builder.Append(FontAwesome.CircleExclamation);
                        builder.End();
                        min.X -= warnWidth;
                        draw.AddText(min, red, buffer);
                    }
                    else if (mod.HasWarnings)
                    {
                        builder.Reset();
                        builder.Append(FontAwesome.Warning);
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
                ModKind.Unknown => FontAwesome.CircleQuestion,
                ModKind.Base => FontAwesome.Star,
                ModKind.Local => FontAwesome.HardDrive,
                ModKind.Steam => FontAwesome.Steam,
                ModKind.All => FontAwesome.List,
                _ => FontAwesome.CircleQuestion
            };
        }

        private static Vector4 KindToColor(ModKind kind)
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

        private static void ContextMenu(RimMod mod)
        {
            if (!ImGui.BeginPopupContextItem())
            {
                return;
            }

            if (ImGui.MenuItem("Open in Explorer"u8))
            {
                OpenFolder(mod.Path);
            }

            if (ImGui.MenuItem("Open URL"u8))
            {
                if (mod.SteamId.HasValue)
                {
                    OpenUrl($"https://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamId.Value}");
                }
                else
                {
                    OpenUrl(mod.Metadata.Url);
                }
            }

            if (mod.SteamId.HasValue)
            {
                if (ImGui.MenuItem("Open in Steam"))
                {
                    OpenUrl($"steam://openurl/https://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamId.Value}");
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

        private static StrBuilder BuildLabel(StrBuilder builder, char icon)
        {
            builder.Reset();
            builder.Append(icon);
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
    }
}