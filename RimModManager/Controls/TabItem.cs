namespace RimModManager.Controls
{
    using Hexa.NET.ImGui;

    public abstract class TabItem
    {
        private bool isShown = true;

        public abstract string Name { get; }

        public virtual ImGuiTabItemFlags Flags { get; }

        public bool IsShown
        {
            get
            {
                return isShown;
            }
            set
            {
                isShown = value;
            }
        }

        public bool IsClosable { get; set; }

        public virtual unsafe void Draw()
        {
            if (IsClosable)
            {
                if (!ImGui.BeginTabItem(Name, ref isShown, Flags)) return;
            }
            else if (!ImGui.BeginTabItem(Name, Flags)) return;

            DrawContent();

            ImGui.EndTabItem();
        }

        public abstract void DrawContent();
    }
}