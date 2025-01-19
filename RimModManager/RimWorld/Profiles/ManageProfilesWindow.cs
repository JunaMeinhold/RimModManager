namespace RimModManager.RimWorld.Profiles
{
    using Hexa.NET.ImGui;
    using Hexa.NET.ImGui.Widgets;
    using Hexa.NET.KittyUI.ImGuiBackend;
    using Hexa.NET.Utilities.Text;
    using System.Numerics;

    public class ManageProfilesWindow : ImWindow
    {
        private readonly RimProfileManager profileManager;
        private readonly RimModList mods;
        private RimProfile? selectedProfile;
        private float split = 200;

        public ManageProfilesWindow(RimProfileManager profileManager, RimModList mods)
        {
            this.profileManager = profileManager;
            this.mods = mods;
        }

        protected override string Name { get; } = "Manage Profiles";

        public override unsafe void DrawContent()
        {
            ImGui.BeginChild("##SidePanel", new Vector2(split, 0));
            var avail = ImGui.GetContentRegionAvail();
            if (ImGui.BeginListBox("##ProfilesList"u8, avail))
            {
                foreach (var profile in profileManager.Profiles)
                {
                    if (ImGui.Selectable(profile.Name, selectedProfile == profile))
                    {
                        selectedProfile = profile;
                        selectedProfile.PopulateList(mods);
                    }
                }
                ImGui.EndListBox();
            }
            ImGui.EndChild();

            ImGuiSplitter.VerticalSplitter("Splitter"u8, ref split);

            ImGui.BeginChild("##MainPanel");

            if (selectedProfile != null)
            {
                byte* buffer = stackalloc byte[2048];
                StrBuilder builder = new(buffer, 2048);

                builder.Reset();
                builder.Append(MaterialIcons.Delete);
                builder.End();

                if (ImGui.Button(builder))
                {
                    profileManager.Delete(selectedProfile);
                    selectedProfile = null;
                }

                builder.Reset();
                builder.Append(MaterialIcons.TextFieldsAlt);
                builder.End();

                ImGui.SameLine();

                if (ImGui.Button(builder) && selectedProfile != null)
                {
                    profileManager.Rename(selectedProfile);
                }

                selectedProfile?.DrawLoadOrder(builder);
            }

            ImGui.EndChild();
        }
    }
}