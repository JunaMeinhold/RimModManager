namespace RimModManager.Tabs
{
    using Hexa.NET.ImGui;
    using Hexa.NET.Utilities.Text;
    using RimModManager.Controls;
    using RimModManager.RimWorld;
    using System.Numerics;

    public abstract class TabItemBase : TabItem
    {
        protected TabItemBase()
        {
        }

        public abstract void UpdateConfig(RimModManagerConfig config, RimModList mods);
    }

    public class SaveGameTab : TabItemBase
    {
        private readonly RimSaveGameManager manager = new();
        private readonly HashSet<RimSaveGame> openSet = [];

        public SaveGameTab() : base()
        {
        }

        public override string Name { get; } = "Save Games";

        public override unsafe void DrawContent()
        {
            byte* buffer = stackalloc byte[2048];
            StrBuilder builder = new(buffer, 2048);

            foreach (var saveGame in manager.SaveGames)
            {
                bool extended = openSet.Contains(saveGame);
                if (DrawSaveGame(saveGame, extended))
                {
                    if (extended)
                    {
                        openSet.Remove(saveGame);
                    }
                    else
                    {
                        openSet.Add(saveGame);
                    }
                }
            }
        }

        private static unsafe bool DrawSaveGame(RimSaveGame saveGame, bool extended, Vector2 size = default)
        {
            var window = ImGuiP.GetCurrentWindow();
            if (window.SkipItems) return false;

            uint id = ImGui.GetID(saveGame.Path);

            var style = ImGui.GetStyle();
            var lineHeight = ImGui.GetTextLineHeight();

            var cur = ImGui.GetCursorScreenPos();
            var avail = ImGui.GetContentRegionAvail();

            Vector2 actualSize = default;
            if (size.X <= 0)
            {
                actualSize.X += avail.X;
            }

            actualSize.Y = lineHeight * 2 + style.FramePadding.Y * 2;
            ImRect rect = new(cur, cur + actualSize);
            ImGuiP.ItemSize(rect);
            if (!ImGuiP.ItemAdd(rect, id, &rect, ImGuiItemFlags.None))
            {
                return false;
            }

            var hovered = ImGuiP.ItemHoverable(rect, id, ImGuiItemFlags.None);

            bool result = false;
            if (hovered && ImGuiP.IsMouseClicked(ImGuiMouseButton.Left))
            {
                result = true;
            }

            var draw = ImGui.GetWindowDrawList();

            var bgColor = ImGui.GetColorU32(ImGuiCol.FrameBg);
            var bgHovColor = ImGui.GetColorU32(ImGuiCol.FrameBgHovered);
            var text = ImGui.GetColorU32(ImGuiCol.Text);
            var textDisabled = ImGui.GetColorU32(ImGuiCol.TextDisabled);

            draw.AddRect(rect.Min, rect.Max, hovered ? bgHovColor : bgColor);

            byte* buffer = stackalloc byte[256];
            StrBuilder builder = new(buffer, 256);
            builder.Append(saveGame.CreateDate);
            builder.End();

            cur += style.FramePadding;
            draw.AddText(cur, text, saveGame.Name);
            cur.Y += lineHeight;
            draw.AddText(cur, textDisabled, buffer);

            return result;
        }

        public override void UpdateConfig(RimModManagerConfig config, RimModList mods)
        {
            openSet.Clear();
            manager.Load(config, mods);
        }
    }
}