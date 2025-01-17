namespace RimModManager.Controls
{
    using Hexa.NET.ImGui;

    public class TabBarControl
    {
        public string Name { get; set; } = string.Empty;

        public ImGuiTabBarFlags Flags { get; set; }

        public List<TabItem> Items { get; } = [];

        public virtual void Draw()
        {
            if (!ImGui.BeginTabBar(Name, Flags)) return;

            DrawContent();

            ImGui.EndTabBar();
        }

        public virtual void DrawContent()
        {
            foreach (var item in Items)
            {
                item.Draw();
            }
        }
    }
}