namespace RimModManager
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.ImGui.Widgets.Dialogs;
    using Hexa.NET.KittyUI.Graphics;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.Mathematics;
    using Hexa.NET.Utilities.Text;
    using RimModManager.RimWorld;
    using RimModManager.RimWorld.Profiles;
    using RimModManager.TextureOptimizer;
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class MainWindow : ImWindow
    {
        private readonly RimModManagerConfig config;
        private readonly RimProfileManager profileManager = new();

        private RimModList? mods;
        private RimLoadOrder? loadOrder;
        private RimMod? selectedMod;

        private readonly Lock imageLock = new();
        private Image2D? previewImage;

        private float splitA = 600;
        private float splitB = 300;

        private FilterState? inactiveFilterState;
        private FilterState? activeFilterState;

        private Task? refreshTask;

        private bool refreshUI = false;
        private int focusOnIndex = -1;
        private FilteredList<RimMod>? focusOnTarget;

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
                RimModLoader.RefreshMods(config);
                mods = RimModLoader.Current;
                loadOrder = ModsConfig.Load(config, mods);
                loadOrder.CheckForProblems();

                inactiveFilterState = new(loadOrder.InactiveMods);
                activeFilterState = new(loadOrder.ActiveMods);
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
                if (ImGui.BeginMenu("File"u8))
                {
                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem("Edit"u8))
                {
                }
                if (ImGui.BeginMenu("Profiles"u8))
                {
                    if (ImGui.MenuItem("Create new"u8) && loadOrder != null)
                    {
                        profileManager.Create(loadOrder);
                    }

                    if (ImGui.MenuItem("Manage Profiles"u8) && mods != null)
                    {
                        profileManager.OpenProfileManager(mods);
                    }
                    ImGui.SeparatorText("Apply Profile"u8);

                    foreach (var profile in profileManager.Profiles)
                    {
                        if (ImGui.MenuItem(profile.Name) && loadOrder != null && mods != null)
                        {
                            ApplyProfileDialog dialog = new(profile, loadOrder, mods);
                            dialog.Show();
                        }
                    }

                    ImGui.EndMenu();
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

                if (refreshUI)
                {
                    inactiveFilterState?.Refresh();
                    activeFilterState?.Refresh();
                    refreshUI = false;
                }

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
                    loadOrder?.Clear();
                    RefreshUI();
                }
                ImGui.SameLine();
                if (ImGui.Button("Restore"u8))
                {
                    Refresh();
                }
                ImGui.SameLine();
                if (ImGui.Button("Sort"u8) && loadOrder != null && mods != null)
                {
                    HashSet<RimMod>? missingMods = null;
                    foreach (var mod in loadOrder.FindMissingDependencies(mods))
                    {
                        missingMods ??= []; // lazy init.
                        missingMods.Add(mod);
                    }

                    if (missingMods != null)
                    {
                        MissingDependenciesDialog dialog = new(loadOrder, missingMods);
                        dialog.Show((s, r) =>
                        {
                            if (r != DialogResult.Ok) return;
                            Sort();
                        });
                    }
                    else
                    {
                        Sort();
                    }
                }
                ImGui.SameLine();
                if (ImGui.Button("Save"u8) && loadOrder != null)
                {
                    ModsConfig.Save(config, loadOrder);
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

        private void Sort()
        {
            loadOrder?.Sort();
            RefreshUI();
        }

        private void RefreshUI(int focusOnIndex = -1, FilteredList<RimMod>? focusOnTarget = null)
        {
            refreshUI = true;
            this.focusOnIndex = focusOnIndex;
            this.focusOnTarget = focusOnTarget;
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
            lock (imageLock)
            {
                if (previewImage != null)
                {
                    var cur = ImGui.GetCursorPos();
                    Vector2 imageSize = new(previewImage.Width, previewImage.Height);
                    Vector2 scale = imageContent / imageSize;
                    float scaleMin = Math.Min(scale.X, scale.Y);
                    ImGui.Image(previewImage, imageSize * scaleMin);
                    ImGui.SetCursorPos(cur);
                }
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
            builder.Append(RimMod.KindToIcon(state.Filter));
            builder.End();
            ImGui.PushStyleColor(ImGuiCol.Text, RimMod.KindToColor(state.Filter));
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
            if (!ImGui.BeginChild(strId, size) || activeFilterState == null || loadOrder == null)
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

            loadOrder.Messages.DrawBar(builder);

            ImGuiManager.PopFont();
            ImGui.EndChild();
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

        private unsafe void DisplayMods(ReadOnlySpan<byte> strId, ReadOnlySpan<byte> label, FilteredList<RimMod> mods, Vector2 size)
        {
            if (!ImGui.BeginChild(strId, size, ImGuiChildFlags.FrameStyle) || loadOrder == null || inactiveFilterState == null || activeFilterState == null)
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
            ImGui.PushStyleColor(ImGuiCol.NavCursor, 0);

            for (int i = start; i < end; i++)
            {
                var mod = mods[i];

                var dropRectMin = ImGui.GetCursorScreenPos();
                mod.DrawIcon(builder);
                ImGui.SameLine();

                bool isSelected = SelectedMod == mod;
                if (ImGui.Selectable(mod.BuildLabel(builder, i), isSelected))
                {
                    SelectedMod = mod;
                }

                if (i == focusOnIndex && focusOnTarget == mods)
                {
                    SelectedMod = mod;
                    focusOnIndex = -1;
                    focusOnTarget = null;
                    ImGuiP.FocusItem();
                }

                bool isFocused = ImGui.IsItemFocused();
                if (isFocused)
                {
                    SelectedMod = mod;
                }

                if (isFocused && ImGuiP.IsKeyPressed(ImGuiKey.Enter))
                {
                    loadOrder.ToggleMod(mod);
                    RefreshUI(i, mods);
                }

                HandleDragDrop(mods, ref builder, i, avail, lineHeight, ref draw, mod, dropRectMin);

                mod.DrawContextMenu();

                var hovered = ImGui.IsItemHovered();
                if (hovered && ImGuiP.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    loadOrder.ToggleMod(mod);
                    RefreshUI();
                }

                bool hoveredMessages = mod.DrawMessages(builder, hovered, avail.X);

                if (hovered && !hoveredMessages)
                {
                    mod.DrawTooltip(builder);
                }
            }
            ImGui.PopStyleColor();
            int delta = mods.Count - end;
            if (delta > 0)
            {
                ImGui.Dummy(new(0, delta * lineHeight));
            }

            ImGui.EndChild();
        }

        private unsafe void HandleDragDrop(FilteredList<RimMod> mods, ref StrBuilder builder, int i, Vector2 avail, float lineHeight, ref ImDrawListPtr draw, RimMod mod, Vector2 dropRectMin)
        {
            if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.None))
            {
                ImGui.SetDragDropPayload("RimMod"u8, &i, sizeof(int));
                builder.Reset();
                builder.Append(mod.GetIcon());
                builder.End();
                ImGui.TextColored(mod.GetIconColor(), builder);
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
                        loadOrder!.Move(mods[index], i);
                        RefreshUI();
                    }
                }
                ImGui.EndDragDropTarget();
            }
        }

        private static StrBuilder BuildLabel(StrBuilder builder, char icon)
        {
            builder.Reset();
            builder.Append(icon);
            builder.End();
            return builder;
        }
    }
}